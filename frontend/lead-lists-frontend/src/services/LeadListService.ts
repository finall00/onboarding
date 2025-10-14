import type {
  LeadList,
  CreateLeadListRequest,
  UpdateLeadListRequest,
} from "../lib/leadlist";

const BASE = (import.meta.env.VITE_API as string) || (import.meta.env.VITE_API_BASE as string) || "http://localhost:8080";

  class HttpError extends Error {
    status?: number;
    body?: unknown;
    constructor(message: string, status?: number, body?: unknown) {
      super(message);
      this.status = status;
      this.body = body;
    }
  }

  let _forceReloadToken = 0;
  function forceReload() {
    _forceReloadToken += 1;
  }

  function mapApiToLeadList(api: unknown): LeadList {
    const a = api as Record<string, unknown>;
    return {
      id: String(a.id),
      name: String(a.name ?? ""),
      sourceUrl: String(a.sourceUrl ?? ""),
      status: (a.status as LeadList["status"]) || "Pending",
      processedCount: Number(a.processedCount ?? 0),
      errorMessage: a.errorMessage === undefined || a.errorMessage === null ? null : String(a.errorMessage),
      createdAt: String(a.createdAt ?? new Date().toISOString()),
      updatedAt: String(a.updatedAt ?? new Date().toISOString()),
      correlationId: String(a.correlationId ?? ""),
    } as LeadList;
  }

  export const leadListService = {
    async getAll(filters?: { status?: string; search?: string; page?: number; pageSize?: number }) {
      const params = new URLSearchParams();
      if (filters?.status) params.set("status", filters.status);
      if (filters?.search) params.set("q", filters.search);
      if (typeof filters?.page === "number") params.set("page", String(filters.page));
      if (typeof filters?.pageSize === "number") params.set("pageSize", String(filters.pageSize));

      const res = await fetch(`${BASE}/lead-lists?${params.toString()}`);
      if (!res.ok) {
        const contentType = res.headers.get("content-type") || "";
        let bodyText = "";
        try {
          if (contentType.includes("application/json")) {
            const errJson = await res.json();
            bodyText = JSON.stringify(errJson);
          } else {
            bodyText = await res.text();
          }
        } catch {
          bodyText = "(unable to parse error body)";
        }
        throw new Error(`Failed to fetch lead lists: ${res.status} ${res.statusText} - ${bodyText}`);
      }

      const json = (await res.json()) as unknown;
      let items: unknown[] | null = null;
      if (Array.isArray(json)) items = json;
      else if (json && typeof json === "object") {
        const j = json as Record<string, unknown>;
        if (Array.isArray(j.items)) items = j.items as unknown[];
        else if (Array.isArray(j.data)) items = j.data as unknown[];
        else if (Array.isArray(j.leadLists)) items = j.leadLists as unknown[];
      }

      if (!items) throw new Error("Invalid response from API: expected array or { items|data|leadLists }");
      const mapped = items.map(mapApiToLeadList);

      let total: number | undefined = undefined;
      if (json && typeof json === "object") {
        const j = json as Record<string, unknown>;
        if (typeof j.total === "number") total = j.total as number;
        else if (typeof j.totalCount === "number") total = j.totalCount as number;
      }

      return { items: mapped, total };
    },

    async getById(id: string) {
      const res = await fetch(`${BASE}/lead-lists/${id}`);
      if (res.status === 404) return null;
      if (!res.ok) throw new Error(`Failed to fetch lead list: ${res.status}`);
      const json = await res.json();
      return mapApiToLeadList(json);
    },

    async create(request: CreateLeadListRequest) {
      const res = await fetch(`${BASE}/lead-lists`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: request.name, sourceUrl: request.sourceUrl }),
      });
      if (!res.ok) {
        const contentType = res.headers.get("content-type") || "";
        let body: unknown = undefined;
        try {
          if (contentType.includes("application/json")) body = await res.json();
          else body = await res.text();
        } catch (parseErr) {
          console.warn("Failed to parse error body", parseErr);
        }
        throw new HttpError(`Failed to create lead list: ${res.status}`, res.status, body);
      }
      const json = await res.json();
      forceReload();
      return mapApiToLeadList(json);
    },

    async update(id: string, request: UpdateLeadListRequest) {
      const url = `${BASE}/lead-lists/${id}`;
      const res = await fetch(url, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: request.name, sourceUrl: request.sourceUrl }),
      });
      if (!res.ok) {
        const contentType = res.headers.get("content-type") || "";
        let body: unknown = undefined;
        try {
          if (contentType.includes("application/json")) body = await res.json();
          else body = await res.text();
        } catch (parseErr) {
          console.warn("Failed to parse error body", parseErr);
        }
        throw new HttpError(`Failed to update lead list: ${res.status}`, res.status, body);
      }
      const json = await res.json();
      forceReload();
      return mapApiToLeadList(json);
    },

    async delete(id: string) {
      const res = await fetch(`${BASE}/lead-lists/${id}`, { method: "DELETE" });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(`Failed to delete lead list: ${res.status} ${text}`);
      }
      forceReload();
    },

    async reprocess(id: string) {
      const res = await fetch(`${BASE}/lead-lists/${id}/reprocess`, { method: "POST" });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(`Failed to reprocess lead list: ${res.status} ${text}`);
      }
      const json = await res.json();
      forceReload();
      return mapApiToLeadList(json);
    },

    canEdit(leadList: LeadList): boolean {
      return leadList.status === "Pending" || leadList.status === "Failed";
    },

    canDelete(leadList: LeadList): boolean {
      return leadList.status === "Pending" || leadList.status === "Failed";
    },
  };

  export const leadListCache = {
    forceReload: forceReload,
    get currentToken() {
      return _forceReloadToken;
    },
  };
