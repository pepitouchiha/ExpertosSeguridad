export const REQUEST_STATUSES = ['Pending', 'InProgress', 'OnHold', 'Resolved', 'Cancelled'] as const;
export const REQUEST_PRIORITIES = ['Low', 'Medium', 'High', 'Critical'] as const;
export const REQUEST_CATEGORIES = ['Infrastructure', 'Equipment', 'Software', 'Other'] as const;
export const HISTORY_EVENT_TYPES = ['Created', 'StatusChanged', 'ResponsibleChanged'] as const;

export type RequestStatus = (typeof REQUEST_STATUSES)[number];
export type RequestPriority = (typeof REQUEST_PRIORITIES)[number];
export type RequestCategory = (typeof REQUEST_CATEGORIES)[number];
export type HistoryEventType = (typeof HISTORY_EVENT_TYPES)[number];
export type SortDirection = 'Asc' | 'Desc';

export interface User {
  id: string;
  name: string;
}

export interface MaintenanceRequestListItem {
  id: string;
  title: string;
  category: RequestCategory;
  priority: RequestPriority;
  status: RequestStatus;
  responsible: User | null;
  createdAt: string;
}

export interface HistoryEntry {
  id: string;
  eventType: HistoryEventType;
  previousValue: string | null;
  newValue: string | null;
  actor: User;
  occurredAt: string;
}

export interface MaintenanceRequestDetail {
  id: string;
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  status: RequestStatus;
  requester: User;
  responsible: User | null;
  createdAt: string;
  updatedAt: string;
  /** Computed by the backend from the transition policy, so the UI never restates the rule. */
  allowedNextStatuses: RequestStatus[];
  history: HistoryEntry[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface RequestSummary {
  total: number;
  pending: number;
  inProgress: number;
  onHold: number;
  resolved: number;
  cancelled: number;
}

export interface MaintenanceRequestFilters {
  page?: number;
  pageSize?: number;
  status?: RequestStatus;
  priority?: RequestPriority;
  category?: RequestCategory;
  search?: string;
  sortByCreatedAt?: SortDirection;
}

export interface CreateMaintenanceRequestPayload {
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  requesterId: string;
}
