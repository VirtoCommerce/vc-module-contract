<template>
  <!-- Picks the base price list of the contract (legacy pricelist-select-list) -->
  <VcBlade
    :title="$t('CONTRACTS.PRICELIST_SELECT.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading || linking"
    width="40%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:sort-field="sortField"
      v-model:sort-order="sortOrder"
      v-model:selection="selected"
      selection-mode="single"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      state-key="CONTRACT_PRICELIST_SELECT"
      :empty-state="{ icon: 'lucide-badge-dollar-sign', title: $t('CONTRACTS.PRICELIST_SELECT.EMPTY') }"
      @row-click="(e: { data: Named }) => (selected = e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="name"
        :title="$t('CONTRACTS.FIELDS.NAME')"
        mobile-role="title"
        sortable
        always-visible
      />
      <VcColumn
        id="currency"
        :title="$t('CONTRACTS.PRICELIST_SELECT.CURRENCY')"
        mobile-role="field"
        sortable
        width="110px"
      />
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
  useTableSearch,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { searchPricelists, type Named } from "../api";

defineBlade({ name: "ContractPricelistSelect" });

const PAGE_SIZE = 20;
const { t } = useI18n({ useScope: "global" });
const { callParent, closeSelf } = useBlade();
const { debounce } = useFunctions();

const items = ref<Named[]>([]);
const totalCount = ref(0);
// single selection mode: the table binds one row
const selected = ref<Named>();
const { sortField, sortOrder, sortExpression } = useDataTableSort({
  stateKey: "CONTRACT_PRICELIST_SELECT",
  initialField: "name",
  initialDirection: "ASC",
});
const { searchValue } = useTableSearch({ stateKey: "CONTRACT_PRICELIST_SELECT" });
const pagination = useDataTablePagination({ stateKey: "CONTRACT_PRICELIST_SELECT", pageSize: PAGE_SIZE, totalCount });

const { loading, action: load } = useAsync(async () => {
  const r = await searchPricelists(searchValue.value || undefined, pagination.skip, PAGE_SIZE, sortExpression.value);
  items.value = r.results;
  totalCount.value = r.totalCount;
});

load();
watch(searchValue, () => pagination.setPage(1));
watch([sortExpression, searchValue, () => pagination.skip], debounce(load, 300));

const { loading: linking, action: link } = useAsync(async () => {
  await callParent("linkPricelist", selected.value!.id);
  await closeSelf();
});

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("CONTRACTS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "confirm",
    title: t("CONTRACTS.PRICELIST_SELECT.CONFIRM"),
    icon: "lucide-check",
    disabled: computed(() => !selected.value || linking.value),
    clickHandler: () => link(),
  },
]);
</script>
