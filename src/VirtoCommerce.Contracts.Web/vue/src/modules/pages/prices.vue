<template>
  <!-- The contract's product prices (legacy contract-prices-list): the linked base price list merged with the
       contract's own (priority) price list. Until a price list is linked, only "Link price list" is offered. -->
  <VcBlade
    :title="$t('CONTRACTS.PRICES.TITLE')"
    :subtitle="options?.name"
    :toolbar-items="bladeToolbar"
    :loading="loading || saving"
    width="60%"
  >
    <div
      v-if="!linked"
      class="contracts-link-pricelist"
    >
      <VcIcon
        icon="lucide-badge-dollar-sign"
        size="xl"
      />
      <p>{{ $t("CONTRACTS.PRICES.NOT_LINKED") }}</p>
      <VcButton
        v-if="hasAccess('Contract:update')"
        icon="lucide-plus"
        @click="openPricelistSelect"
      >
        {{ $t("CONTRACTS.PRICES.LINK_PRICELIST") }}
      </VcButton>
    </div>
    <VcDataTable
      v-else
      v-model:search-value="searchValue"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      state-key="CONTRACT_PRICES"
      :active-item-id="activeChildParam"
      :is-row-selectable="(g: Row) => g.groupState !== 'Base'"
      :empty-state="{ icon: 'lucide-badge-dollar-sign', title: $t('CONTRACTS.PRICES.EMPTY') }"
      @row-click="(e: { data: Row }) => openProduct(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="productImgSrc"
        :title="$t('CONTRACTS.PRICES.COLUMNS.IMAGE')"
        type="image"
        mobile-role="image"
        width="64px"
      />
      <!-- the item code under the name keeps the name readable next to the contract blade -->
      <VcColumn
        id="productName"
        :title="$t('CONTRACTS.PRICES.COLUMNS.NAME')"
        mobile-role="title"
        always-visible
      >
        <template #body="{ data }">
          <div class="contracts-product">
            <span class="contracts-product__name">{{ data.productName }}</span>
            <span class="contracts-product__code">{{ data.productCode }}</span>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="groupPricesCount"
        :title="$t('CONTRACTS.PRICES.COLUMNS.COUNT')"
        type="number"
        mobile-role="field"
        width="90px"
      />
      <VcColumn
        id="range"
        :title="$t('CONTRACTS.PRICES.COLUMNS.RANGE')"
        mobile-role="field"
        align="end"
        width="190px"
      >
        <template #body="{ data }">
          <span class="vc-table-cell-number">{{ data.range }}</span>
        </template>
      </VcColumn>
      <VcColumn
        id="groupState"
        :title="$t('CONTRACTS.PRICES.COLUMNS.STATE')"
        mobile-role="status"
        width="110px"
      >
        <template #body="{ data }">
          <div
            v-if="data.groupState !== 'Base'"
            class="dash-inline"
          >
            <VcStatus variant="warning">{{ $t("CONTRACTS.PRICES.MODIFIED") }}</VcStatus>
          </div>
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  useAsync,
  useBlade,
  useDataTablePagination,
  useFunctions,
  usePermissions,
  usePopup,
  useTableSearch,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcButton, VcColumn, VcDataTable, VcIcon, VcStatus } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { usePricesClient, type ProductEntry } from "../api";
import type { MergedPriceGroup } from "../../api_client/virtocommerce.contracts";

defineBlade({ name: "ContractPrices" });

type Row = MergedPriceGroup & { id: string; range: string };

const PAGE_SIZE = 20;
const { t, n } = useI18n({ useScope: "global" });
const { options, activeChildParam, openBlade, callParent, exposeToChildren, closeChildren } = useBlade<{
  contractId: string;
  code: string;
  name?: string;
  pricelistLinked?: boolean;
}>();
const { showConfirmation } = usePopup();
const { hasAccess } = usePermissions();
const { debounce } = useFunctions();
const client = usePricesClient();

const linked = ref(!!options.value?.pricelistLinked);
const items = ref<Row[]>([]);
const totalCount = ref(0);
const selection = ref<Row[]>([]);
const { searchValue } = useTableSearch({ stateKey: "CONTRACT_PRICES" });
const pagination = useDataTablePagination({ stateKey: "CONTRACT_PRICES", pageSize: PAGE_SIZE, totalCount });

// the legacy getPriceRange: lowest of list/sale to highest of list/sale
const fmt = (v: number) => n(v, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
function range(g: MergedPriceGroup) {
  const min = g.minSalePrice ? Math.min(g.minListPrice ?? 0, g.minSalePrice) : (g.minListPrice ?? 0);
  const max = Math.max(g.maxListPrice ?? 0, g.maxSalePrice ?? 0);
  return min === max ? fmt(max) : `${fmt(min)} – ${fmt(max)}`;
}

const { loading, action: load } = useAsync(async () => {
  if (!linked.value) return;
  const r = await (await client()).searchContractGroupedPrices({
    contractId: options.value!.contractId,
    keyword: searchValue.value || undefined,
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = (r.results ?? []).map((g) => ({ ...g, id: g.productId!, range: range(g) }));
  totalCount.value = r.totalCount ?? 0;
  selection.value = [];
});

load();
watch(searchValue, () => pagination.setPage(1));
watch([searchValue, () => pagination.skip], debounce(load, 300));

async function changed() {
  await Promise.all([load(), callParent("refreshCounts")]);
}

const { loading: saving, action: run } = useAsync<() => Promise<unknown>>(async (fn) => {
  await fn!();
});

const openPricelistSelect = () => openBlade({ name: "ContractPricelistSelect" });

// from ContractPricelistSelect: creates the contract's price lists and assignments on the server
async function linkPricelist(pricelistId: unknown) {
  await run(async () => {
    const contract = await (await client()).linkPricelist({ contractId: options.value!.contractId, pricelistId: pricelistId as string });
    // the server links nothing (and answers no contract) when the price list or the contract is gone
    if (!contract) throw new Error(t("CONTRACTS.ALERTS.PRICELIST_NOT_LINKED"));
    linked.value = true;
    await callParent("pricelistLinked", contract);
  });
  await load();
}

// from ContractProductPicker: a single-item tier per product in the contract's price list (as the legacy blade)
async function addProducts(products: unknown) {
  await run(async () =>
    (await client()).saveContractPrice({
      contractId: options.value!.contractId,
      prices: (products as ProductEntry[]).map((p) => ({ productId: p.id, minQuantity: 1 })),
    }),
  );
  await changed();
}

function openProduct(g: Row) {
  openBlade({
    name: "ContractProductPrices",
    param: g.productId,
    options: { contractId: options.value!.contractId, productName: g.productName },
  });
}

async function restore() {
  if (!(await showConfirmation(t("CONTRACTS.ALERTS.RESTORE_PRODUCTS", { count: selection.value.length })))) return;
  await closeChildren();
  const productIds = selection.value.map((g) => g.productId!);
  await run(async () => (await client()).restoreContractPrices({ contractId: options.value!.contractId, productIds, priceIds: [] }));
  await changed();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "link",
    title: t("CONTRACTS.PRICES.LINK_PRICELIST"),
    icon: "lucide-link",
    permissions: "Contract:update",
    isVisible: computed(() => !linked.value),
    clickHandler: openPricelistSelect,
  },
  {
    id: "refresh",
    title: t("CONTRACTS.TOOLBAR.REFRESH"),
    icon: "lucide-refresh-cw",
    isVisible: linked,
    clickHandler: () => load(),
  },
  {
    id: "add",
    title: t("CONTRACTS.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: "Contract:update",
    isVisible: linked,
    clickHandler: () => openBlade({ name: "ContractProductPicker" }),
  },
  {
    id: "restore",
    title: t("CONTRACTS.PRICES.RESTORE"),
    icon: "lucide-undo-2",
    permissions: "Contract:update",
    isVisible: linked,
    disabled: computed(() => !selection.value.length),
    clickHandler: restore,
  },
]);

exposeToChildren({ linkPricelist, addProducts, reload: changed });
</script>

<style lang="scss">
.contracts-product {
  display: flex;
  flex-direction: column;
  min-width: 0;
  &__name {
    overflow: hidden;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    color: var(--color-neutral-900);
  }
  &__code {
    font-size: 12px;
    color: var(--color-neutral-500);
  }
}

.contracts-link-pricelist {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 64px 24px;
  text-align: center;
  font-size: 14px;
  color: var(--color-neutral-500);
}
</style>
