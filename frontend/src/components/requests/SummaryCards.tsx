import { maintenanceRequestsApi } from '@/lib/api/client';
import { ErrorMessage } from '@/components/ui/Feedback';
import { redirectIfSessionEnded } from '@/lib/auth/serverSession';

/**
 * Contadores del panel. Se muestran en el servidor a partir del agregado de la API, así que los
 * números siempre reflejan los datos persistidos y no las filas cargadas en la tabla.
 *
 * Solo los ven el personal y el administrador; el alcance lo decide el backend a partir del
 * token, y el componente no necesita saberlo.
 */
export async function SummaryCards({ token }: { token: string }) {
  let summary;
  try {
    summary = await maintenanceRequestsApi.getSummary(token);
  } catch (error) {
    redirectIfSessionEnded(error);

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
