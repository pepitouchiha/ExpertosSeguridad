import { StatusBadge } from '@/components/ui/Badge';
import type { MaintenanceRequestDetail } from '@/lib/api/types';
import { formatDateTime } from '@/lib/labels';

type Audience = 'requester' | 'supervisor';

const copy: Record<Audience, { title: string; open: string }> = {
  requester: {
    title: 'Estado de su solicitud',
    open: 'El personal de Expertos Seguridad actualizará el estado a medida que avance la atención. Cada cambio queda registrado en el historial.',
  },
  supervisor: {
    title: 'Estado de la solicitud',
    open: 'Vista de supervisión: la gestión del estado y la asignación corresponden al personal de la empresa.',
  },
};

/**
 * Lo que ven todos menos el personal en lugar de los controles: el estado actual y quién la
 * atiende. De solo lectura por diseño: avanzar el ciclo de vida es una operación del personal,
 * y la API la rechaza para solicitantes y administradores sin importar lo que muestre la interfaz.
 */
export function RequestStatusPanel({
  request,
  audience = 'requester',
}: {
  request: MaintenanceRequestDetail;
  audience?: Audience;
}) {
  const isTerminal = request.allowedNextStatuses.length === 0;
  const text = copy[audience];

  return (
    <div className="card space-y-4 p-4">
      <div>
        <h2 className="text-sm font-semibold text-slate-900">{text.title}</h2>
        <div className="mt-2">
          <StatusBadge status={request.status} />
        </div>
      </div>

      <div className="border-t border-slate-100 pt-4">
        <h3 className="text-xs uppercase tracking-wide text-slate-400">Responsable asignado</h3>
        <p className="mt-1 text-sm text-slate-700">
          {request.responsible?.name ?? 'Aún sin asignar'}
        </p>
      </div>

      <div className="border-t border-slate-100 pt-4">
        <h3 className="text-xs uppercase tracking-wide text-slate-400">Última actualización</h3>
        <p className="mt-1 text-sm text-slate-700">{formatDateTime(request.updatedAt)}</p>
      </div>

      <p className="border-t border-slate-100 pt-4 text-xs text-slate-500">
        {isTerminal
          ? 'La solicitud está cerrada y no admite más cambios.'
          : text.open}
      </p>
    </div>
  );
}
