'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useActor } from '@/components/actor/ActorProvider';
import { Button } from '@/components/ui/Button';
import { ErrorMessage, Spinner } from '@/components/ui/Feedback';
import { maintenanceRequestsApi } from '@/lib/api/client';
import type { MaintenanceRequestDetail, RequestStatus } from '@/lib/api/types';
import { statusLabels } from '@/lib/labels';

/**
 * Status and assignment controls. The available transitions come from
 * `allowedNextStatuses`, computed by the backend policy: the UI hides what is not allowed
 * without ever restating the rule, and the server validates it again on every call.
 */
export function RequestActions({ request }: { request: MaintenanceRequestDetail }) {
  const router = useRouter();
  const { actor, users } = useActor();

  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedResponsible, setSelectedResponsible] = useState(request.responsible?.id ?? '');

  const isTerminal = request.allowedNextStatuses.length === 0;

  const run = async (actionKey: string, action: () => Promise<unknown>) => {
    setPendingAction(actionKey);
    setError(null);
    try {
      await action();
      router.refresh();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'No fue posible completar la operación.');
    } finally {
      setPendingAction(null);
    }
  };

  const changeStatus = (status: RequestStatus) =>
    run(`status-${status}`, () => maintenanceRequestsApi.changeStatus(request.id, status, actor.id));

  const assignResponsible = () =>
    run('responsible', () =>
      maintenanceRequestsApi.assignResponsible(request.id, selectedResponsible || null, actor.id),
    );

  const isBusy = pendingAction !== null;
  const responsibleUnchanged = (request.responsible?.id ?? '') === selectedResponsible;

  return (
    <div className="card space-y-5 p-4">
      <div>
        <h2 className="text-sm font-semibold text-slate-900">Cambiar estado</h2>

        {isTerminal ? (
          <p className="mt-2 text-sm text-slate-500">
            La solicitud está en estado «{statusLabels[request.status]}» y no admite más cambios.
          </p>
        ) : (
          <div className="mt-2 flex flex-wrap gap-2">
            {request.allowedNextStatuses.map((status) => (
              <Button
                key={status}
                variant={status === 'Cancelled' ? 'danger' : 'secondary'}
                disabled={isBusy}
                onClick={() => changeStatus(status)}
              >
                {statusLabels[status]}
              </Button>
            ))}
          </div>
        )}
      </div>

      <div className="border-t border-slate-100 pt-4">
        <h2 className="text-sm font-semibold text-slate-900">Responsable</h2>

        {isTerminal ? (
          <p className="mt-2 text-sm text-slate-500">
            No se puede modificar el responsable de una solicitud cerrada.
          </p>
        ) : (
          <div className="mt-2 flex flex-wrap items-end gap-2">
            <div className="min-w-[12rem] flex-1">
              <label className="sr-only" htmlFor="responsible">
                Responsable asignado
              </label>
              <select
                id="responsible"
                className="field-control"
                value={selectedResponsible}
                disabled={isBusy}
                onChange={(event) => setSelectedResponsible(event.target.value)}
              >
                <option value="">Sin asignar</option>
                {users.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.name}
                  </option>
                ))}
              </select>
            </div>
            <Button onClick={assignResponsible} disabled={isBusy || responsibleUnchanged}>
              Guardar
            </Button>
          </div>
        )}
      </div>

      {isBusy ? <Spinner label="Aplicando cambio…" /> : null}
      {error ? <ErrorMessage title={error} /> : null}

      <p className="text-xs text-slate-400">
        Los cambios se registran a nombre de <strong>{actor?.name}</strong>.
      </p>
    </div>
  );
}
