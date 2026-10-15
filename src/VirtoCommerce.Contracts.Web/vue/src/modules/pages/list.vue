<template>
  <VcBlade
    :title="$t('CONTRACTS.LIST.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:sort-field="sortField"
      v-model:sort-order="sortOrder"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      state-key="CONTRACTS"
      :active-item-id="activeItemId"
      :show-all-columns="!activeItemId"
      :row-actions="rowActions"
      row-actions-mode="dropdown"
      row-actions-position="column"
      :empty-state="{ icon: 'lucide-file-signature', title: $t('CONTRACTS.LIST.EMPTY') }"
      @row-click="(e: { data: Contract }) => openDetails(e.data.id)"
      @pagination-click="pagination.goToPage"
    >
      <template #search-header-actions>
        <div class="contracts-saved-filter">
          <VcSelect
            :model-value="currentId"
            :options="savedFilterOptions"
            :placeholder="$t('CONTRACTS.LIST.SAVED_FILTERS.PLACEHOLDER')"
            size="small"
            @update:model-value="onSavedFilterPicked"
          />
          <VcButton
            v-if="currentFilter"
            icon="lucide-pencil"
            variant="ghost"
            size="sm"
            :aria-label="$t('CONTRACTS.LIST.SAVED_FILTERS.EDIT')"
            @click="openFilter(currentFilter.id)"
          />
        </div>
      </template>
      <VcColumn
        id="name"
        :title="$t('CONTRACTS.FIELDS.NAME')"
        mobile-role="title"
        sortable
        always-visible
      />
      <VcColumn
        id="code"
        :title="$t('CONTRACTS.FIELDS.CODE')"
        mobile-role="field"
        sortable
      />
      <VcColumn
        id="storeId"
        :title="$t('CONTRACTS.FIELDS.STORE')"
        mobile-role="field"
        sortable
      >
        <template #body="{ data }">{{ storeNames[data.storeId] ?? data.storeId }}</template>
      </VcColumn>
      <VcColumn
        id="startDate"
        :title="$t('CONTRACTS.FIELDS.START_DATE')"
        type="date"
        mobile-role="field"
        sortable
      />
      <VcColumn
        id="endDate"
        :title="$t('CONTRACTS.FIELDS.END_DATE')"
        type="date"
        mobile-role="field"
        sortable
      />
      <VcColumn
        id="status"
        :title="$t('CONTRACTS.FIELDS.STATUS')"
        mobile-role="status"
        sortable
        always-visible
      >
        <template #body="{ data }">
          <div
            v-if="data.status"
            class="dash-inline"
          >
            <VcStatus :variant="statusVariant(data.status)">{{ data.status }}</VcStatus>
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
  useDataTableSort,
  useFunctions,
  useLatestRequest,
  usePermissions,
  usePopup,
  useTableSearch,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcButton, VcColumn, VcDataTable, VcSelect, VcStatus } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { errorMessage, searchStoreOptions, useContractsClient } from "../api";
import { useSavedFilters } from "../composables";
import type { Contract } from "../../api_client/virtocommerce.contracts";

defineBlade({
  url: "/contracts",
  name: "ContractsList",
  isWorkspace: true,
  permissions: ["Contract:access"],
  menuItem: {
    title: "CONTRACTS.MENU.TITLE",
    icon: "lucide-file-signature",
    priority: 35,
  },
});

const PAGE_SIZE = 20;
const { t } = useI18n({ useScope: "global" });
const { openBlade, exposeToChildren, closeChildren } = useBlade();
const { debounce } = useFunctions();
const { showConfirmation, showError } = usePopup();
const { hasAccess } = usePermissions();
const client = useContractsClient();
const latest = useLatestRequest();

const items = ref<Contract[]>([]);
const totalCount = ref(0);
const selection = ref<Contract[]>([]);
const activeItemId = ref<string>();
const storeNames = ref<Record<string, string>>({});
searchStoreOptions(undefined, 0)
  .then((r) => (storeNames.value = Object.fromEntries(r.results.map((s) => [s.id, s.name]))))
  .catch(() => undefined);

const { sortField, sortOrder, sortExpression } = useDataTableSort({ stateKey: "CONTRACTS", initialField: "name", initialDirection: "ASC" });
const { searchValue } = useTableSearch({ stateKey: "CONTRACTS" });
const pagination = useDataTablePagination({ stateKey: "CONTRACTS", pageSize: PAGE_SIZE, totalCount });

// saved filters (the AngularJS filter dropdown): the user's filters and "new filter"
const { filters: savedFilters, currentId, find } = useSavedFilters();
const NEW_FILTER = "__new";
const currentFilter = computed(() => find(currentId.value));
const savedFilterOptions = computed(() => [
  ...savedFilters.value.map((f) => ({ id: f.id, title: f.name })),
  { id: NEW_FILTER, title: t("CONTRACTS.LIST.SAVED_FILTERS.NEW") },
]);

async function onSavedFilterPicked(id: unknown) {
  if (id === NEW_FILTER) {
    openFilter();
    return;
  }
  // as the AngularJS list, another filter closes the filter editor (the only child while no contract is open)
  if (!activeItemId.value) await closeChildren();
  currentId.value = (id as string) || undefined;
  reloadFirstPage();
}

const openFilter = (filterId?: string) => openBlade({ name: "ContractFilterDetails", options: { filterId } });

const { loading, action: load } = useAsync(async () => {
  const f = currentFilter.value;
  const result = await latest.latest(
    (await client()).searchContracts({
      keyword: searchValue.value || undefined,
      statuses: f?.statuses?.length ? f.statuses : undefined,
      storeIds: f?.storeIds?.length ? f.storeIds : undefined,
      vendorId: f?.vendorId || undefined,
      activeStartDate: f?.activeStartDate ? new Date(f.activeStartDate) : undefined,
      activeEndDate: f?.activeEndDate ? new Date(f.activeEndDate) : undefined,
      sort: sortExpression.value,
      skip: pagination.skip,
      take: PAGE_SIZE,
    }),
  );
  if (!result) return; // a newer search superseded this one
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
});

function reloadFirstPage() {
  if (pagination.skip) pagination.setPage(1);
  else load();
}

load();
watch(searchValue, () => pagination.setPage(1));
watch([sortExpression, searchValue, () => pagination.skip], debounce(load, 300));

async function deleteContracts(list: Contract[]) {
  if (!(await showConfirmation(t("CONTRACTS.ALERTS.DELETE", { count: list.length })))) return;
  await closeChildren();
  try {
    await (await client()).deleteContract(list.map((x) => x.id!));
  } catch (e) {
    showError(errorMessage(e));
    return;
  }
  selection.value = [];
  await load();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("CONTRACTS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "add",
    title: t("CONTRACTS.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: "Contract:create",
    clickHandler: () => openDetails(),
  },
  {
    id: "delete",
    title: t("CONTRACTS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "Contract:delete",
    disabled: computed(() => !selection.value.length),
    clickHandler: () => deleteContracts(selection.value),
  },
]);

// the AngularJS row context menu
const rowActions = (contract: Contract) => [
  {
    id: "delete",
    icon: "lucide-trash-2",
    title: t("CONTRACTS.TOOLBAR.DELETE"),
    type: "danger" as const,
    variant: "danger" as const,
    hidden: !hasAccess("Contract:delete"),
    clickHandler: () => deleteContracts([contract]),
  },
];

function openDetails(id?: string) {
  // a new contract has no row: "new" only hides the extra columns while the blade is open
  const key = (activeItemId.value = id ?? "new");
  openBlade({
    name: "ContractDetails",
    param: id,
    onClose() {
      // opening another contract closes this blade after the new one became active
      if (activeItemId.value === key) activeItemId.value = undefined;
    },
  });
}

// Maps a Contract.Status value to a VcStatus variant
function statusVariant(status?: string) {
  const s = (status ?? "").toLowerCase();
  if (s === "active") return "success";
  if (["pending approval", "suspended"].includes(s)) return "warning";
  if (["expired", "terminated", "cancelled", "canceled"].includes(s)) return "danger";
  return "info";
}

exposeToChildren({
  reload: () => load(),
  reloadFirstPage,
  // a contract was created in the "new contract" blade: show it in place of that blade
  created: async (id: unknown) => {
    await load();
    openDetails(id as string);
  },
});
</script>

<style lang="scss">
.contracts-saved-filter {
  display: flex;
  align-items: center;
  gap: 4px;
  min-width: 200px;
}
</style>
