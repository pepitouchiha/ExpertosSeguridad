import type { Session } from '@/lib/api/types';

/**
 * Nombre de la cookie que guarda la sesión. La comparten el helper del servidor, el del
 * navegador y proxy.ts, así que hay un único lugar donde se escribe.
 */
export const SESSION_COOKIE = 'expertos_seguridad_session';

/**
 * Interpreta el valor de la cookie. Cualquier cosa mal formada se trata como «sin sesión» en
 * lugar de lanzar un error: una cookie truncada o editada a mano debe llevar al login, no romper
 * el render.
 */
export function parseSession(raw: string | undefined): Session | null {
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(decodeURIComponent(raw)) as Partial<Session>;

    if (!parsed.accessToken || !parsed.user?.id || !parsed.user.role) {
      return null;
    }

    // Un token vencido solo lo rechazaría la API; descartarlo aquí evita un viaje inútil y un
    // parpadeo del layout autenticado.
    if (parsed.expiresAt && Date.parse(parsed.expiresAt) <= Date.now()) {
      return null;
    }

    return parsed as Session;
  } catch {
    return null;
  }
}

export function serialiseSession(session: Session): string {
  return encodeURIComponent(JSON.stringify(session));
}
