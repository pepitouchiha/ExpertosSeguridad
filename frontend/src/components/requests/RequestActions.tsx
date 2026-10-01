'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/components/auth/AuthProvider';
import { Button } from '@/components/ui/Button';
import { ErrorMessage, Spinner } from '@/components/ui/Feedback';
import { maintenanceRequestsApi } from '@/lib/api/client';
import { ResolveDialog } from './ResolveDialog';
import type { MaintenanceRequestDetail, RequestStatus, User } from '@/lib/api/types';
import { statusLabels } from '@/lib/labels';

/** Cómo se nombra cada transición como acción, en lugar de como el estado al que lleva. */
const actionLabels: Partial<Record<RequestStatus, string>> = {
  Resolved: 'Resolver solicitud',
  OnHold: 'Poner en espera',
  InProgress: 'Reanudar atención',
  Cancelled: 'Cancelar solicitud',
};

/**
 * Cancelar cierra la solicitud para siempre, así que pregunta antes de aplicarse. Resolver
 * también, pero con su propio diálogo: necesita una respuesta para el solicitante, no solo un sí.
 */
const needsConfirmation = new Set<RequestStatus>(['Cancelled']);

/**
 * Controles del personal, organizados según el flujo en lugar de como una fila de botones de estado:
 *
 * - **Responsable** (coordinación, cualquiera del personal): asignar una solicitud pendiente la
 *   inicia; una ya iniciada se puede reasignar, pero nunca dejar sin responsable.
 * - **Atención** (ejecución, solo el responsable): pausar, reanudar, resolver.
 * - **Cancelar** (coordinación, cualquiera del personal).
 *
 * Qué botones aparecen sale por completo de `availableActions`, calculado en el backend con las
 * mismas políticas que validan cada llamada. El componente nunca decide quién puede hacer qué;
 * solo decide dónde va cada acción en la pantalla.
 */
export function RequestActions({
  request,
  staff,
}: {
  request: MaintenanceRequestDetail;
  staff: User[];
}) {
  const router = useRouter();
  const { user, accessToken } = useAuth();

  const { canAssign, statusTransitions } = request.availableActions;
  const isPending = request.status === 'Pending';
  const isTerminal = request.allowedNextStatuses.length === 0;
  const isResponsible = request.responsible?.id === user.id;

  const executionActions = statusTransitions.filter((status) => status !== 'Cancelled');
  const canCancel = statusTransitions.includes('Cancelled');

  const [selectedResponsible, setSelectedResponsible] = useState(request.responsible?.id ?? '');
  const [confirming, setConfirming] = useState<RequestStatus | null>(null);
  const [isResolving, setIsResolving] = useState(false);
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const isBusy = pendingAction !== null;

  const run = async (actionKey: string, action: () => Promise<unknown>) => {
    setPendingAction(actionKey);
    setError(null);
    try {
      await action();
      setConfirming(null);
      router.refresh();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'No fue posible completar la operación.');
    } finally {
      setPendingAction(null);
    }
  };

  const changeStatus = (status: RequestStatus) =>
    run(`status-${status}`, () => maintenanceRequestsApi.changeStatus(request.id, status, accessToken));

  const assign = (responsibleId: string) =>
    run('assign', () => maintenanceRequestsApi.assignResponsible(request.id, responsibleId, accessToken));

  const requestStatusChange = (status: RequestStatus) => {
    if (status === 'Resolved') {
      setIsResolving(true);
    } else if (needsConfirmation.has(status)) {
      setConfirming(status);
    } else {
      void changeStatus(status);
    }
  };

  if (isTerminal) {
    return (
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-slate-900">Solicitud cerrada</h2>
        <p className="mt-2 text-sm text-slate-500">
          Está en estado «{statusLabels[request.status]}» y no admite más cambios.
        </p>
      </div>
    );
  }

  return (
    <div className="card divide-y divide-slate-100">
      {/* --- Responsable: coordinación --- */}
      <section className="space-y-3 p-4">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">Responsable</h2>
          <p className="mt-1 text-sm text-slate-700">
            {request.responsible ? (
              <>
                {request.responsible.name}
                {isResponsible ? <span className="ml-1 text-xs text-brand-700">(usted)</span> : null}
              </>
            ) : (
              <span className="text-slate-500">Sin asignar</span>
            )}
          </p>
        </div>

        {canAssign ? (
          <>
            <div className="flex flex-wrap items-end gap-2">
              <div className="min-w-[12rem] flex-1">
                <label className="sr-only" htmlFor="responsible">
                  {isPending ? 'Responsable a asignar' : 'Nuevo responsable'}
                </label>
                <select
                  id="responsible"
                  className="field-control"
                  value={selectedResponsible}
                  disabled={isBusy}
                  onChange={(event) => setSelectedResponsible(event.target.value)}
                >
                  {/* Solo se ofrece mientras nadie está asignado: una solicitud iniciada se puede
                      pasar a otra persona, pero no dejar sin responsable. */}
                  {isPending ? <option value="">Seleccione…</option> : null}
                  {staff.map((member) => (
                    <option key={member.id} value={member.id}>
                      {member.name}
                    </option>
                  ))}
                </select>
              </div>
              <Button
                onClick={() => assign(selectedResponsible)}
                disabled={isBusy || !selectedResponsible || selectedResponsible === request.responsible?.id}
              >
                {isPending ? 'Asignar e iniciar' : 'Reasignar'}
              </Button>
            </div>

            {!isResponsible ? (
              <button
                type="button"
                className="text-sm font-medium text-brand-700 hover:underline disabled:text-slate-400"
                disabled={isBusy}
                onClick={() => assign(user.id)}
              >
                {isPending ? 'Tomar esta solicitud' : 'Asignármela'}
              </button>
            ) : null}

            {isPending ? (
              <p className="text-xs text-slate-500">
                Al asignarla, la solicitud pasa automáticamente a «En progreso».
              </p>
            ) : null}
          </>
        ) : null}
      </section>

      {/* --- Atención: ejecución, solo el responsable --- */}
      {!isPending ? (
        <section className="space-y-3 p-4">
          <h2 className="text-sm font-semibold text-slate-900">Atención</h2>

          {executionActions.length > 0 ? (
            <div className="flex flex-col gap-2">
              {/* Resolver primero y destacado: es lo que el responsable vino a hacer. */}
              {executionActions
                .slice()
                .sort((a, b) => (a === 'Resolved' ? -1 : b === 'Resolved' ? 1 : 0))
                .map((status) => (
                  <Button
                    key={status}
                    variant={status === 'Resolved' ? 'primary' : 'secondary'}
                    disabled={isBusy || confirming !== null}
                    onClick={() => requestStatusChange(status)}
                  >
                    {actionLabels[status] ?? statusLabels[status]}
                  </Button>
                ))}
            </div>
          ) : (
            <p className="text-sm text-slate-500">
              La atención está a cargo de <strong>{request.responsible?.name}</strong>. Solo esa persona puede
              ponerla en espera, reanudarla o resolverla. Si hace falta, reasígnela.
            </p>
          )}
        </section>
      ) : null}

      {/* --- Cancelar: coordinación --- */}
      {canCancel ? (
        <section className="p-4">
          <Button
            variant="danger"
            className="w-full"
            disabled={isBusy || confirming !== null}
            onClick={() => requestStatusChange('Cancelled')}
          >
            {actionLabels.Cancelled}
          </Button>
        </section>
      ) : null}

      {confirming ? (
        <section
          role="alertdialog"
          aria-live="polite"
          className="space-y-3 bg-amber-50 p-4"
        >
          <p className="text-sm text-amber-900">
            ¿Cancelar la solicitud? Es un estado final: después no se podrá reabrir ni modificar.
          </p>
          <div className="flex gap-2">
            <Button variant="danger" disabled={isBusy} onClick={() => changeStatus(confirming)}>
              Sí, cancelar
            </Button>
            <Button variant="secondary" disabled={isBusy} onClick={() => setConfirming(null)}>
              Volver
            </Button>
          </div>
        </section>
      ) : null}

      {isBusy || error ? (
        <section className="space-y-2 p-4">
          {isBusy ? <Spinner label="Aplicando cambio…" /> : null}
          {error ? <ErrorMessage title={error} /> : null}
        </section>
      ) : null}

      <p className="px-4 py-3 text-xs text-slate-400">
        Los cambios se registran a nombre de <strong>{user.name}</strong>.
      </p>

      <ResolveDialog
        open={isResolving}
        requestId={request.id}
        requestTitle={request.title}
        onClose={() => setIsResolving(false)}
        onResolved={() => {
          setIsResolving(false);
          router.refresh();
        }}
      />
    </div>
  );
}
