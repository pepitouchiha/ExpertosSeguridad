'use client';

import { useActor } from './ActorProvider';

export function ActorSelector() {
  const { actor, users, setActorId } = useActor();

  return (
    <label className="flex items-center gap-2 text-sm">
      <span className="hidden text-slate-500 sm:inline">Actuando como</span>
      <select
        aria-label="Usuario que realiza las operaciones"
        value={actor?.id ?? ''}
        onChange={(event) => setActorId(event.target.value)}
        className="rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-900"
      >
        {users.map((user) => (
          <option key={user.id} value={user.id}>
            {user.name}
          </option>
        ))}
      </select>
    </label>
  );
}
