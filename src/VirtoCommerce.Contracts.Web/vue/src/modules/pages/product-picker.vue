<template>
  <!-- Replaces the legacy Catalog "select items" blade: keyword search over all catalogs' products -->
  <VcBlade
    :title="$t('CONTRACTS.PRODUCT_PICKER.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading || adding"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      state-key="CONTRACT_PRODUCT_PICKER"
      :empty-state="{ icon: 'lucide-package', title: $t('CONTRACTS.PRODUCT_PICKER.EMPTY') }"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="imageUrl"
        :title="$t('CONTRACTS.PRICES.COLUMNS.IMAGE')"
        type="image"
        mobile-role="image"
        width="72px"
      />
      <VcColumn
        id="name"
        :title="$t('CONTRACTS.PRICES.COLUMNS.NAME')"
        mobile-role="title"
        always-visible
      />
      <VcColumn
        id="code"
        :title="$t('CONTRACTS.PRICES.COLUMNS.CODE')"
        mobile-role="field"
        always-visible
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useDataTablePagination, useFunctions, useTableSearch, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { searchProducts, type ProductEntry } from "../api";

defineBlade({ name: "ContractProductPicker" });

const PAGE_SIZE = 20;
const { t } = useI18n({ useScope: "global" });
const { callParent, closeSelf } = useBlade();
const { debounce } = useFunctions();

const items = ref<ProductEntry[]>([]);
const totalCount = ref(0);
const selection = ref<ProductEntry[]>([]);
const { searchValue } = useTableSearch({ stateKey: "CONTRACT_PRODUCT_PICKER" });
const pagination = useDataTablePagination({ stateKey: "CONTRACT_PRODUCT_PICKER", pageSize: PAGE_SIZE, totalCount });

const { loading, action: load } = useAsync(async () => {
  const r = await searchProducts(searchValue.value || undefined, pagination.skip, PAGE_SIZE);
  items.value = r.results;
  totalCount.value = r.totalCount;
});

load();
watch(searchValue, () => pagination.setPage(1));
watch([searchValue, () => pagination.skip], debounce(load, 300));

const { loading: adding, action: add } = useAsync(async () => {
  await callParent("addProducts", selection.value);
  await closeSelf();
});

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "add",
    title: t("CONTRACTS.PRODUCT_PICKER.ADD_SELECTED"),
    icon: "lucide-plus",
    disabled: computed(() => !selection.value.length || adding.value),
    clickHandler: () => add(),
  },
]);
</script>
