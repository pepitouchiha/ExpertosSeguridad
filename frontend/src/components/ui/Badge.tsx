import type { RequestPriority, RequestStatus, UserRole } from '@/lib/api/types';
import { priorityLabels, roleLabels, statusLabels } from '@/lib/labels';

const statusStyles: Record<RequestStatus, string> = {
  Pending: 'bg-amber-100 text-amber-800 ring-amber-200',
  InProgress: 'bg-blue-100 text-blue-800 ring-blue-200',
  OnHold: 'bg-slate-200 text-slate-700 ring-slate-300',
  Resolved: 'bg-emerald-100 text-emerald-800 ring-emerald-200',
  Cancelled: 'bg-red-100 text-red-800 ring-red-200',
};

const priorityStyles: Record<RequestPriority, string> = {
  Low: 'bg-slate-100 text-slate-700 ring-slate-200',
  Medium: 'bg-sky-100 text-sky-800 ring-sky-200',
  High: 'bg-orange-100 text-orange-800 ring-orange-200',
  Critical: 'bg-red-100 text-red-800 ring-red-200',
};

const base = 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset';

export function StatusBadge({ status }: { status: RequestStatus }) {
  return <span className={`${base} ${statusStyles[status]}`}>{statusLabels[status]}</span>;
}

export function PriorityBadge({ priority }: { priority: RequestPriority }) {
  return <span className={`${base} ${priorityStyles[priority]}`}>{priorityLabels[priority]}</span>;
}

const roleStyles: Record<UserRole, string> = {
  Requester: 'bg-slate-100 text-slate-700 ring-slate-200',
  Staff: 'bg-brand-50 text-brand-700 ring-brand-200',
  Admin: 'bg-violet-50 text-violet-700 ring-violet-200',
};

export function RoleBadge({ role }: { role: UserRole }) {
  return <span className={`${base} ${roleStyles[role]}`}>{roleLabels[role]}</span>;
}

export function ActiveBadge({ isActive }: { isActive: boolean }) {
  return isActive ? (
    <span className={`${base} bg-emerald-50 text-emerald-700 ring-emerald-200`}>Activa</span>
  ) : (
    <span className={`${base} bg-slate-100 text-slate-500 ring-slate-200`}>Desactivada</span>
  );
}
