'use client';

import { RoleBadge } from '@/components/ui/Badge';
import { useAuth } from './AuthProvider';

/** Quién inició sesión, qué rol tiene y la forma de salir. */
export function UserMenu() {
  const { user, signOut } = useAuth();

  return (
    <div className="flex items-center gap-3">
      <div className="text-right">
        <p className="text-sm font-medium leading-tight text-slate-900">{user.name}</p>
        <p className="text-xs leading-tight text-slate-500">{user.email}</p>
      </div>

      <RoleBadge role={user.role} />

      <button
        type="button"
        onClick={signOut}
        className="rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-700
          transition-colors hover:bg-slate-50"
      >
        Salir
      </button>
    </div>
  );
}
