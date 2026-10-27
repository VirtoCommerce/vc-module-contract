<template>
  <!-- The contract's attachments (legacy contract-attachments-list + contract-attachment-add blades):
       edited here, written back into the contract on OK and saved with it. -->
  <VcBlade
    :title="$t('CONTRACTS.ATTACHMENTS.TITLE')"
    :subtitle="options?.name"
    :toolbar-items="bladeToolbar"
    width="50%"
  >
    <div
      v-if="showUpload"
      class="contracts-upload"
    >
      <VcFileUpload
        :loading="uploading"
        accept="*"
        multiple
        @upload="onFiles"
      />
    </div>
    <VcDataTable
      v-model:selection="selection"
      selection-mode="multiple"
      :items="rows"
      state-key="CONTRACT_ATTACHMENTS"
      :row-actions="rowActions"
      row-actions-mode="dropdown"
      row-actions-position="column"
      :empty-state="{ icon: 'lucide-paperclip', title: $t('CONTRACTS.ATTACHMENTS.EMPTY') }"
      @row-click="(e: { data: Row }) => download(e.data)"
    >
      <VcColumn
        id="icon"
        title=""
        width="56px"
      >
        <template #body="{ data }">
          <VcIcon :icon="/\.(png|jpe?g|gif|svg|webp)$/i.test(data.url ?? '') ? 'lucide-image' : 'lucide-file-text'" />
        </template>
      </VcColumn>
      <VcColumn
        id="name"
        :title="$t('CONTRACTS.FIELDS.NAME')"
        mobile-role="title"
        always-visible
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { notification, useBlade, useBladeForm, usePermissions, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcFileUpload, VcIcon } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { clone, errorMessage, uploadAttachment } from "../api";
import type { Contract, ContractAttachment } from "../../api_client/virtocommerce.contracts";

defineBlade({ name: "ContractAttachments" });

type Row = ContractAttachment & { id: string };

const { t } = useI18n({ useScope: "global" });
const { options, callParent, closeSelf } = useBlade<{ contractId: string; code: string; name?: string }>();
const { showConfirmation } = usePopup();
const { hasAccess } = usePermissions();

const list = ref<ContractAttachment[]>([]);
// table rows need an id: new (unsaved) attachments are keyed by their url
const rows = computed<Row[]>(() => list.value.map((a) => ({ ...a, id: a.id ?? a.url! })));
const selection = ref<Row[]>([]);
const showUpload = ref(false);
const uploading = ref(false);
const form = useBladeForm({ data: list, closeConfirmMessage: computed(() => t("CONTRACTS.ALERTS.CLOSE_CONFIRMATION")) });

onMounted(async () => {
  const contract = await callParent<Contract>("getEntity");
  list.value = clone(contract?.attachments ?? []);
  form.setBaseline();
});

async function onFiles(files: FileList) {
  uploading.value = true;
  try {
    // in parallel; the files that uploaded are kept when one fails
    const results = await Promise.allSettled(Array.from(files).map((file) => uploadAttachment(file, options.value!.code)));
    for (const r of results) if (r.status === "fulfilled") list.value.push(...r.value.map((x) => ({ name: x.name, url: x.url })));
    const failed = results.find((r) => r.status === "rejected");
    if (failed) throw failed.reason;
    showUpload.value = false;
  } catch (e) {
    notification.error(errorMessage(e), { timeout: 5000 });
  } finally {
    uploading.value = false;
  }
}

const remove = (selected: Row[]) => {
  const keys = new Set(selected.map((x) => x.id));
  list.value = list.value.filter((a) => !keys.has(a.id ?? a.url!));
  selection.value = [];
};

async function confirmRemove(selected: Row[]) {
  if (await showConfirmation(t("CONTRACTS.ALERTS.DELETE_ATTACHMENTS", { count: selected.length }))) remove(selected);
}

const download = (a: Row) => window.open(a.url, "_blank");

async function copyLink(a: Row) {
  await navigator.clipboard.writeText(a.url ?? "");
  notification.success(t("CONTRACTS.ALERTS.COPIED"), { timeout: 2500 });
}

const rowActions = (a: Row) => [
  { id: "download", icon: "lucide-download", title: t("CONTRACTS.ATTACHMENTS.DOWNLOAD"), type: "info" as const, clickHandler: () => download(a) },
  { id: "copy", icon: "lucide-link", title: t("CONTRACTS.ATTACHMENTS.COPY_LINK"), type: "info" as const, clickHandler: () => copyLink(a) },
  {
    id: "delete",
    icon: "lucide-trash-2",
    title: t("CONTRACTS.TOOLBAR.DELETE"),
    type: "danger" as const,
    variant: "danger" as const,
    hidden: !hasAccess("Contract:delete"),
    clickHandler: () => confirmRemove([a]),
  },
];

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "apply",
    title: t("CONTRACTS.TOOLBAR.APPLY"),
    icon: "lucide-check",
    disabled: computed(() => !form.isModified.value),
    async clickHandler() {
      await callParent("setAttachments", clone(list.value));
      form.setBaseline();
      await closeSelf();
    },
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
    title: t("CONTRACTS.ATTACHMENTS.UPLOAD"),
    icon: "lucide-upload",
    permissions: "Contract:create",
    clickHandler: () => (showUpload.value = !showUpload.value),
  },
  {
    id: "delete",
    title: t("CONTRACTS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "Contract:delete",
    disabled: computed(() => !selection.value.length),
    clickHandler: () => confirmRemove(selection.value),
  },
]);
</script>

<style lang="scss">
.contracts-upload {
  padding: 16px 20px 8px;
}
</style>
