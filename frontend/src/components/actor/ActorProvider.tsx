'use client';

import { createContext, useCallback, useContext, useMemo, useSyncExternalStore } from 'react';
import type { ReactNode } from 'react';
import type { User } from '@/lib/api/types';

const STORAGE_KEY = 'expertos-seguridad.actor-id';

const listeners = new Set<() => void>();

function subscribe(onStoreChange: () => void) {
  listeners.add(onStoreChange);
  // The 'storage' event fires in other tabs, keeping the selection consistent across them.
  window.addEventListener('storage', onStoreChange);

  return () => {
    listeners.delete(onStoreChange);
    window.removeEventListener('storage', onStoreChange);
  };
}

function getStoredActorId(): string | null {
  try {
    return window.localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

/** On the server there is no stored selection, so the default user is used. */
const getServerSnapshot = () => null;

function storeActorId(id: string) {
  try {
    window.localStorage.setItem(STORAGE_KEY, id);
  } catch {
    // A browser blocking storage only loses the preference between reloads.
  }
  listeners.forEach((listener) => listener());
}

interface ActorContextValue {
  actor: User;
  users: User[];
  setActorId: (id: string) => void;
}

const ActorContext = createContext<ActorContextValue | null>(null);

/**
 * Holds the acting user for the whole app. This is the UI half of the documented
 * authentication simplification: the selected identity travels to the API in the
 * X-Actor-Id header, where a real deployment would send an access token instead.
 *
 * The selection is read with useSyncExternalStore because localStorage is an external
 * store: this keeps the server and the first client render in agreement instead of
 * correcting the value in an effect after mount.
 */
export function ActorProvider({ users, children }: { users: User[]; children: ReactNode }) {
  const storedActorId = useSyncExternalStore(subscribe, getStoredActorId, getServerSnapshot);

  const setActorId = useCallback((id: string) => storeActorId(id), []);

  const value = useMemo<ActorContextValue>(() => {
    const actor = users.find((user) => user.id === storedActorId) ?? users[0];

    return { actor, users, setActorId };
  }, [storedActorId, users, setActorId]);

  return <ActorContext.Provider value={value}>{children}</ActorContext.Provider>;
}

export function useActor(): ActorContextValue {
  const context = useContext(ActorContext);

  if (!context) {
    throw new Error('useActor debe usarse dentro de ActorProvider.');
  }

  return context;
}
