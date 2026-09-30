import type {
  CreateMaintenanceRequestPayload,
  MaintenanceRequestDetail,
  MaintenanceRequestFilters,
  MaintenanceRequestListItem,
  PagedResult,
  RequestStatus,
  RequestSummary,
  User,
} from './types';

/**
 * Single place where the app talks to the API. Components never call fetch directly, so the
 * base URL, the actor header and the error shape are handled once.
 *
 * Server Components run inside the container and reach the API by its service name, while the
 * browser reaches it through the host port — hence two variables.
 */
const SERVER_BASE_URL = process.env.API_INTERNAL_URL ?? 'http://localhost:8080';
const BROWSER_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080';

const baseUrl = () => (typeof window === 'undefined' ? SERVER_BASE_URL : BROWSER_BASE_URL);

/** Problem Details payload returned by the API's centralised error handler. */
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  /** Field-level messages, present when the API rejected the payload with a 400. */
  readonly fieldErrors: Record<string, string[]>;

  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

async function request<T>(path: string, init: RequestInit & { actorId?: string } = {}): Promise<T> {
  const { actorId, headers, ...rest } = init;

  let response: Response;
  try {
    response = await fetch(`${baseUrl()}${path}`, {
      ...rest,
      headers: {
        'Content-Type': 'application/json',
        ...(actorId ? { 'X-Actor-Id': actorId } : {}),
        ...headers,
      },
      cache: 'no-store',
    });
  } catch {
    throw new ApiError('No fue posible contactar el servidor. Verifique que la API esté disponible.', 0);
  }

  if (!response.ok) {
    let problem: ProblemDetails = {};
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      // A non-JSON error body leaves the generic message below.
    }

    throw new ApiError(
      problem.detail ?? problem.title ?? 'Ocurrió un error al procesar la solicitud.',
      response.status,
      problem.errors ?? {},
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function buildQueryString(filters: MaintenanceRequestFilters): string {
  const params = new URLSearchParams();

  if (filters.page) params.set('page', String(filters.page));
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize));
  if (filters.status) params.set('status', filters.status);
  if (filters.priority) params.set('priority', filters.priority);
  if (filters.category) params.set('category', filters.category);
  if (filters.search) params.set('search', filters.search);
  if (filters.sortByCreatedAt) params.set('sortByCreatedAt', filters.sortByCreatedAt);

  const queryString = params.toString();
  return queryString ? `?${queryString}` : '';
}

export const maintenanceRequestsApi = {
  search: (filters: MaintenanceRequestFilters) =>
    request<PagedResult<MaintenanceRequestListItem>>(
      `/api/maintenance-requests${buildQueryString(filters)}`,
    ),

  getSummary: () => request<RequestSummary>('/api/maintenance-requests/summary'),

  getById: (id: string) => request<MaintenanceRequestDetail>(`/api/maintenance-requests/${id}`),

  create: (payload: CreateMaintenanceRequestPayload, actorId: string) =>
    request<MaintenanceRequestDetail>('/api/maintenance-requests', {
      method: 'POST',
      body: JSON.stringify(payload),
      actorId,
    }),

  changeStatus: (id: string, newStatus: RequestStatus, actorId: string) =>
    request<MaintenanceRequestDetail>(`/api/maintenance-requests/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ newStatus }),
      actorId,
    }),

  assignResponsible: (id: string, responsibleId: string | null, actorId: string) =>
    request<MaintenanceRequestDetail>(`/api/maintenance-requests/${id}/responsible`, {
      method: 'PATCH',
      body: JSON.stringify({ responsibleId }),
      actorId,
    }),
};

export const usersApi = {
  getAll: () => request<User[]>('/api/users'),
};
