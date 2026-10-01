export const REQUEST_STATUSES = ['Pending', 'InProgress', 'OnHold', 'Resolved', 'Cancelled'] as const;
export const REQUEST_PRIORITIES = ['Low', 'Medium', 'High', 'Critical'] as const;
export const REQUEST_CATEGORIES = ['Infrastructure', 'Equipment', 'Software', 'Other'] as const;
export const HISTORY_EVENT_TYPES = ['Created', 'StatusChanged', 'ResponsibleChanged'] as const;

export type RequestStatus = (typeof REQUEST_STATUSES)[number];
export type RequestPriority = (typeof REQUEST_PRIORITIES)[number];
export type RequestCategory = (typeof REQUEST_CATEGORIES)[number];
export type HistoryEventType = (typeof HISTORY_EVENT_TYPES)[number];
export type SortDirection = 'Asc' | 'Desc';

/**
 * Requester: reporta y sigue sus propias solicitudes. Staff: atiende todas las solicitudes.
 * Admin: gestiona cuentas y roles, y supervisa las solicitudes sin operarlas.
 */
export const USER_ROLES = ['Requester', 'Staff', 'Admin'] as const;
export type UserRole = (typeof USER_ROLES)[number];

export interface User {
  id: string;
  name: string;
}

export interface AuthenticatedUser {
  id: string;
  name: string;
  email: string;
  role: UserRole;
}

export interface AuthResult {
  accessToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

/** Lo que el navegador conserva entre cargas de página. Refleja la respuesta del login. */
export interface Session {
  accessToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

export interface MaintenanceRequestListItem {
  id: string;
  /** Número secuencial legible, que se muestra en lugar del Guid (ver formatRequestNumber). */
  number: number;
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
  number: number;
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  status: RequestStatus;
  requester: User;
  responsible: User | null;
  createdAt: string;
  updatedAt: string;
  /** Lo calcula el backend a partir de la política de transiciones, así que la interfaz nunca repite la regla. */
  allowedNextStatuses: RequestStatus[];
  /**
   * Lo que el usuario de la sesión puede hacer con esta solicitud ahora, calculado en el backend
   * con las mismas políticas que validan cada llamada. Los botones se dibujan a partir de esto,
   * nunca a partir del rol ni de una copia local de las reglas.
   */
  availableActions: RequestActions;
  /** La respuesta del responsable al solicitante; existe una vez resuelta la solicitud. */
  resolution: Resolution | null;
  history: HistoryEntry[];
}

export interface Resolution {
  title: string;
  description: string;
  respondedBy: User;
  respondedAt: string;
}

/** No hay campo para quién responde: la API registra al responsable autenticado. */
export interface ResolveRequestPayload {
  title: string;
  description: string;
}

export interface RequestActions {
  canAssign: boolean;
  statusTransitions: RequestStatus[];
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

/** No hay campo de solicitante: la API lo toma del usuario autenticado. */
export interface CreateMaintenanceRequestPayload {
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
}

/** No hay campo de rol: toda cuenta creada por registro es de solicitante. */
export interface RegisterPayload {
  fullName: string;
  email: string;
  password: string;
}

/** Una cuenta tal como la lista el panel de administración. */
export interface UserSummary {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface UserFilters {
  page?: number;
  pageSize?: number;
  role?: UserRole;
  isActive?: boolean;
  search?: string;
}
