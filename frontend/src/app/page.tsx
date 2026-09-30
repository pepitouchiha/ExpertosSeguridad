import Link from 'next/link';
import { Suspense } from 'react';
import { Pagination } from '@/components/requests/Pagination';
import { RequestFilters } from '@/components/requests/RequestFilters';
import { RequestsTable } from '@/components/requests/RequestsTable';
import { SummaryCards } from '@/components/requests/SummaryCards';
import { ErrorMessage, SkeletonRows } from '@/components/ui/Feedback';
import { maintenanceRequestsApi } from '@/lib/api/client';
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

/** Only values the API knows are forwarded, so a hand-edited URL cannot break the request. */
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

async function RequestsList({ filters }: { filters: MaintenanceRequestFilters }) {
  let result;
  try {
    result = await maintenanceRequestsApi.search(filters);
  } catch (error) {
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
      <RequestsTable items={result.items} />
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
  const resolvedParams = await searchParams;
  const filters = parseFilters(resolvedParams);

  // The key forces a new Suspense boundary per filter combination, so the skeleton shows
  // again on every navigation instead of leaving the previous page on screen.
  const suspenseKey = JSON.stringify(filters);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-slate-900">Panel de solicitudes</h1>
          <p className="text-sm text-slate-500">Seguimiento de las solicitudes de mantenimiento registradas.</p>
        </div>
        <Link
          href="/requests/new"
          className="rounded-md bg-brand-600 px-3.5 py-2 text-sm font-medium text-white hover:bg-brand-700"
        >
          Nueva solicitud
        </Link>
      </div>

      <Suspense fallback={<div className="h-20 animate-pulse rounded-lg bg-slate-100" />}>
        <SummaryCards />
      </Suspense>

      <RequestFilters />

      <section className="card overflow-hidden">
        <Suspense key={suspenseKey} fallback={<SkeletonRows />}>
          <RequestsList filters={filters} />
        </Suspense>
      </section>
    </div>
  );
}
