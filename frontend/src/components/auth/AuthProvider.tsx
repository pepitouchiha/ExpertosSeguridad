'use client';

import { createContext, useCallback, useContext, useMemo } from 'react';
import type { ReactNode } from 'react';
import type { AuthenticatedUser, Session } from '@/lib/api/types';

interface AuthContextValue {
  user: AuthenticatedUser;
  /** Lo necesitan los componentes de cliente que llaman a la API directamente (cambios de estado, asignación). */
  accessToken: string;
  isRequester: boolean;
  isStaff: boolean;
  isAdmin: boolean;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Guarda el usuario de la sesión para toda el área autenticada. La sesión llega desde el layout
 * del servidor, que ya leyó la cookie, así que el primer render del cliente coincide con el del
 * servidor y no hay parpadeo de un estado equivocado.
 *
 * Los indicadores solo deciden qué mostrar. La API vuelve a aplicar cada una de esas decisiones,
 * y es el único lugar donde de verdad protegen algo.
 */
export function AuthProvider({ session, children }: { session: Session; children: ReactNode }) {
  // Una navegación completa, no router.replace: /logout es un route handler que borra la cookie
  // en el servidor, y una carga completa además descarta todas las páginas autenticadas en caché.
  // eslint-disable-next-line @next/next/no-location-assign-relative-destination -- /logout es un route handler, no una página
  const signOut = useCallback(() => window.location.assign('/logout'), []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session.user,
      accessToken: session.accessToken,
      isRequester: session.user.role === 'Requester',
      isStaff: session.user.role === 'Staff',
      isAdmin: session.user.role === 'Admin',
      signOut,
    }),
    [session, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth debe usarse dentro de AuthProvider.');
  }

  return context;
}
