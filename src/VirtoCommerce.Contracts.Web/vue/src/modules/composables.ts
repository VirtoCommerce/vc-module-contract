import { ref, watch, type Ref } from "vue";
import { DynamicPropertiesClient, useApiClient, type DynamicProperty } from "@vc-shell/framework";
import { http } from "./api";

// Saved contract filters of the AngularJS list ($localStorage.contractSearchFilters), shared by the list and filter blades.
export interface SavedContractFilter {
  id: string;
  name: string;
  statuses?: string[];
  vendorId?: string;
  storeIds?: string[];
  activeStartDate?: string;
  activeEndDate?: string;
}

const KEY = "vc-contracts.savedFilters";
const CURRENT_KEY = "vc-contracts.savedFilterId";

function read<T>(key: string, fallback: T): T {
  try {
    return (JSON.parse(localStorage.getItem(key) ?? "") as T) ?? fallback;
  } catch {
    return fallback;
  }
}

const filters = ref<SavedContractFilter[]>(read(KEY, []));
const currentId = ref<string | undefined>(read<string | undefined>(CURRENT_KEY, undefined));
watch(filters, (v) => localStorage.setItem(KEY, JSON.stringify(v)), { deep: true });
watch(currentId, (v) => (v ? localStorage.setItem(CURRENT_KEY, JSON.stringify(v)) : localStorage.removeItem(CURRENT_KEY)));

export function useSavedFilters() {
  const find = (id?: string) => (id ? filters.value.find((f) => f.id === id) : undefined);
  return { filters, currentId, find };
}

/** Allowed values of the Contract.Status dictionary setting as select options; `load` re-reads them. */
export function useStatuses() {
  const statuses = ref<{ id: string; title: string }[]>([]);
  // settings/values needs only a signed-in user (legacy settings.getValues), unlike settings/{name}
  const load = () =>
    http<unknown[]>("/api/platform/settings/values/Contract.Status")
      .then((values) => (statuses.value = (values ?? []).map((x) => ({ id: String(x), title: String(x) }))))
      .catch(() => (statuses.value = []));
  load();
  return { statuses, load };
}

// dynamic property definitions per object type, shared by the widget's counter and the properties blade
const definitions = new Map<string, Promise<DynamicProperty[]>>();

export function useDynamicPropertyDefinitions() {
  const { getApiClient } = useApiClient(DynamicPropertiesClient);
  return (objectType: string, refresh = false) => {
    if (refresh || !definitions.has(objectType)) {
      definitions.set(
        objectType,
        getApiClient()
          .then((c) => c.searchDynamicProperties({ objectType, take: 1000 }))
          .then((r) => r.results ?? [])
          .catch((e) => {
            definitions.delete(objectType);
            throw e;
          }),
      );
    }
    return definitions.get(objectType)!;
  };
}

/** Number of dynamic properties defined for the entity's type (what the legacy widget shows). */
export function useDynamicPropertyCount(objectType: Ref<string | undefined>) {
  const load = useDynamicPropertyDefinitions();
  const count = ref(0);
  watch(
    objectType,
    async (type) => {
      count.value = type ? (await load(type).catch(() => [])).length : 0;
    },
    { immediate: true },
  );
  return count;
}
