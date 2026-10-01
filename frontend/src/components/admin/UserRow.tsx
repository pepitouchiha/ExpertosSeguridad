'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/components/auth/AuthProvider';
import { ActiveBadge, RoleBadge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { ErrorMessage, Spinner } from '@/components/ui/Feedback';
import { adminApi } from '@/lib/api/client';
import type { UserRole, UserSummary } from '@/lib/api/types';
import { USER_ROLES } from '@/lib/api/types';
import { formatDateTime, roleLabels } from '@/lib/labels';

type Confirming = null | 'role' | 'deactivate';

/**
 * Una cuenta con sus controles. Ambas acciones terminan la sesión actual de la persona afectada,
 * así que ninguna se aplica con un solo clic: elegir un rol solo lo propone, y desactivar vuelve a
 * preguntar. Activar no pide confirmación: no le quita nada a nadie.
 *
 * La fila del propio administrador es de solo lectura. La API rechaza esos cambios de todos modos
 * (un administrador no puede degradarse ni desactivarse); la interfaz simplemente no los ofrece.
 */
export function UserRow({ user }: { user: UserSummary }) {
  const router = useRouter();
  const { user: currentUser, accessToken } = useAuth();

  const [selectedRole, setSelectedRole] = useState<UserRole>(user.role);
  const [confirming, setConfirming] = useState<Confirming>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isSelf = user.id === currentUser.id;

  const run = async (action: () => Promise<unknown>) => {
    setIsBusy(true);
    setError(null);
    try {
      await action();
      setConfirming(null);
      router.refresh();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'No fue posible aplicar el cambio.');
    } finally {
      setIsBusy(false);
    }
  };

  const cancel = () => {
    setSelectedRole(user.role);
    setConfirming(null);
    setError(null);
  };

  return (
    <li className="px-4 py-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-[14rem] flex-1">
          <p className="font-medium text-slate-900">
            {user.name}
            {isSelf ? <span className="ml-2 text-xs font-normal text-slate-400">(usted)</span> : null}
          </p>
          <p className="text-sm text-slate-500">{user.email}</p>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <RoleBadge role={user.role} />
            <ActiveBadge isActive={user.isActive} />
            <span className="text-xs text-slate-400">Alta: {formatDateTime(user.createdAt)}</span>
          </div>
        </div>

        {isSelf ? (
          <p className="max-w-xs text-xs text-slate-400">
            No puede cambiar su propio rol ni desactivar su propia cuenta. Así la plataforma nunca queda sin
            administrador.
          </p>
        ) : (
          <div className="flex flex-wrap items-end gap-2">
            <div>
              <label className="sr-only" htmlFor={`role-${user.id}`}>
                Rol de {user.name}
              </label>
              <select
                id={`role-${user.id}`}
                className="field-control w-auto"
                value={selectedRole}
                disabled={isBusy || confirming === 'deactivate'}
                onChange={(event) => {
                  const role = event.target.value as UserRole;
                  setSelectedRole(role);
                  setConfirming(role === user.role ? null : 'role');
                }}
              >
                {USER_ROLES.map((role) => (
                  <option key={role} value={role}>
                    {roleLabels[role]}
                  </option>
                ))}
              </select>
            </div>

            {user.isActive ? (
              <Button
                variant="danger"
                disabled={isBusy || confirming !== null}
                onClick={() => setConfirming('deactivate')}
              >
                Desactivar
              </Button>
            ) : (
              <Button
                variant="secondary"
                disabled={isBusy || confirming !== null}
                onClick={() => run(() => adminApi.changeStatus(user.id, true, accessToken))}
              >
                Activar
              </Button>
            )}
          </div>
        )}
      </div>

      {confirming ? (
        <div
          role="alertdialog"
          aria-live="polite"
          className="mt-3 flex flex-wrap items-center gap-3 rounded-md border border-amber-200 bg-amber-50 px-3 py-2.5"
        >
          <p className="flex-1 text-sm text-amber-900">
            {confirming === 'role' ? (
              <>
                ¿Cambiar el rol de <strong>{user.name}</strong> de {roleLabels[user.role]} a{' '}
                <strong>{roleLabels[selectedRole]}</strong>? Su sesión actual se cerrará.
              </>
            ) : (
              <>
                ¿Desactivar la cuenta de <strong>{user.name}</strong>? No podrá iniciar sesión y su sesión actual se
                cerrará. Su historial se conserva.
              </>
            )}
          </p>
          <div className="flex gap-2">
            <Button
              variant={confirming === 'deactivate' ? 'danger' : 'primary'}
              disabled={isBusy}
              onClick={() =>
                run(() =>
                  confirming === 'role'
                    ? adminApi.changeRole(user.id, selectedRole, accessToken)
                    : adminApi.changeStatus(user.id, false, accessToken),
                )
              }
            >
              Confirmar
            </Button>
            <Button variant="secondary" disabled={isBusy} onClick={cancel}>
              Cancelar
            </Button>
          </div>
        </div>
      ) : null}

      {isBusy ? (
        <div className="mt-2">
          <Spinner label="Aplicando cambio…" />
        </div>
      ) : null}
      {error ? (
        <div className="mt-2">
          <ErrorMessage title={error} />
        </div>
      ) : null}
    </li>
  );
}
