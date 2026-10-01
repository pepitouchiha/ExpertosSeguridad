import Link from 'next/link';
import { Fragment } from 'react';
import { PriorityBadge, StatusBadge } from '@/components/ui/Badge';
import { EmptyState } from '@/components/ui/Feedback';
import { ClickableRow } from './ClickableRow';
import type { MaintenanceRequestListItem } from '@/lib/api/types';
import { categoryLabels, formatDateTime, formatRequestNumber } from '@/lib/labels';

/**
 * Muestra la página actual de resultados. En pantallas pequeñas la tabla se convierte en
 * tarjetas, lo que mantiene legible cada columna sin desplazamiento horizontal.
 *
 * Para el personal, la API ya devuelve primero todo lo que tiene asignado; este componente solo
 * rotula los dos grupos para que el orden se explique solo. No reordena nada: el orden, y por
 * lo tanto en qué página cae cada fila, lo decide el backend.
 */
export function RequestsTable({
  items,
  currentUserId,
}: {
  items: MaintenanceRequestListItem[];
  /** Se pasa para el personal: marca las filas que tiene asignadas y rotula los dos grupos. */
  currentUserId?: string;
}) {
  if (items.length === 0) {
    return (
      <EmptyState
        title="No hay solicitudes que coincidan con los criterios."
        description="Ajuste los filtros o registre una nueva solicitud."
      />
    );
  }

  const isMine = (item: MaintenanceRequestListItem) =>
    currentUserId !== undefined && item.responsible?.id === currentUserId;

  // Encabezados de grupo solo cuando esta página contiene de verdad algo del usuario.
  const showGroups = items.some(isMine);
  const groupLabel = (index: number): string | null => {
    if (!showGroups) return null;
    if (index === 0) return isMine(items[0]) ? 'Asignadas a usted' : 'Resto de solicitudes';
    return isMine(items[index - 1]) && !isMine(items[index]) ? 'Resto de solicitudes' : null;
  };

  return (
    <>
      <table className="hidden w-full text-left text-sm md:table">
        <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">N.º</th>
            <th scope="col" className="px-4 py-3 font-medium">Título</th>
            <th scope="col" className="px-4 py-3 font-medium">Categoría</th>
            <th scope="col" className="px-4 py-3 font-medium">Prioridad</th>
            <th scope="col" className="px-4 py-3 font-medium">Estado</th>
            <th scope="col" className="px-4 py-3 font-medium">Responsable</th>
            <th scope="col" className="px-4 py-3 font-medium">Creación</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {items.map((item, index) => (
            <Fragment key={item.id}>
              {groupLabel(index) ? (
                <tr className="bg-slate-50">
                  <th
                    scope="colgroup"
                    colSpan={7}
                    className="px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-500"
                  >
                    {groupLabel(index)}
                  </th>
                </tr>
              ) : null}
              <ClickableRow
                href={`/requests/${item.id}`}
                className={isMine(item) ? 'bg-brand-50/60 hover:bg-brand-50' : 'hover:bg-slate-50'}
              >
                <td className="whitespace-nowrap px-4 py-3 text-xs font-medium text-slate-500">
                  {formatRequestNumber(item.number)}
                </td>
                <td className="px-4 py-3">
                  <Link href={`/requests/${item.id}`} className="font-medium text-brand-700 hover:underline">
                    {item.title}
                  </Link>
                </td>
                <td className="px-4 py-3 text-slate-600">{categoryLabels[item.category]}</td>
                <td className="px-4 py-3"><PriorityBadge priority={item.priority} /></td>
                <td className="px-4 py-3"><StatusBadge status={item.status} /></td>
                <td className="px-4 py-3 text-slate-600">{item.responsible?.name ?? 'Sin asignar'}</td>
                <td className="px-4 py-3 text-slate-600">{formatDateTime(item.createdAt)}</td>
              </ClickableRow>
            </Fragment>
          ))}
        </tbody>
      </table>

      <ul className="divide-y divide-slate-100 md:hidden">
        {items.map((item, index) => (
          <li key={item.id} className={isMine(item) ? 'bg-brand-50/60' : undefined}>
            {groupLabel(index) ? (
              <p className="bg-slate-50 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                {groupLabel(index)}
              </p>
            ) : null}
            {/* En móvil, toda la tarjeta es el enlace. */}
            <Link href={`/requests/${item.id}`} className="block p-4 hover:bg-slate-50">
              <span className="block text-xs font-medium text-slate-500">{formatRequestNumber(item.number)}</span>
              <span className="font-medium text-brand-700">{item.title}</span>
              <div className="mt-2 flex flex-wrap gap-2">
                <StatusBadge status={item.status} />
                <PriorityBadge priority={item.priority} />
              </div>
              <dl className="mt-3 grid grid-cols-2 gap-2 text-xs text-slate-600">
                <div>
                  <dt className="text-slate-400">Categoría</dt>
                  <dd>{categoryLabels[item.category]}</dd>
                </div>
                <div>
                  <dt className="text-slate-400">Responsable</dt>
                  <dd>{item.responsible?.name ?? 'Sin asignar'}</dd>
                </div>
                <div className="col-span-2">
                  <dt className="text-slate-400">Creación</dt>
                  <dd>{formatDateTime(item.createdAt)}</dd>
                </div>
              </dl>
            </Link>
          </li>
        ))}
      </ul>
    </>
  );
}
