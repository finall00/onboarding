export type LeadListStatus = "Pending" | "Processing" | "Completed" | "Failed";

export interface LeadList {
  id: string;
  name: string;
  sourceUrl: string;
  status: LeadListStatus;
  processedCount: number;
  errorMessage: string | null;
  createdAt: string;
  updatedAt: string;
  correlationId: string;
}

export interface CreateLeadListRequest {
  name: string;
  sourceUrl: string;
}

export interface UpdateLeadListRequest {
  name: string;
  sourceUrl: string;
}
