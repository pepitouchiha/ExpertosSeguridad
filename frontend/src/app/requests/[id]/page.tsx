import Link from 'next/link';
import { notFound } from 'next/navigation';
import { HistoryTimeline } from '@/components/requests/HistoryTimeline';
import { RequestActions } from '@/components/requests/RequestActions';
import { PriorityBadge, StatusBadge } from '@/components/ui/Badge';
import { ErrorMessage } from '@/components/ui/Feedback';
import { ApiError, maintenanceRequestsApi } from '@/lib/api/client';
import type { MaintenanceRequestDetail } from '@/lib/api/types';
import { categoryLabels, formatDateTime } from '@/lib/labels';

export default async function RequestDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  let request: MaintenanceRequestDetail;
  try {
    request = await maintenanceRequestsApi.getById(id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }

    return (
      <ErrorMessage title="No fue posible cargar la solicitud.">
        {error instanceof Error ? error.message : null}
      </ErrorMessage>
    );
  }

  return (
    <div className="space-y-5">
      <div>
        <Link href="/" className="text-sm text-brand-700 hover:underline">
          ← Volver al panel
        </Link>
        <h1 className="mt-2 text-xl font-semibold text-slate-900">{request.title}</h1>
        <div className="mt-2 flex flex-wrap items-center gap-2">
          <StatusBadge status={request.status} />
          <PriorityBadge priority={request.priority} />
          <span className="text-xs text-slate-400">{request.id}</span>
        </div>
      </div>

      <div className="grid gap-5 lg:grid-cols-3">
        <div className="space-y-5 lg:col-span-2">
          <section className="card p-4 sm:p-6">
            <h2 className="text-sm font-semibold text-slate-900">Descripción</h2>
            <p className="mt-2 whitespace-pre-line text-sm text-slate-700">{request.description}</p>

            <dl className="mt-5 grid grid-cols-2 gap-4 border-t border-slate-100 pt-4 text-sm sm:grid-cols-3">
              <div>
                <dt className="text-xs uppercase tracking-wide text-slate-400">Categoría</dt>
                <dd className="mt-0.5 text-slate-700">{categoryLabels[request.category]}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-slate-400">Solicitante</dt>
                <dd className="mt-0.5 text-slate-700">{request.requester.name}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-slate-400">Responsable</dt>
                <dd className="mt-0.5 text-slate-700">{request.responsible?.name ?? 'Sin asignar'}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-slate-400">Creación</dt>
                <dd className="mt-0.5 text-slate-700">{formatDateTime(request.createdAt)}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-slate-400">Última actualización</dt>
                <dd className="mt-0.5 text-slate-700">{formatDateTime(request.updatedAt)}</dd>
              </div>
            </dl>
          </section>

          <section className="card p-4 sm:p-6">
            <h2 className="text-sm font-semibold text-slate-900">Historial de actividad</h2>
            <p className="mb-4 text-xs text-slate-400">Ordenado cronológicamente.</p>
            <HistoryTimeline history={request.history} />
          </section>
        </div>

        <div className="lg:col-span-1">
          <RequestActions request={request} />
        </div>
      </div>
    </div>
  );
}
