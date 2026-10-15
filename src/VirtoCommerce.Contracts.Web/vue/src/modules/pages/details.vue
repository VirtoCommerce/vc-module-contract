<template>
  <VcBlade
    :title="title"
    :subtitle="isNew ? undefined : $t('CONTRACTS.DETAILS.SUBTITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    :modified="form.isModified.value"
    width="50%"
  >
    <VcContainer>
      <VcForm class="contracts-form tw-space-y-4">
        <VcRow class="tw-gap-4">
          <VcCol :size="2">
            <Field
              v-slot="{ errorMessage, handleChange, errors }"
              name="name"
              rules="required"
              :model-value="item.name"
            >
              <VcInput
                v-model="item.name"
                :label="$t('CONTRACTS.FIELDS.NAME')"
                :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.NAME')"
                required
                :disabled="readonly"
                :error="!!errors.length"
                :error-message="errorMessage"
                @update:model-value="handleChange"
              />
            </Field>
          </VcCol>
          <VcCol>
            <Field
              v-slot="{ errorMessage, handleChange, errors }"
              name="code"
              :rules="codeRule"
              :model-value="item.code"
            >
              <VcInput
                :model-value="item.code"
                :label="$t('CONTRACTS.FIELDS.CODE')"
                :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.CODE')"
                :disabled="!isNew"
                :error="!!errors.length"
                :error-message="errorMessage"
                @update:model-value="(v) => (onCodeTyped(v), handleChange(v))"
              />
            </Field>
          </VcCol>
          <VcCol>
            <div class="contracts-field-with-action">
              <VcSelect
                v-model="item.status"
                class="contracts-field-with-action__field"
                :label="$t('CONTRACTS.FIELDS.STATUS')"
                :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.STATUS')"
                :options="statuses"
                :disabled="readonly"
              />
              <VcButton
                v-if="hasAccess('platform:setting:update')"
                icon="lucide-pencil"
                variant="ghost"
                size="sm"
                :aria-label="$t('CONTRACTS.DETAILS.EDIT_STATUSES')"
                :title="$t('CONTRACTS.DETAILS.EDIT_STATUSES')"
                @click="editStatuses"
              />
            </div>
          </VcCol>
        </VcRow>
        <VcTextarea
          v-model="item.description"
          :label="$t('CONTRACTS.FIELDS.DESCRIPTION')"
          :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.DESCRIPTION')"
          :disabled="readonly"
        />
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcDatePicker
              v-model="item.startDate"
              :label="$t('CONTRACTS.FIELDS.START_DATE')"
              :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.START_DATE')"
              clearable
              :disabled="readonly"
            />
          </VcCol>
          <VcCol>
            <VcDatePicker
              v-model="item.endDate"
              :label="$t('CONTRACTS.FIELDS.END_DATE')"
              :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.END_DATE')"
              clearable
              :disabled="readonly"
            />
          </VcCol>
        </VcRow>
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcSelect
              v-model="item.vendorId"
              :label="$t('CONTRACTS.FIELDS.VENDOR')"
              :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.VENDOR')"
              :options="searchMemberOptions"
              option-value="id"
              option-label="name"
              searchable
              :disabled="readonly"
            />
          </VcCol>
          <VcCol>
            <Field
              v-slot="{ errorMessage, handleChange, errors }"
              name="storeId"
              rules="required"
              :model-value="item.storeId"
            >
              <VcSelect
                v-model="item.storeId"
                :label="$t('CONTRACTS.FIELDS.STORE')"
                :placeholder="$t('CONTRACTS.DETAILS.PLACEHOLDERS.STORE')"
                :options="searchStoreOptions"
                option-value="id"
                option-label="name"
                searchable
                required
                :clearable="false"
                :disabled="!isNew"
                :error="!!errors.length"
                :error-message="errorMessage"
                @update:model-value="handleChange"
              />
            </Field>
          </VcCol>
        </VcRow>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  defineBladeContext,
  useBlade,
  useBladeForm,
  useBladeWidgets,
  useFunctions,
  usePermissions,
  usePopup,
  type IBladeToolbar,
} from "@vc-shell/framework";
import {
  VcBlade,
  VcButton,
  VcCol,
  VcContainer,
  VcDatePicker,
  VcForm,
  VcInput,
  VcRow,
  VcSelect,
  VcTextarea,
} from "@vc-shell/framework/ui";
import { Field } from "vee-validate";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import {
  clone,
  codeFromName,
  errorMessage,
  isValidCode,
  searchMemberOptions,
  searchStoreOptions,
  useContractsClient,
  useMembersClient,
  usePricesClient,
} from "../api";
import { useDynamicPropertyCount, useStatuses } from "../composables";
import type { Contract, ContractAttachment } from "../../api_client/virtocommerce.contracts";

defineBlade({
  url: "/contract",
  name: "ContractDetails",
  permissions: ["Contract:read"],
});

// param: the contract id (none for a new contract)
const { t } = useI18n({ useScope: "global" });
const { param, openBlade, callParent, exposeToChildren } = useBlade();
const { showError } = usePopup();
const { hasAccess } = usePermissions();
const { debounce } = useFunctions();
const client = useContractsClient();
const membersClient = useMembersClient();
const pricesClient = usePricesClient();
const { statuses, load: loadStatuses } = useStatuses();

const isNew = computed(() => !param.value);
const readonly = computed(() => !hasAccess(isNew.value ? "Contract:create" : "Contract:update"));
const item = ref<Contract>({});
const loading = ref(false);
const membersCount = ref(0);
const pricesCount = ref(0);

const form = useBladeForm({
  data: item,
  closeConfirmMessage: computed(() => t("CONTRACTS.ALERTS.CLOSE_CONFIRMATION")),
});

const title = computed(() => (isNew.value ? t("CONTRACTS.DETAILS.NEW_TITLE") : (item.value.name ?? "")));

// the code follows the name of a new contract until the user types one (legacy debounced $watch)
const codeRule = (value: unknown) => isValidCode(value as string | undefined) || t("CONTRACTS.VALIDATION.CODE");
let codeTyped = false;
function onCodeTyped(value: unknown) {
  codeTyped = true;
  item.value.code = (value as string) || undefined;
}
watch(
  () => item.value.name,
  debounce(() => {
    if (isNew.value && !codeTyped) item.value.code = codeFromName(item.value.name);
  }, 800),
);

async function loadCounts() {
  const contractId = param.value;
  if (!contractId) return;
  const [members, prices] = await Promise.all([
    (await membersClient()).searchContractMembers({ contractId, deepSearch: true, take: 0 }).catch(() => undefined),
    (await pricesClient()).searchContractGroupedPrices({ contractId, take: 0 }).catch(() => undefined),
  ]);
  membersCount.value = members?.totalCount ?? 0;
  pricesCount.value = prices?.totalCount ?? 0;
}

// the contract as last loaded: what differs from it are the user's unsaved edits
let loaded: Record<string, unknown> = {};

async function load() {
  loading.value = true;
  const counts = loadCounts();
  try {
    item.value = isNew.value ? { attachments: [], dynamicProperties: [] } : await (await client()).getContractById(param.value!);
    item.value.attachments ??= [];
    form.setBaseline();
    loaded = clone(item.value) as Record<string, unknown>;
  } finally {
    loading.value = false;
  }
  await counts;
}

// The server saved the contract (a linked price list): re-read it and put the user's unsaved edits back on top.
// A field both changed keeps the user's value.
async function reloadKeepingEdits() {
  const current = item.value as Record<string, unknown>;
  const keys = new Set([...Object.keys(current), ...Object.keys(loaded)]);
  const edits = [...keys].filter((k) => JSON.stringify(current[k]) !== JSON.stringify(loaded[k]));
  const values = Object.fromEntries(edits.map((k) => [k, current[k]]));
  await load();
  Object.assign(item.value, values);
}

async function save() {
  loading.value = true;
  try {
    const c = await client();
    if (isNew.value) {
      const created = await c.createContract(item.value);
      form.setBaseline();
      // the list reloads and opens the created contract in place of this blade
      await callParent("created", created.id);
      return;
    }
    await c.updateContract(item.value);
    callParent("reload");
  } catch (e) {
    showError(errorMessage(e));
    return;
  } finally {
    loading.value = false;
  }
  await load();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: computed(() => (isNew.value ? t("CONTRACTS.TOOLBAR.CREATE") : t("CONTRACTS.TOOLBAR.SAVE"))),
    icon: "lucide-save",
    permissions: isNew.value ? "Contract:create" : "Contract:update",
    disabled: computed(() => !form.canSave.value),
    clickHandler: save,
  },
  {
    id: "reset",
    title: t("CONTRACTS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    permissions: "Contract:update",
    isVisible: computed(() => !isNew.value),
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
]);

const editStatuses = () =>
  openBlade({ name: "PlatformSettingDictionary", param: "Contract.Status", onClose: () => loadStatuses() });

const dynamicPropertyCount = useDynamicPropertyCount(computed(() => item.value.objectType));
const childOptions = () => ({ contractId: item.value.id, code: item.value.code, name: item.value.name });

useBladeWidgets([
  {
    id: "ContractAttachments",
    icon: "lucide-paperclip",
    title: "CONTRACTS.WIDGETS.ATTACHMENTS",
    badge: computed(() => item.value.attachments?.length ?? 0),
    isVisible: computed(() => !isNew.value),
    onClick: () => openBlade({ name: "ContractAttachments", options: childOptions() }),
  },
  {
    id: "ContractCustomers",
    icon: "lucide-users",
    title: "CONTRACTS.WIDGETS.CUSTOMERS",
    badge: membersCount,
    isVisible: computed(() => !isNew.value),
    onClick: () => openBlade({ name: "ContractMembers", options: childOptions() }),
  },
  {
    id: "ContractDynamicProperties",
    icon: "lucide-list-plus",
    title: "CONTRACTS.WIDGETS.PROPERTIES",
    badge: dynamicPropertyCount,
    isVisible: computed(() => !isNew.value && hasAccess("platform:dynamic_properties:read")),
    onClick: () => openBlade({ name: "ContractDynamicProperties" }),
  },
  {
    id: "ContractPrices",
    icon: "lucide-badge-dollar-sign",
    title: "CONTRACTS.WIDGETS.PRICES",
    badge: pricesCount,
    isVisible: computed(() => !isNew.value),
    onClick: () =>
      openBlade({ name: "ContractPrices", options: { ...childOptions(), pricelistLinked: !!item.value.basePricelistAssignmentId } }),
  },
]);

// the contract, for widgets other modules may add to this blade (registerExternalWidget targetBlades: ["ContractDetails"])
defineBladeContext({ item });

exposeToChildren({
  // the dynamic properties / attachments blades edit the contract itself (saved with Save)
  getEntity: () => item.value,
  setAttachments: (list: unknown) => (item.value.attachments = list as ContractAttachment[]),
  refreshCounts: () => loadCounts(),
  // a price list was linked: the server saved the contract with its new assignment ids, so reload it
  // (as the AngularJS widget did; keeping a stale copy would drop the link on the next Save), keeping unsaved edits
  pricelistLinked: () => (form.isModified.value ? reloadKeepingEdits() : load()),
});

onMounted(load);
</script>

<style lang="scss">
/* the design's blade body padding, shared by the contract's child blades */
.contracts-form {
  padding: 20px 22px 36px;
}

.contracts-field-with-action {
  display: flex;
  align-items: flex-end;
  gap: 4px;
  &__field {
    flex: 1;
    min-width: 0;
  }
}
</style>
