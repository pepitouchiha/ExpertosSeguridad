import { maintenanceRequestsApi } from '@/lib/api/client';
import { ErrorMessage } from '@/components/ui/Feedback';

/**
 * Dashboard counters. Rendered on the server from the API aggregate, so the numbers always
 * reflect persisted data rather than the rows currently loaded in the table.
 */
export async function SummaryCards() {
  let summary;
  try {
    summary = await maintenanceRequestsApi.getSummary();
  } catch {
    return <ErrorMessage title="No fue posible cargar los indicadores." />;
  }

  const cards = [
    { label: 'Total', value: summary.total, accent: 'text-slate-900' },
    { label: 'Pendientes', value: summary.pending, accent: 'text-amber-600' },
    { label: 'En progreso', value: summary.inProgress, accent: 'text-blue-600' },
    { label: 'En espera', value: summary.onHold, accent: 'text-slate-600' },
    { label: 'Resueltas', value: summary.resolved, accent: 'text-emerald-600' },
    { label: 'Canceladas', value: summary.cancelled, accent: 'text-red-600' },
  ];

  return (
    <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
      {cards.map((card) => (
        <div key={card.label} className="card px-4 py-3">
          <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">{card.label}</dt>
          <dd className={`mt-1 text-2xl font-semibold tabular-nums ${card.accent}`}>{card.value}</dd>
        </div>
      ))}
    </dl>
  );
}
