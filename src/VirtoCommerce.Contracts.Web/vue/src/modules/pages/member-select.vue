<template>
  <!-- Picks customers for the contract (legacy member-select-list): browse the member tree (click an organization
       to open it) or search; members already in the contract can't be picked. -->
  <VcBlade
    :title="$t('CONTRACTS.MEMBER_SELECT.TITLE')"
    :subtitle="trail"
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
      state-key="CONTRACT_MEMBER_SELECT"
      :is-row-selectable="(m: Named) => !m.groups?.includes(options!.code)"
      :empty-state="{ icon: 'lucide-users', title: $t('CONTRACTS.MEMBERS.EMPTY') }"
      @row-click="(e: { data: Named }) => open(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="memberType"
        :title="$t('CONTRACTS.MEMBERS.TYPE')"
        width="72px"
      >
        <template #body="{ data }">
          <VcIcon
            :icon="MEMBER_BLADES[data.memberType ?? '']?.icon ?? 'lucide-user'"
            :title="data.memberType"
          />
        </template>
      </VcColumn>
      <VcColumn
        id="name"
        :title="$t('CONTRACTS.FIELDS.NAME')"
        mobile-role="title"
        always-visible
      >
        <template #body="{ data }">
          {{ data.name }}
          <span
            v-if="data.groups?.includes(options!.code)"
            class="contracts-muted"
            >· {{ $t("CONTRACTS.MEMBER_SELECT.IN_CONTRACT") }}</span
          >
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useDataTablePagination, useFunctions, useTableSearch, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcIcon } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { MEMBER_BLADES, searchMembers, type Named } from "../api";

defineBlade({ name: "ContractMemberSelect" });

const PAGE_SIZE = 20;
const { t } = useI18n({ useScope: "global" });
const { options, callParent, closeSelf } = useBlade<{ code: string }>();
const { debounce } = useFunctions();

const items = ref<Named[]>([]);
const totalCount = ref(0);
const path = ref<Named[]>([]);
const trail = computed(() => [t("CONTRACTS.MEMBER_SELECT.ALL"), ...path.value.map((x) => x.name)].join(" / "));
const { searchValue } = useTableSearch({ stateKey: "CONTRACT_MEMBER_SELECT" });
const pagination = useDataTablePagination({ stateKey: "CONTRACT_MEMBER_SELECT", pageSize: PAGE_SIZE, totalCount });

// the picks are kept across pages and folders, as the legacy blade did (the table keeps off-page selected rows).
// ponytail: the header "select all" replaces them with the page's rows; keep a picked-ids map if that matters
const selection = ref<Named[]>([]);

const { loading, action: load } = useAsync(async () => {
  const r = await searchMembers({
    keyword: searchValue.value || undefined,
    memberId: path.value[path.value.length - 1]?.id,
    sort: "memberType:desc;name",
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = r.results;
  totalCount.value = r.totalCount;
});

load();
watch(searchValue, () => pagination.setPage(1));
watch([searchValue, () => pagination.skip], debounce(load, 300));

function go(next: Named[]) {
  path.value = next;
  // clearing the search or the page reloads through the watchers
  if (searchValue.value) searchValue.value = "";
  else if (pagination.skip) pagination.setPage(1);
  else load();
}

// organizations open like folders
const open = (m: Named) => m.memberType === "Organization" && go([...path.value, m]);

const { loading: adding, action: add } = useAsync(async () => {
  await callParent("addMembers", selection.value.map((m) => m.id));
  await closeSelf();
});

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("CONTRACTS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "up",
    title: t("CONTRACTS.MEMBER_SELECT.UP"),
    icon: "lucide-corner-left-up",
    disabled: computed(() => !path.value.length),
    clickHandler: () => go(path.value.slice(0, -1)),
  },
  {
    id: "confirm",
    title: computed(() => t("CONTRACTS.MEMBER_SELECT.CONFIRM", { count: selection.value.length })),
    icon: "lucide-check",
    disabled: computed(() => !selection.value.length || adding.value),
    clickHandler: () => add(),
  },
]);
</script>

<style lang="scss">
.contracts-muted {
  color: var(--color-neutral-500);
  font-size: 12px;
}
</style>
