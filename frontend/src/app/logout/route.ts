import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { SESSION_COOKIE } from '@/lib/auth/session';

/**
 * Termina la sesión. Es un route handler y no una página porque los Server Components pueden
 * leer cookies pero no borrarlas; un route handler sí. Toda salida de la aplicación pasa por
 * aquí: el botón «Salir» y cualquier llamada, del servidor o del navegador, que encuentre a la
 * API rechazando un token que antes aceptaba (cuenta desactivada, rol cambiado).
 *
 * El motivo viaja hasta la página de login para que pueda explicarle al usuario por qué está ahí.
 */
export function GET(request: NextRequest) {
  const reason = request.nextUrl.searchParams.get('reason');
  const target = reason === 'session' ? '/login?reason=session' : '/login';

  // Location relativa a propósito. Dentro del contenedor, el servidor standalone ve la dirección
  // en la que escucha (0.0.0.0), así que una URL absoluta construida desde request.url mandaría al
  // navegador a un host que no puede abrir. El navegador resuelve una relativa contra la dirección
  // que de verdad usó. 303: después de un cambio de estado, continuar con un GET simple.
  const response = new NextResponse(null, { status: 303, headers: { Location: target } });
  response.cookies.delete(SESSION_COOKIE);

  return response;
}
