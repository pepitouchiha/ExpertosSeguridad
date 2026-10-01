import type {
  HistoryEventType,
  RequestCategory,
  RequestPriority,
  RequestStatus,
  UserRole,
} from './api/types';

/**
 * La API habla el lenguaje del dominio (nombres de enum en inglés) y la interfaz el del usuario.
 * Tener la traducción en un solo mapa evita repartir textos por todos los componentes.
 */
export const statusLabels: Record<RequestStatus, string> = {
  Pending: 'Pendiente',
  InProgress: 'En progreso',
  OnHold: 'En espera',
  Resolved: 'Resuelta',
  Cancelled: 'Cancelada',
};

export const priorityLabels: Record<RequestPriority, string> = {
  Low: 'Baja',
  Medium: 'Media',
  High: 'Alta',
  Critical: 'Crítica',
};

export const categoryLabels: Record<RequestCategory, string> = {
  Infrastructure: 'Infraestructura',
  Equipment: 'Equipos',
  Software: 'Software',
  Other: 'Otros',
};

export const roleLabels: Record<UserRole, string> = {
  Requester: 'Solicitante',
  Staff: 'Personal Expertos Seguridad',
  Admin: 'Administrador',
};

export const historyEventLabels: Record<HistoryEventType, string> = {
  Created: 'Solicitud creada',
  StatusChanged: 'Cambio de estado',
  ResponsibleChanged: 'Cambio de responsable',
};

const dateFormatter = new Intl.DateTimeFormat('es-CO', {
  dateStyle: 'medium',
  timeStyle: 'short',
});

export const formatDateTime = (isoDate: string) => dateFormatter.format(new Date(isoDate));

/**
 * Cómo se refieren las personas a una solicitud. El Guid sigue siendo el identificador en las
 * URLs y en las llamadas; este es el legible que muestra el listado, ya que RF-02 pide un
 * identificador en él.
 */
export const formatRequestNumber = (value: number) => `SOL-${String(value).padStart(4, '0')}`;

/** Traduce un valor del historial, que en los eventos del ciclo de vida lleva un nombre de estado. */
export const describeHistoryValue = (value: string | null): string => {
  if (!value) return '—';
  return statusLabels[value as RequestStatus] ?? value;
};
