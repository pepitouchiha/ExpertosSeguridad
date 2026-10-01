import type {
  AuthResult,
  AuthenticatedUser,
  CreateMaintenanceRequestPayload,
  MaintenanceRequestDetail,
  MaintenanceRequestFilters,
  MaintenanceRequestListItem,
  PagedResult,
  RequestStatus,
  RegisterPayload,
  RequestSummary,
  ResolveRequestPayload,
  User,
  UserFilters,
  UserRole,
  UserSummary,
} from './types';

/**
 * A dónde va la aplicación cuando la API rechaza un token que antes aceptaba: la cuenta se
 * desactivó o cambió su rol. La ruta /logout borra la cookie y lleva al login con una explicación.
 */
export const SESSION_EXPIRED_PATH = '/logout?reason=session';

/**
 * Único lugar donde la aplicación habla con la API. Los componentes nunca llaman a fetch
 * directamente, así que la URL base, el token Bearer y la forma de los errores se resuelven una vez.
 *
 * Los Server Components corren dentro del contenedor y llegan a la API por el nombre del
 * servicio, mientras que el navegador llega por el puerto del host; por eso hay dos variables.
 */
const SERVER_BASE_URL = process.env.API_INTERNAL_URL ?? 'http://localhost:8080';
const BROWSER_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080';

const baseUrl = () => (typeof window === 'undefined' ? SERVER_BASE_URL : BROWSER_BASE_URL);

/** Cuerpo Problem Details que devuelve el manejador centralizado de errores de la API. */
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  /** Mensajes por campo, presentes cuando la API rechazó el cuerpo con un 400. */
  readonly fieldErrors: Record<string, string[]>;

  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

/**
 * El token se pasa explícitamente en lugar de leerse de una variable global: los Server
 * Components lo toman de las cookies y los componentes de cliente del contexto de autenticación,
 * y ninguno de los dos módulos se puede importar en el entorno del otro.
 */
async function request<T>(path: string, init: RequestInit & { token?: string | null } = {}): Promise<T> {
  const { token, headers, ...rest } = init;

  let response: Response;
  try {
    response = await fetch(`${baseUrl()}${path}`, {
      ...rest,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...headers,
      },
      cache: 'no-store',
    });
  } catch {
    throw new ApiError('No fue posible contactar el servidor. Verifique que la API esté disponible.', 0);
  }

  // Un 401 en una llamada que llevaba token significa que la sesión ya no es válida. En el
  // navegador la aplicación sale de inmediato; en el servidor, las páginas revisan el código y se
  // redirigen solas (ver redirectIfSessionEnded), porque una función de librería no puede redirigir
  // un render. Las llamadas sin token (el propio login) conservan su 401 para que el formulario lo muestre.
  if (response.status === 401 && token && typeof window !== 'undefined') {
    // Navegación completa a propósito: /logout es un route handler que borra la cookie en el servidor.
    // eslint-disable-next-line @next/next/no-location-assign-relative-destination
    window.location.assign(SESSION_EXPIRED_PATH);
  }

  if (!response.ok) {
    let problem: ProblemDetails = {};
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      // Un cuerpo de error que no es JSON deja el mensaje genérico de abajo.
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

export const authApi = {
  login: (email: string, password: string) =>
    request<AuthResult>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  register: (payload: RegisterPayload) =>
    request<AuthResult>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  me: (token: string) => request<AuthenticatedUser>('/api/auth/me', { token }),
};

export const maintenanceRequestsApi = {
  search: (filters: MaintenanceRequestFilters, token: string) =>
    request<PagedResult<MaintenanceRequestListItem>>(
      `/api/maintenance-requests${buildQueryString(filters)}`,
      { token },
    ),

  getSummary: (token: string) => request<RequestSummary>('/api/maintenance-requests/summary', { token }),

  getById: (id: string, token: string) =>
    request<MaintenanceRequestDetail>(`/api/maintenance-requests/${id}`, { token }),

  create: (payload: CreateMaintenanceRequestPayload, token: string) =>
    request<MaintenanceRequestDetail>('/api/maintenance-requests', {
      method: 'POST',
      body: JSON.stringify(payload),
      token,
    }),

  changeStatus: (id: string, newStatus: RequestStatus, token: string) =>
    request<MaintenanceRequestDetail>(`/api/maintenance-requests/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ newStatus }),
      token,
    }),

  resolve: (id: string, payload: ResolveRequestPayload, token: string) =>
    request<MaintenanceRequestDetail>(`/api/maintenance-requests/${id}/resolution`, {
      method: 'POST',
      body: JSON.stringify(payload),
      token,
    }),

  assignResponsible: (id: string, responsibleId: string | null, token: string) =>
    request<MaintenanceRequestDetail>(`/api/maintenance-requests/${id}/responsible`, {
      method: 'PATCH',
      body: JSON.stringify({ responsibleId }),
      token,
    }),
};

export const usersApi = {
  /** Solo personal; un solicitante recibe 403 y nunca necesita esta lista. */
  getStaff: (token: string) => request<User[]>('/api/users/staff', { token }),
};

function buildUserQueryString(filters: UserFilters): string {
  const params = new URLSearchParams();

  if (filters.page) params.set('page', String(filters.page));
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize));
  if (filters.role) params.set('role', filters.role);
  if (filters.isActive !== undefined) params.set('isActive', String(filters.isActive));
  if (filters.search) params.set('search', filters.search);

  const queryString = params.toString();
  return queryString ? `?${queryString}` : '';
}

/** Panel de administración. Cada llamada responde 403 a quien no sea administrador. */
export const adminApi = {
  searchUsers: (filters: UserFilters, token: string) =>
    request<PagedResult<UserSummary>>(`/api/admin/users${buildUserQueryString(filters)}`, { token }),

  changeRole: (id: string, role: UserRole, token: string) =>
    request<UserSummary>(`/api/admin/users/${id}/role`, {
      method: 'PATCH',
      body: JSON.stringify({ role }),
      token,
    }),

  changeStatus: (id: string, isActive: boolean, token: string) =>
    request<UserSummary>(`/api/admin/users/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ isActive }),
      token,
    }),
};
