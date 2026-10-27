<template>
  <!-- A product's contract price tiers (legacy product-prices-list): base price list tiers merged with the contract's
       overrides. Editing a base tier saves an override; Restore drops overrides back to the base prices. -->
  <VcBlade
    :title="$t('CONTRACTS.PRODUCT_PRICES.TITLE', { name: options?.productName ?? '' })"
    :toolbar-items="bladeToolbar"
    :loading="loading || saving"
    width="60%"
  >
    <VcContainer>
      <div class="contracts-form tw-space-y-4">
        <VcBanner
          v-if="error"
          variant="danger"
          icon="lucide-circle-alert"
        >
          {{ error }}
        </VcBanner>
        <VcCard :header="$t('CONTRACTS.WIDGETS.PRICES')">
          <VcDataTable
            v-model:selection="selection"
            selection-mode="multiple"
            :items="rows"
            edit-mode="inline"
            state-key="CONTRACT_PRODUCT_PRICES"
            :is-row-selectable="(p: Row) => p.state !== 'Base'"
            :empty-state="{ icon: 'lucide-badge-dollar-sign', title: $t('CONTRACTS.PRICES.EMPTY') }"
            @cell-edit-complete="onEdit"
          >
            <VcColumn
              id="currency"
              :title="$t('CONTRACTS.PRODUCT_PRICES.CURRENCY')"
              mobile-role="field"
              width="100px"
            />
            <VcColumn
              id="list"
              :title="$t('CONTRACTS.PRODUCT_PRICES.LIST')"
              type="number"
              mobile-role="title"
              editable
              always-visible
            />
            <VcColumn
              id="sale"
              :title="$t('CONTRACTS.PRODUCT_PRICES.SALE')"
              type="number"
              mobile-role="field"
              editable
              always-visible
            />
            <VcColumn
              id="minQuantity"
              :title="$t('CONTRACTS.PRODUCT_PRICES.MIN_QTY')"
              type="number"
              mobile-role="field"
              editable
              always-visible
            />
            <VcColumn
              id="state"
              :title="$t('CONTRACTS.PRICES.COLUMNS.STATE')"
              mobile-role="status"
              width="100px"
            >
              <template #body="{ data }">
                <div
                  v-if="data.state === 'New' || data.state === 'Updated' || !data.id"
                  class="dash-inline"
                >
                  <VcStatus :variant="data.state === 'Updated' ? 'warning' : 'primary'">{{
                    data.state === "Updated" ? $t("CONTRACTS.PRODUCT_PRICES.STATE_UPDATED") : $t("CONTRACTS.PRODUCT_PRICES.STATE_NEW")
                  }}</VcStatus>
                </div>
              </template>
            </VcColumn>
          </VcDataTable>
        </VcCard>
      </div>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useBladeForm, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBanner, VcBlade, VcCard, VcColumn, VcContainer, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { clone, errorMessage, priceError, usePricesClient } from "../api";
import type { MergedPrice } from "../../api_client/virtocommerce.contracts";

defineBlade({ name: "ContractProductPrices" });

type Row = MergedPrice & { id: string };

// param: the product id
const { t } = useI18n({ useScope: "global" });
const { param, options, callParent, closeSelf } = useBlade<{ contractId: string; productName?: string }>();
const { showConfirmation, showError } = usePopup();
const client = usePricesClient();

const prices = ref<MergedPrice[]>([]);
let original: MergedPrice[] = [];
// table rows need an id: unsaved tiers get a temporary one
const rows = computed<Row[]>(() => prices.value.map((p, i) => ({ ...p, id: p.id ?? `new-${i}` })));
const selection = ref<Row[]>([]);
const error = computed(() => prices.value.map((p) => priceError(p, prices.value, t)).find(Boolean) ?? "");

const form = useBladeForm({
  data: prices,
  canSaveOverride: computed(() => !error.value),
  closeConfirmMessage: computed(() => t("CONTRACTS.ALERTS.CLOSE_CONFIRMATION")),
});

const { loading, action: load } = useAsync(async () => {
  // the endpoint returns the merged prices as a plain array (swagger declares a search result)
  const r = (await (await client()).searchContractProductPrices({
    contractId: options.value!.contractId,
    productId: param.value!,
  })) as unknown as MergedPrice[];
  prices.value = [...(r ?? [])].sort((a, b) => (a.minQuantity ?? 0) - (b.minQuantity ?? 0));
  original = clone(prices.value);
  selection.value = [];
  form.setBaseline();
});

function onEdit(e: { field: string; newValue: unknown; index: number }) {
  const price = prices.value[e.index] as Record<string, unknown>;
  // a base tier keeps its quantity: an override replaces the base price of that quantity
  if (e.field === "minQuantity" && price.state === "Base") {
    prices.value.splice(e.index, 1, { ...prices.value[e.index] });
    return;
  }
  // an emptied cell (e.g. no sale price) is "not set"
  price[e.field] = e.newValue === "" || e.newValue === null || e.newValue === undefined ? undefined : Number(e.newValue);
}

function addPrice() {
  prices.value.push({
    productId: param.value,
    currency: prices.value[0]?.currency,
    minQuantity: prices.value.length ? undefined : 1,
  });
}

const same = (a: MergedPrice, b: MergedPrice) => a.list === b.list && a.sale === b.sale && a.minQuantity === b.minQuantity;

const { loading: saving, action: save } = useAsync(async () => {
  // only changed or new tiers; a changed base tier becomes a new override in the contract's price list
  const changed = prices.value
    .filter((p) => {
      const was = original.find((o) => o.id && o.id === p.id);
      return !was || !same(was, p);
    })
    .map((p) => ({ ...clone(p), id: p.state === "Base" ? undefined : p.id, state: undefined }));
  try {
    if (changed.length) await (await client()).saveContractPrice({ contractId: options.value!.contractId, prices: changed });
  } catch (e) {
    showError(errorMessage(e));
    return;
  }
  form.setBaseline();
  await callParent("reload");
  await closeSelf();
});

async function restore() {
  if (!(await showConfirmation(t("CONTRACTS.ALERTS.RESTORE_PRICES", { count: selection.value.length })))) return;
  try {
    await (await client()).restoreContractPrices({
      contractId: options.value!.contractId,
      productIds: [param.value!],
      priceIds: selection.value.map((p) => p.id),
    });
  } catch (e) {
    showError(errorMessage(e));
    return;
  }
  form.setBaseline();
  await callParent("reload");
  await closeSelf();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: t("CONTRACTS.TOOLBAR.SAVE"),
    icon: "lucide-save",
    permissions: "Contract:update",
    disabled: computed(() => !form.canSave.value || saving.value),
    clickHandler: () => save(),
  },
  {
    id: "reset",
    title: t("CONTRACTS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
  {
    id: "add",
    title: t("CONTRACTS.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: "Contract:update",
    clickHandler: addPrice,
  },
  {
    id: "restore",
    title: t("CONTRACTS.PRICES.RESTORE"),
    icon: "lucide-history",
    permissions: "Contract:update",
    disabled: computed(() => !selection.value.length || selection.value.some((p) => p.id.startsWith("new-"))),
    clickHandler: restore,
  },
]);

onMounted(load);
</script>
