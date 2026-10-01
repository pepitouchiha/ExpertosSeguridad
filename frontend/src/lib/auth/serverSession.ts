import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import { ApiError, SESSION_EXPIRED_PATH } from '@/lib/api/client';
import type { Session } from '@/lib/api/types';
import { SESSION_COOKIE, parseSession } from './session';

/**
 * Llamar primero en el catch de cualquier llamada a la API desde el servidor. Un 401 ahí
 * significa que la cuenta se desactivó o cambió de rol después de emitirse el token, así que lo
 * correcto es terminar la sesión, no mostrar un error dentro de una página que quizá el usuario
 * ya no puede ver.
 *
 * Debe ejecutarse dentro del catch y no alrededor de la llamada: redirect() funciona lanzando
 * una excepción, y un try/catch alrededor la atraparía y mostraría el error en su lugar.
 */
export function redirectIfSessionEnded(error: unknown): void {
  if (error instanceof ApiError && error.status === 401) {
    redirect(SESSION_EXPIRED_PATH);
  }
}

/**
 * Lee la sesión en el servidor. Esta es la razón de que el token viva en una cookie y no en
 * localStorage: los Server Components pueden leerla, así que el panel y el detalle siguen pidiendo
 * sus datos en el servidor en lugar de hacerlo después de la hidratación.
 *
 * Compromiso, documentado en `docs/decisiones-tecnicas.md`: la cookie no es httpOnly, así que un
 * XSS exitoso podría leer el token, la misma exposición que tendría localStorage. Mover el token
 * detrás de route handlers de Next (una cookie httpOnly que el navegador nunca ve) es la mejora
 * que lo elimina.
 */
export async function getServerSession(): Promise<Session | null> {
  const store = await cookies();

  return parseSession(store.get(SESSION_COOKIE)?.value);
}
