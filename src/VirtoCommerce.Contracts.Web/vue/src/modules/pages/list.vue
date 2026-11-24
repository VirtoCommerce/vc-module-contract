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
      search-bar
      state-key="CONTRACTS"
      :global-filters="globalFilters"
      :active-item-id="activeItemId"
      :show-all-columns="!activeItemId"
      :row-actions="rowActions"
      row-actions-mode="dropdown"
      row-actions-position="column"
      :empty-state="{ icon: 'lucide-file-signature', title: $t('CONTRACTS.LIST.EMPTY') }"
      @row-click="(e: { data: Contract }) => openDetails(e.data.id)"
      @pagination-click="pagination.goToPage"
      @filter="onFilter"
    >
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
  type GlobalFilterConfig,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { errorMessage, searchStoreOptions, useContractsClient } from "../api";
import { useStatuses } from "../composables";
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

// The AngularJS saved filters (status, store, vendor, period) become the search bar: an account manager narrows
// the list to a store's contracts, to a status (e.g. pending approval) or to the contracts in force today.
// Vendor is left out: the bar takes fixed options and any contact or organization can be a vendor.
const { statuses } = useStatuses();
const filters = ref<{ statuses?: string[]; storeIds?: string[]; onlyActive?: boolean; activeStartDate?: Date; activeEndDate?: Date }>({});
// the search bar takes toggle fields, which vc-shell 2.13 does not yet type in GlobalFilterConfig
const globalFilters = computed(
  () =>
    [
      {
        id: "statuses",
        label: t("CONTRACTS.FIELDS.STATUS"),
        filter: { multiple: true, options: statuses.value.map((s) => ({ value: s.id, label: s.title })) },
      },
      {
        id: "storeIds",
        label: t("CONTRACTS.FIELDS.STORE"),
        filter: {
          multiple: true,
          options: Object.entries(storeNames.value)
            .map(([value, label]) => ({ value, label }))
            .sort((a, b) => a.label.localeCompare(b.label)),
        },
      },
      { id: "onlyActive", label: t("CONTRACTS.LIST.FILTERS.ONLY_ACTIVE"), filter: { toggle: true } },
      { id: "period", label: t("CONTRACTS.LIST.FILTERS.PERIOD"), filter: { range: ["activeStartDate", "activeEndDate"] } },
    ] as unknown as GlobalFilterConfig[],
);

const asList = (value: unknown) => {
  const values = ([] as string[]).concat((value as string | string[] | undefined) ?? []);
  return values.length ? values : undefined;
};
// the search bar sends days (YYYY-MM-DD, both ends included); the API compares times, so the end is that day's last moment
const localDay = (ymd: unknown, endOfDay = false) => {
  if (typeof ymd !== "string" || !ymd) return undefined;
  const day = new Date(`${ymd}T00:00`);
  return endOfDay ? new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1, 0, 0, 0, -1) : day;
};

function onFilter(event: { filters: Record<string, unknown> }) {
  filters.value = {
    statuses: asList(event.filters.statuses),
    storeIds: asList(event.filters.storeIds),
    onlyActive: event.filters.onlyActive === true || undefined,
    activeStartDate: localDay(event.filters.activeStartDate),
    activeEndDate: localDay(event.filters.activeEndDate, true),
  };
  reloadFirstPage();
}

const { loading, action: load } = useAsync(async () => {
  const result = await latest.latest(
    (await client()).searchContracts({
      keyword: searchValue.value || undefined,
      ...filters.value,
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
  // a contract was created in the "new contract" blade: show it in place of that blade
  created: async (id: unknown) => {
    await load();
    openDetails(id as string);
  },
});
</script>

