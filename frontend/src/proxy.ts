import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { SESSION_COOKIE, parseSession } from '@/lib/auth/session';

/** Accesibles sin sesión. A un usuario con sesión se le envía al inicio. */
const PUBLIC_PAGES = new Set(['/login', '/register']);

/**
 * Dirige el tráfico entre las páginas públicas y el área autenticada. Next 16 llama «proxy» a
 * esta convención de archivo; es lo que las versiones anteriores llamaban «middleware».
 *
 * Es una comodidad de navegación, no una frontera de seguridad: solo mira la forma de la cookie,
 * no verifica la firma del token. La API valida cada llamada, así que una cookie falsificada no
 * consigue más que una pantalla vacía llena de errores.
 */
export function proxy(request: NextRequest) {
  const { pathname, search } = request.nextUrl;

  // /logout siempre debe ejecutarse, con o sin sesión: es la salida.
  if (pathname === '/logout') {
    return NextResponse.next();
  }

  const session = parseSession(request.cookies.get(SESSION_COOKIE)?.value);
  const isPublicPage = PUBLIC_PAGES.has(pathname);

  if (!session && !isPublicPage) {
    const login = new URL('/login', request.url);
    // Recordar a dónde iba el usuario, para que al iniciar sesión llegue ahí.
    login.searchParams.set('next', pathname + search);

    return NextResponse.redirect(login);
  }

  if (session && isPublicPage) {
    return NextResponse.redirect(new URL('/', request.url));
  }

  return NextResponse.next();
}

export const config = {
  // Todo excepto los recursos propios de Next, el favicon y los archivos públicos.
  matcher: ['/((?!_next/static|_next/image|favicon.ico|.*\\.png$|.*\\.svg$).*)'],
};
