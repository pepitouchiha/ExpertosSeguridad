'use client';

import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useTransition } from 'react';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Feedback';
import { USER_ROLES } from '@/lib/api/types';
import { roleLabels } from '@/lib/labels';

/**
 * El mismo enfoque que los filtros de solicitudes: los criterios viven en la URL y la página
 * del servidor le pide a la API exactamente esa página, así que el filtrado ocurre en el backend.
 */
export function UserFilters() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const activeSearch = searchParams.get('search') ?? '';

  const applyParam = (key: string, value: string) => {
    const params = new URLSearchParams(searchParams.toString());

    if (value) {
      params.set(key, value);
    } else {
      params.delete(key);
    }

    params.delete('page');
    startTransition(() => router.push(`${pathname}?${params.toString()}`));
  };

  const hasFilters = ['role', 'isActive', 'search'].some((key) => searchParams.get(key));

  return (
    <form
      className="card p-4"
      onSubmit={(event) => {
        event.preventDefault();
        const term = new FormData(event.currentTarget).get('search');
        applyParam('search', typeof term === 'string' ? term.trim() : '');
      }}
    >
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div className="lg:col-span-2">
          <label className="field-label" htmlFor="user-search">
            Buscar por nombre o correo
          </label>
          <input
            key={activeSearch}
            id="user-search"
            name="search"
            type="search"
            defaultValue={activeSearch}
            placeholder="Ej: torres o @cliente.com"
            className="field-control"
          />
        </div>

        <div>
          <label className="field-label" htmlFor="user-role">
            Rol
          </label>
          <select
            id="user-role"
            className="field-control"
            value={searchParams.get('role') ?? ''}
            onChange={(event) => applyParam('role', event.target.value)}
          >
            <option value="">Todos</option>
            {USER_ROLES.map((role) => (
              <option key={role} value={role}>
                {roleLabels[role]}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="field-label" htmlFor="user-status">
            Estado de la cuenta
          </label>
          <select
            id="user-status"
            className="field-control"
            value={searchParams.get('isActive') ?? ''}
            onChange={(event) => applyParam('isActive', event.target.value)}
          >
            <option value="">Todas</option>
            <option value="true">Activas</option>
            <option value="false">Desactivadas</option>
          </select>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <Button type="submit">Buscar</Button>

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
