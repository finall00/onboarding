import type {
  LeadList,
  CreateLeadListRequest,
  UpdateLeadListRequest,
} from "../model/leadlist";

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

  async function parseErrorBody(res: Response): Promise<unknown> {
    const contentType = res.headers.get("content-type") || "";
    try {
      return contentType.includes("application/json") ? await res.json() : await res.text();
    } catch {
      return undefined;
    }
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
        const body = await parseErrorBody(res);
        throw new Error(`Failed to fetch lead lists: ${res.status} ${res.statusText} - ${JSON.stringify(body)}`);
      }

      const json = await res.json();
      const items = Array.isArray(json) ? json : json.items ?? json.data ?? json.leadLists;
      if (!Array.isArray(items)) throw new Error("Invalid response from API: expected array of lead lists");
      const total = json.total ?? json.totalCount;

      return { items: items as LeadList[], total };
    },

    async getById(id: string) {
      const res = await fetch(`${BASE}/lead-lists/${id}`);
      if (res.status === 404) return null;
      if (!res.ok) throw new Error(`Failed to fetch lead list: ${res.status}`);
      const json = await res.json();
      return json as LeadList;
    },

    async create(request: CreateLeadListRequest) {
      const res = await fetch(`${BASE}/lead-lists`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: request.name, sourceUrl: request.sourceUrl }),
      });
      if (!res.ok) {
        const body = await parseErrorBody(res);
        throw new HttpError(`Failed to create lead list: ${res.status}`, res.status, body);
      }
      const json = await res.json();
      forceReload();
      return json as LeadList;
    },

    async update(id: string, request: UpdateLeadListRequest) {
      const url = `${BASE}/lead-lists/${id}`;
      const res = await fetch(url, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: request.name, sourceUrl: request.sourceUrl }),
      });
      if (!res.ok) {
        const body = await parseErrorBody(res);
        throw new HttpError(`Failed to update lead list: ${res.status}`, res.status, body);
      }
      const json = await res.json();
      forceReload();
      return json as LeadList;
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
      return json as LeadList;
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
