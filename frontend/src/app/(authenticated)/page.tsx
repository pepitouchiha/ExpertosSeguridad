import Link from 'next/link';
import { Suspense } from 'react';
import { Pagination } from '@/components/ui/Pagination';
import { RequestFilters } from '@/components/requests/RequestFilters';
import { RequestsTable } from '@/components/requests/RequestsTable';
import { SummaryCards } from '@/components/requests/SummaryCards';
import { ErrorMessage, SkeletonRows } from '@/components/ui/Feedback';
import { maintenanceRequestsApi } from '@/lib/api/client';
import { getServerSession, redirectIfSessionEnded } from '@/lib/auth/serverSession';
import type {
  MaintenanceRequestFilters,
  RequestCategory,
  RequestPriority,
  RequestStatus,
  SortDirection,
} from '@/lib/api/types';
import { REQUEST_CATEGORIES, REQUEST_PRIORITIES, REQUEST_STATUSES } from '@/lib/api/types';

type SearchParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined): string | undefined =>
  Array.isArray(value) ? value[0] : value;

/** Solo se reenvían valores que la API conoce, para que una URL editada a mano no rompa la petición. */
function parseFilters(searchParams: SearchParams): MaintenanceRequestFilters {
  const status = first(searchParams.status);
  const priority = first(searchParams.priority);
  const category = first(searchParams.category);
  const sort = first(searchParams.sortByCreatedAt);
  const page = Number(first(searchParams.page) ?? '1');

  return {
    page: Number.isFinite(page) && page > 0 ? page : 1,
    pageSize: 10,
    status: REQUEST_STATUSES.includes(status as RequestStatus) ? (status as RequestStatus) : undefined,
    priority: REQUEST_PRIORITIES.includes(priority as RequestPriority) ? (priority as RequestPriority) : undefined,
    category: REQUEST_CATEGORIES.includes(category as RequestCategory) ? (category as RequestCategory) : undefined,
    search: first(searchParams.search) || undefined,
    sortByCreatedAt: sort === 'Asc' ? 'Asc' : ('Desc' as SortDirection),
  };
}

async function RequestsList({
  filters,
  token,
  currentUserId,
}: {
  filters: MaintenanceRequestFilters;
  token: string;
  currentUserId?: string;
}) {
  let result;
  try {
    result = await maintenanceRequestsApi.search(filters, token);
  } catch (error) {
    redirectIfSessionEnded(error);

    return (
      <div className="p-4">
        <ErrorMessage title="No fue posible cargar las solicitudes.">
          {error instanceof Error ? error.message : null}
        </ErrorMessage>
      </div>
    );
  }

  const baseParams = new URLSearchParams();
  if (filters.status) baseParams.set('status', filters.status);
  if (filters.priority) baseParams.set('priority', filters.priority);
  if (filters.category) baseParams.set('category', filters.category);
  if (filters.search) baseParams.set('search', filters.search);
  if (filters.sortByCreatedAt) baseParams.set('sortByCreatedAt', filters.sortByCreatedAt);

  return (
    <>
      <RequestsTable items={result.items} currentUserId={currentUserId} />
      <Pagination
        page={result.page}
        totalPages={result.totalPages}
        totalItems={result.totalItems}
        hasPreviousPage={result.hasPreviousPage}
        hasNextPage={result.hasNextPage}
        baseParams={baseParams}
      />
    </>
  );
}

export default async function DashboardPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  // El layout de arriba ya garantizó que hay sesión; aquí se lee para las llamadas a la API.
  const session = (await getServerSession())!;
  const resolvedParams = await searchParams;
  const filters = parseFilters(resolvedParams);

  const role = session.user.role;

  // La misma página sirve a tres públicos; solo cambian los textos y la acción de crear.
  // Lo que cada uno puede *ver* lo decide la API a partir del token, no esta página.
  const heading = {
    Requester: {
      title: 'Mis solicitudes',
      subtitle: 'Seguimiento de las solicitudes que usted ha registrado.',
    },
    Staff: {
      title: 'Panel de solicitudes',
      subtitle: 'Primero las asignadas a usted; después, el resto de solicitudes.',
    },
    Admin: {
      title: 'Supervisión de solicitudes',
      subtitle: 'Vista de solo lectura de todas las solicitudes. La atención corresponde al personal.',
    },
  }[role];

  // La clave crea un límite de Suspense nuevo por cada combinación de filtros, para que el
  // esqueleto vuelva a aparecer en cada navegación en vez de dejar la página anterior en pantalla.
  const suspenseKey = JSON.stringify(filters);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-slate-900">{heading.title}</h1>
          <p className="text-sm text-slate-500">{heading.subtitle}</p>
        </div>

        {/* Solo los solicitantes abren solicitudes: el personal las atiende y los administradores las supervisan. */}
        {role === 'Requester' ? (
          <Link
            href="/requests/new"
            className="rounded-md bg-brand-600 px-3.5 py-2 text-sm font-medium text-white hover:bg-brand-700"
          >
            Nueva solicitud
          </Link>
        ) : null}
      </div>

      {/* Los indicadores son una vista operativa para quien atiende o supervisa solicitudes; un
          solicitante sigue sus propias solicitudes en el listado de abajo. */}
      {role !== 'Requester' ? (
        <Suspense fallback={<div className="h-20 animate-pulse rounded-lg bg-slate-100" />}>
          <SummaryCards token={session.accessToken} />
        </Suspense>
      ) : null}

      <RequestFilters />

      <section className="card overflow-hidden">
        <Suspense key={suspenseKey} fallback={<SkeletonRows />}>
          {/* Solo el personal atiende solicitudes, así que solo a ellos se les agrupa primero lo suyo. */}
          <RequestsList
            filters={filters}
            token={session.accessToken}
            currentUserId={role === 'Staff' ? session.user.id : undefined}
          />
        </Suspense>
      </section>
    </div>
  );
}
