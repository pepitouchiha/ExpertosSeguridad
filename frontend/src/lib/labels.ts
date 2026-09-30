import type { HistoryEventType, RequestCategory, RequestPriority, RequestStatus } from './api/types';

/**
 * The API speaks the domain language (English enum names) and the UI speaks the user's
 * language. Keeping the translation in one map avoids scattering literals across components.
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

/** Translates a history value, which carries a status name for lifecycle events. */
export const describeHistoryValue = (value: string | null): string => {
  if (!value) return '—';
  return statusLabels[value as RequestStatus] ?? value;
};
