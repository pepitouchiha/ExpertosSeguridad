import type { Session } from '@/lib/api/types';
import { SESSION_COOKIE, parseSession, serialiseSession } from './session';

/** Lee la cookie de sesión en el navegador. Devuelve null en el servidor o si no existe. */
export function getClientSession(): Session | null {
  if (typeof document === 'undefined') {
    return null;
  }

  const match = document.cookie
    .split('; ')
    .find((entry) => entry.startsWith(`${SESSION_COOKIE}=`));

  return parseSession(match?.slice(SESSION_COOKIE.length + 1));
}

/**
 * Guarda la sesión. SameSite=Lax bloquea la cookie en peticiones desde otros sitios, lo que
 * elimina el vector CSRF; Secure se agrega solo con HTTPS para que el desarrollo local por http
 * siga funcionando.
 */
export function setClientSession(session: Session): void {
  const expires = new Date(session.expiresAt);
  const secure = window.location.protocol === 'https:' ? '; Secure' : '';

  document.cookie =
    `${SESSION_COOKIE}=${serialiseSession(session)}` +
    `; Path=/; Expires=${expires.toUTCString()}; SameSite=Lax${secure}`;
}

export function clearClientSession(): void {
  document.cookie = `${SESSION_COOKIE}=; Path=/; Max-Age=0; SameSite=Lax`;
}
