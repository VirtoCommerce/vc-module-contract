<template>
  <VcBlade
    :title="isNew ? $t('CONTRACTS.FILTER.NEW_TITLE') : filter.name"
    :subtitle="isNew ? $t('CONTRACTS.FILTER.NEW_SUBTITLE') : $t('CONTRACTS.FILTER.SUBTITLE')"
    :toolbar-items="bladeToolbar"
    width="40%"
  >
    <VcContainer>
      <VcForm class="contracts-form tw-space-y-4">
        <Field
          v-slot="{ errorMessage, handleChange, errors }"
          name="name"
          rules="required"
          :model-value="filter.name"
        >
          <VcInput
            v-model="filter.name"
            :label="$t('CONTRACTS.FILTER.NAME')"
            required
            :error="!!errors.length"
            :error-message="errorMessage"
            @update:model-value="handleChange"
          />
        </Field>
        <VcSelect
          v-model="filter.statuses"
          :label="$t('CONTRACTS.FIELDS.STATUS')"
          :placeholder="$t('CONTRACTS.FILTER.ALL')"
          :options="statuses"
          multiple
        />
        <VcSelect
          v-model="filter.vendorId"
          :label="$t('CONTRACTS.FIELDS.VENDOR')"
          :placeholder="$t('CONTRACTS.FILTER.ALL')"
          :options="searchMemberOptions"
          option-value="id"
          option-label="name"
          searchable
        />
        <VcSelect
          v-model="filter.storeIds"
          :label="$t('CONTRACTS.FIELDS.STORE')"
          :placeholder="$t('CONTRACTS.FILTER.ALL')"
          :options="searchStoreOptions"
          option-value="id"
          option-label="name"
          multiple
          searchable
        />
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcInput
              v-model="filter.activeStartDate"
              type="date"
              :label="$t('CONTRACTS.FILTER.FROM')"
            />
          </VcCol>
          <VcCol>
            <VcInput
              v-model="filter.activeEndDate"
              type="date"
              :label="$t('CONTRACTS.FILTER.TO')"
            />
          </VcCol>
        </VcRow>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
// filter-detail: a named, saved contract filter (stored in this browser, like the AngularJS $localStorage filters)
import { useBlade, useBladeForm, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcCol, VcContainer, VcForm, VcInput, VcRow, VcSelect } from "@vc-shell/framework/ui";
import { Field } from "vee-validate";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { clone, searchMemberOptions, searchStoreOptions } from "../api";
import { useSavedFilters, useStatuses, type SavedContractFilter } from "../composables";

defineBlade({ name: "ContractFilterDetails" });

const { t } = useI18n({ useScope: "global" });
const { options, callParent, closeSelf } = useBlade<{ filterId?: string }>();
const { filters, currentId, find } = useSavedFilters();
const { statuses } = useStatuses();

const existing = () => find(filter.value?.id ?? options.value?.filterId);
const isNew = computed(() => !existing());
// dates are edited as yyyy-mm-dd, stored as ISO strings of the local midnight (as the contract's own dates are)
const localDay = (iso?: string) => {
  if (!iso) return undefined;
  const d = new Date(iso);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 10);
};
const toEdit = (f: SavedContractFilter): SavedContractFilter => ({
  ...clone(f),
  activeStartDate: localDay(f.activeStartDate),
  activeEndDate: localDay(f.activeEndDate),
});
const initial = find(options.value?.filterId);
const filter = ref<SavedContractFilter>(initial ? toEdit(initial) : { id: String(Date.now()), name: t("CONTRACTS.FILTER.UNNAMED") });
const form = useBladeForm({ data: filter });
form.setBaseline();

// "yyyy-mm-dd" alone parses as UTC midnight; with a time it parses as local
const isoDate = (v?: string) => (v ? new Date(`${v}T00:00`).toISOString() : undefined);

async function apply() {
  const value = { ...clone(filter.value), activeStartDate: isoDate(filter.value.activeStartDate), activeEndDate: isoDate(filter.value.activeEndDate) };
  const index = filters.value.findIndex((f) => f.id === value.id);
  if (index >= 0) filters.value.splice(index, 1, value);
  else filters.value.push(value);
  currentId.value = value.id;
  form.setBaseline();
  await callParent("reloadFirstPage");
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "apply",
    title: t("CONTRACTS.FILTER.APPLY"),
    icon: "lucide-filter",
    disabled: computed(() => !form.formMeta.value.valid || !filter.value.name),
    clickHandler: apply,
  },
  {
    id: "reset",
    title: t("CONTRACTS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => {
      if (existing()) filter.value = toEdit(existing()!);
      else form.revert();
      form.setBaseline();
    },
  },
  {
    id: "delete",
    title: t("CONTRACTS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    isVisible: computed(() => !isNew.value),
    async clickHandler() {
      filters.value = filters.value.filter((f) => f.id !== filter.value.id);
      if (currentId.value === filter.value.id) currentId.value = undefined;
      form.setBaseline();
      await callParent("reloadFirstPage");
      await closeSelf();
    },
  },
]);
</script>
