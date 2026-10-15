<template>
  <!-- Customers (contacts, organizations...) the contract applies to: members carrying the contract code as a group -->
  <VcBlade
    :title="$t('CONTRACTS.MEMBERS.TITLE')"
    :subtitle="options?.name"
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
      state-key="CONTRACT_MEMBERS"
      :active-item-id="activeChildParam"
      :row-actions="rowActions"
      row-actions-mode="dropdown"
      row-actions-position="column"
      :empty-state="{ icon: 'lucide-users', title: $t('CONTRACTS.MEMBERS.EMPTY') }"
      @row-click="(e: { data: Member }) => openMember(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="memberType"
        :title="$t('CONTRACTS.MEMBERS.TYPE')"
        width="96px"
        sortable
      >
        <template #body="{ data }">
          <VcImage
            v-if="data.iconUrl"
            :src="data.iconUrl"
            size="xs"
            bordered
          />
          <VcIcon
            v-else
            :icon="MEMBER_BLADES[data.memberType ?? '']?.icon ?? 'lucide-user'"
            :title="data.memberType"
          />
        </template>
      </VcColumn>
      <VcColumn
        id="name"
        :title="$t('CONTRACTS.FIELDS.NAME')"
        mobile-role="title"
        sortable
        always-visible
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  notification,
  useAsync,
  useBlade,
  useDataTablePagination,
  useDataTableSort,
  useFunctions,
  usePermissions,
  usePopup,
  useTableSearch,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcIcon, VcImage } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { errorMessage, MEMBER_BLADES, useMembersClient } from "../api";
import type { Member } from "../../api_client/virtocommerce.contracts";

defineBlade({ name: "ContractMembers" });

const PAGE_SIZE = 20;
const { t } = useI18n({ useScope: "global" });
const { options, activeChildParam, openBlade, callParent, exposeToChildren, closeChildren } = useBlade<{ contractId: string; code: string; name?: string }>();
const { showConfirmation, showError } = usePopup();
const { hasAccess } = usePermissions();
const { debounce } = useFunctions();
const client = useMembersClient();

const items = ref<Member[]>([]);
const totalCount = ref(0);
const selection = ref<Member[]>([]);
const { sortField, sortOrder, sortExpression } = useDataTableSort({
  stateKey: "CONTRACT_MEMBERS",
  initialField: "memberType",
  initialDirection: "DESC",
});
const { searchValue } = useTableSearch({ stateKey: "CONTRACT_MEMBERS" });
const pagination = useDataTablePagination({ stateKey: "CONTRACT_MEMBERS", pageSize: PAGE_SIZE, totalCount });

const { loading, action: load } = useAsync(async () => {
  const r = await (await client()).searchContractMembers({
    contractId: options.value!.contractId,
    keyword: searchValue.value || undefined,
    deepSearch: true,
    sort: sortExpression.value,
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = r.results ?? [];
  totalCount.value = r.totalCount ?? 0;
});

load();
watch(searchValue, () => pagination.setPage(1));
watch([sortExpression, searchValue, () => pagination.skip], debounce(load, 300));

async function changed() {
  selection.value = [];
  await Promise.all([load(), callParent("refreshCounts")]);
}

async function remove(list: Member[]) {
  if (!(await showConfirmation(t("CONTRACTS.ALERTS.DELETE_MEMBERS", { count: list.length })))) return;
  await closeChildren();
  try {
    await (await client()).deleteContractMembers({ contractId: options.value!.contractId, memberIds: list.map((m) => m.id!) });
  } catch (e) {
    showError(errorMessage(e));
    return;
  }
  await changed();
}

async function addMembers(ids: unknown) {
  await (await client()).addContractMembers({ contractId: options.value!.contractId, memberIds: ids as string[] });
  await changed();
}

// "Manage": the member's own blade, owned by the Customer module UI (it may not be installed in this host)
async function openMember(member: Member) {
  const blade = MEMBER_BLADES[member.memberType ?? ""]?.blade;
  if (!blade) {
    notification.error(t("CONTRACTS.ALERTS.UNKNOWN_MEMBER_TYPE", { type: member.memberType }), { timeout: 5000 });
    return;
  }
  await openBlade({ name: blade, param: member.id }).catch(() =>
    notification.error(t("CONTRACTS.ALERTS.MEMBER_UNAVAILABLE"), { timeout: 5000 }),
  );
}

const rowActions = (member: Member) => [
  { id: "manage", icon: "lucide-pencil", title: t("CONTRACTS.MEMBERS.MANAGE"), type: "info" as const, clickHandler: () => openMember(member) },
  {
    id: "delete",
    icon: "lucide-trash-2",
    title: t("CONTRACTS.TOOLBAR.DELETE"),
    type: "danger" as const,
    variant: "danger" as const,
    hidden: !hasAccess("Contract:delete"),
    clickHandler: () => remove([member]),
  },
];

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("CONTRACTS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "add",
    title: t("CONTRACTS.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: "Contract:update",
    clickHandler: () => openBlade({ name: "ContractMemberSelect", options: { code: options.value!.code } }),
  },
  {
    id: "delete",
    title: t("CONTRACTS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "Contract:delete",
    disabled: computed(() => !selection.value.length),
    clickHandler: () => remove(selection.value),
  },
]);

exposeToChildren({ addMembers, reload: () => load() });
</script>
