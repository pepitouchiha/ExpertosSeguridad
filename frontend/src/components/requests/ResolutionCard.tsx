import type { Resolution } from '@/lib/api/types';
import { formatDateTime } from '@/lib/labels';

/**
 * La respuesta del responsable, visible para todos los que pueden leer la solicitud, sobre todo
 * para el solicitante.
 */
export function ResolutionCard({ resolution }: { resolution: Resolution }) {
  return (
    <section className="card border-emerald-200 bg-emerald-50/40 p-4 sm:p-6">
      <h2 className="text-sm font-semibold text-emerald-900">Respuesta del responsable</h2>
      <p className="mt-2 font-medium text-slate-900">{resolution.title}</p>
      <p className="mt-1 whitespace-pre-line text-sm text-slate-700">{resolution.description}</p>
      <p className="mt-3 text-xs text-slate-500">
        {resolution.respondedBy.name} · {formatDateTime(resolution.respondedAt)}
      </p>
    </section>
  );
}
