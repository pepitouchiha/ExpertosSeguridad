import type { HistoryEntry } from '@/lib/api/types';
import { describeHistoryValue, formatDateTime, historyEventLabels } from '@/lib/labels';

const dotStyles: Record<HistoryEntry['eventType'], string> = {
  Created: 'bg-slate-400',
  StatusChanged: 'bg-brand-500',
  ResponsibleChanged: 'bg-emerald-500',
};

function describeChange(entry: HistoryEntry): string {
  switch (entry.eventType) {
    case 'Created':
      return 'La solicitud se registró en estado Pendiente.';
    case 'StatusChanged':
      return `${describeHistoryValue(entry.previousValue)} → ${describeHistoryValue(entry.newValue)}`;
    case 'ResponsibleChanged':
      return `${entry.previousValue ?? 'Sin asignar'} → ${entry.newValue ?? 'Sin asignar'}`;
  }
}

export function HistoryTimeline({ history }: { history: HistoryEntry[] }) {
  return (
    <ol className="space-y-4">
      {history.map((entry) => (
        <li key={entry.id} className="relative pl-6">
          <span
            aria-hidden
            className={`absolute left-0 top-1.5 h-2.5 w-2.5 rounded-full ${dotStyles[entry.eventType]}`}
          />
          <p className="text-sm font-medium text-slate-900">{historyEventLabels[entry.eventType]}</p>
          <p className="text-sm text-slate-600">{describeChange(entry)}</p>
          <p className="mt-0.5 text-xs text-slate-400">
            {entry.actor.name} · {formatDateTime(entry.occurredAt)}
          </p>
        </li>
      ))}
    </ol>
  );
}
