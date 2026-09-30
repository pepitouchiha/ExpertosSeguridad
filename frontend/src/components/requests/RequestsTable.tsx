import Link from 'next/link';
import { PriorityBadge, StatusBadge } from '@/components/ui/Badge';
import { EmptyState } from '@/components/ui/Feedback';
import type { MaintenanceRequestListItem } from '@/lib/api/types';
import { categoryLabels, formatDateTime } from '@/lib/labels';

/**
 * Renders the current page of results. On small screens the table collapses into cards,
 * which keeps every column readable without horizontal scrolling.
 */
export function RequestsTable({ items }: { items: MaintenanceRequestListItem[] }) {
  if (items.length === 0) {
    return (
      <EmptyState
        title="No hay solicitudes que coincidan con los criterios."
        description="Ajuste los filtros o registre una nueva solicitud."
      />
    );
  }

  return (
    <>
      <table className="hidden w-full text-left text-sm md:table">
        <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">Título</th>
            <th scope="col" className="px-4 py-3 font-medium">Categoría</th>
            <th scope="col" className="px-4 py-3 font-medium">Prioridad</th>
            <th scope="col" className="px-4 py-3 font-medium">Estado</th>
            <th scope="col" className="px-4 py-3 font-medium">Responsable</th>
            <th scope="col" className="px-4 py-3 font-medium">Creación</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {items.map((item) => (
            <tr key={item.id} className="hover:bg-slate-50">
              <td className="px-4 py-3">
                <Link href={`/requests/${item.id}`} className="font-medium text-brand-700 hover:underline">
                  {item.title}
                </Link>
                <span className="block text-xs text-slate-400">{item.id.slice(0, 8)}</span>
              </td>
              <td className="px-4 py-3 text-slate-600">{categoryLabels[item.category]}</td>
              <td className="px-4 py-3"><PriorityBadge priority={item.priority} /></td>
              <td className="px-4 py-3"><StatusBadge status={item.status} /></td>
              <td className="px-4 py-3 text-slate-600">{item.responsible?.name ?? 'Sin asignar'}</td>
              <td className="px-4 py-3 text-slate-600">{formatDateTime(item.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <ul className="divide-y divide-slate-100 md:hidden">
        {items.map((item) => (
          <li key={item.id} className="p-4">
            <Link href={`/requests/${item.id}`} className="font-medium text-brand-700">
              {item.title}
            </Link>
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
          </li>
        ))}
      </ul>
    </>
  );
}
