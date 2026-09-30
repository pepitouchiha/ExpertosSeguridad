'use client';

import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useTransition } from 'react';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Feedback';
import { REQUEST_CATEGORIES, REQUEST_PRIORITIES, REQUEST_STATUSES } from '@/lib/api/types';
import { categoryLabels, priorityLabels, statusLabels } from '@/lib/labels';

/**
 * Filters live in the URL, not in component state: the server page reads them and asks the
 * API for exactly that page of data, so filtering and sorting really happen in the backend
 * and every view is shareable and reloadable.
 */
export function RequestFilters() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  // The URL is the single source of truth for the criteria. The search box is uncontrolled
  // and keyed by the active term, so navigating (or clearing the filters) resets it without
  // mirroring the URL into component state.
  const activeSearch = searchParams.get('search') ?? '';

  const applyParam = (key: string, value: string) => {
    const params = new URLSearchParams(searchParams.toString());

    if (value) {
      params.set(key, value);
    } else {
      params.delete(key);
    }

    // Any change to the criteria invalidates the current page number.
    params.delete('page');

    startTransition(() => router.push(`${pathname}?${params.toString()}`));
  };

  const hasFilters = ['status', 'priority', 'category', 'search'].some((key) => searchParams.get(key));

  return (
    <form
      className="card p-4"
      onSubmit={(event) => {
        event.preventDefault();
        const term = new FormData(event.currentTarget).get('search');
        applyParam('search', typeof term === 'string' ? term.trim() : '');
      }}
    >
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <div className="lg:col-span-2">
          <label className="field-label" htmlFor="search">
            Buscar por título
          </label>
          <input
            key={activeSearch}
            id="search"
            name="search"
            type="search"
            defaultValue={activeSearch}
            placeholder="Ej: fuga de agua"
            className="field-control"
          />
        </div>

        <div>
          <label className="field-label" htmlFor="status">
            Estado
          </label>
          <select
            id="status"
            className="field-control"
            value={searchParams.get('status') ?? ''}
            onChange={(event) => applyParam('status', event.target.value)}
          >
            <option value="">Todos</option>
            {REQUEST_STATUSES.map((status) => (
              <option key={status} value={status}>
                {statusLabels[status]}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="field-label" htmlFor="priority">
            Prioridad
          </label>
          <select
            id="priority"
            className="field-control"
            value={searchParams.get('priority') ?? ''}
            onChange={(event) => applyParam('priority', event.target.value)}
          >
            <option value="">Todas</option>
            {REQUEST_PRIORITIES.map((priority) => (
              <option key={priority} value={priority}>
                {priorityLabels[priority]}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="field-label" htmlFor="category">
            Categoría
          </label>
          <select
            id="category"
            className="field-control"
            value={searchParams.get('category') ?? ''}
            onChange={(event) => applyParam('category', event.target.value)}
          >
            <option value="">Todas</option>
            {REQUEST_CATEGORIES.map((category) => (
              <option key={category} value={category}>
                {categoryLabels[category]}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <Button type="submit">Buscar</Button>

        <Button
          type="button"
          variant="secondary"
          onClick={() =>
            applyParam('sortByCreatedAt', searchParams.get('sortByCreatedAt') === 'Asc' ? 'Desc' : 'Asc')
          }
        >
          Fecha: {searchParams.get('sortByCreatedAt') === 'Asc' ? 'más antiguas primero' : 'más recientes primero'}
        </Button>

        {hasFilters ? (
          <Button type="button" variant="secondary" onClick={() => startTransition(() => router.push(pathname))}>
            Limpiar filtros
          </Button>
        ) : null}

        {isPending ? <Spinner label="Actualizando…" /> : null}
      </div>
    </form>
  );
}
