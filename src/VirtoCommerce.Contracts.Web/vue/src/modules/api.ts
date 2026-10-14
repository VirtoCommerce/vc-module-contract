import { useApiClient } from "@vc-shell/framework";
import {
  ContractClient,
  ContractMembersClient,
  ContractPricesClient,
  type MergedPrice,
} from "../api_client/virtocommerce.contracts";

export const useContractsClient = () => useApiClient(ContractClient).getApiClient;
export const useMembersClient = () => useApiClient(ContractMembersClient).getApiClient;
export const usePricesClient = () => useApiClient(ContractPricesClient).getApiClient;

// Other modules' endpoints these blades read (members, stores, price lists, products, assets): a few calls,
// so plain same-origin fetches instead of generating their whole API clients.
export async function http<T>(url: string, body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method: body === undefined ? "GET" : "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}: ${url}`);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/** The server's validation message of a failed generated-client call ({ message } in a 400), or its text. */
export function errorMessage(e: unknown): string {
  const raw = (e as { response?: string })?.response ?? (e as Error)?.message ?? String(e);
  try {
    return (JSON.parse(raw) as { message?: string }).message ?? raw;
  } catch {
    return raw;
  }
}

export interface SearchResult<T> {
  results: T[];
  totalCount: number;
}

export interface Named {
  id: string;
  name: string;
  memberType?: string;
  groups?: string[];
  iconUrl?: string;
  currency?: string;
}

// VcSelect async option sources: (keyword, skip, ids) => { results, totalCount }
export async function searchMemberOptions(keyword?: string, skip = 0, ids?: string[]): Promise<SearchResult<Named>> {
  const r = await http<Partial<SearchResult<Named>>>("/api/members/search", {
    deepSearch: true,
    sort: "name",
    keyword: ids?.length ? undefined : keyword,
    objectIds: ids?.length ? ids : undefined,
    skip,
    take: ids?.length || 20,
  });
  return { results: r.results ?? [], totalCount: r.totalCount ?? 0 };
}

export async function searchStoreOptions(keyword?: string, skip = 0, ids?: string[]): Promise<SearchResult<Named>> {
  const r = await http<Partial<SearchResult<Named>>>("/api/stores/search", {
    keyword: ids?.length ? undefined : keyword,
    objectIds: ids?.length ? ids : undefined,
    skip,
    take: ids?.length || 50,
  });
  return { results: r.results ?? [], totalCount: r.totalCount ?? 0 };
}

/** Members picker: the organization's members (or the top level), or a deep keyword search. */
export async function searchMembers(criteria: { keyword?: string; memberId?: string; sort?: string; skip: number; take: number }) {
  const r = await http<Partial<SearchResult<Named>>>("/api/members/search", {
    ...criteria,
    deepSearch: !!criteria.keyword,
    objectType: "Member",
    responseGroup: "WithGroups",
  });
  return { results: r.results ?? [], totalCount: r.totalCount ?? 0 };
}

export async function searchPricelists(keyword: string | undefined, skip: number, take: number, sort?: string) {
  const q = new URLSearchParams({ responseGroup: "NoDetails", skip: String(skip), take: String(take) });
  if (keyword) q.set("keyword", keyword);
  if (sort) q.set("sort", sort);
  const r = await http<Partial<SearchResult<Named>>>(`/api/pricing/pricelists?${q}`);
  return { results: r.results ?? [], totalCount: r.totalCount ?? 0 };
}

export interface ProductEntry {
  id: string;
  name: string;
  code?: string;
  imageUrl?: string;
}

export async function searchProducts(keyword: string | undefined, skip: number, take: number) {
  const r = await http<{ listEntries?: ProductEntry[]; totalCount?: number }>("/api/catalog/listentries", {
    keyword,
    objectTypes: ["CatalogProduct"],
    searchInChildren: true,
    searchInVariations: true,
    skip,
    take,
  });
  return { results: r.listEntries ?? [], totalCount: r.totalCount ?? 0 };
}

/** Uploads a file into the contract's asset folder (legacy: api/assets?folderUrl=contracts/{code}). */
export async function uploadAttachment(file: File, code: string) {
  const body = new FormData();
  body.append("file", file, file.name);
  const res = await fetch(`/api/assets?folderUrl=${encodeURIComponent(`contracts/${code}`)}`, { method: "POST", body });
  if (!res.ok) throw new Error(`${file.name}: ${res.status} ${res.statusText}`);
  return (await res.json()) as { name: string; url: string; size?: number; contentType?: string }[];
}

/** Customer blades by member type (owned by the Customer module UI). */
export const MEMBER_BLADES: Record<string, { blade: string; icon: string }> = {
  Organization: { blade: "CustomerOrganizationDetails", icon: "lucide-building-2" },
  Contact: { blade: "CustomerContactDetails", icon: "lucide-user" },
  Employee: { blade: "CustomerEmployeeDetails", icon: "lucide-id-card" },
  Vendor: { blade: "CustomerVendorDetails", icon: "lucide-briefcase" },
};

// --- contract code (legacy contract-detail.js) -------------------------------------------------------------

/** "contract-" + the name in lower case, non-word runs as hyphens. */
export const codeFromName = (name?: string) => {
  const trimmed = name?.trim();
  return trimmed ? `contract-${trimmed.toLowerCase().replace(/\W+/g, "-")}` : undefined;
};
export const isValidCode = (code?: string) => !code || !/[^\w_-]/.test(code);

// --- price rules (legacy pricing priceValidatorsService) ----------------------------------------------------

const empty = (v: unknown) => v === undefined || v === null || v === "";

/** Validation message for one contract price among all prices of the product, or "" when valid. */
export function priceError(p: MergedPrice, all: MergedPrice[], t: (key: string) => string): string {
  if (empty(p.list) || Number(p.list) < 0) return t("CONTRACTS.VALIDATION.LIST");
  if (!empty(p.sale) && Number(p.sale) > Number(p.list)) return t("CONTRACTS.VALIDATION.SALE");
  const qty = Math.round(Number(p.minQuantity));
  if (!(qty > 0)) return t("CONTRACTS.VALIDATION.QTY");
  if (all.some((x) => x !== p && Math.round(Number(x.minQuantity)) === qty)) return t("CONTRACTS.VALIDATION.QTY_UNIQUE");
  if (!all.some((x) => Math.round(Number(x.minQuantity)) === 1)) return t("CONTRACTS.VALIDATION.SINGLE_ITEM");
  return "";
}

/** A deep copy of plain (or reactive) JSON data. */
export const clone = <T>(v: T): T => JSON.parse(JSON.stringify(v)) as T;
