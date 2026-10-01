import Image from 'next/image';
import Link from 'next/link';
import { redirect } from 'next/navigation';
import { AuthProvider } from '@/components/auth/AuthProvider';
import { MainNav } from '@/components/auth/MainNav';
import { UserMenu } from '@/components/auth/UserMenu';
import { getServerSession } from '@/lib/auth/serverSession';

/**
 * Estructura del área autenticada. La sesión se lee en el servidor, así que todas las páginas
 * de abajo pueden seguir pidiendo sus datos en el servidor con el mismo token.
 *
 * La redirección repite a propósito lo que ya hace proxy.ts: el proxy es una comodidad de
 * navegación y esto es la garantía de que ninguna página bajo este layout se muestra sin sesión.
 */
export default async function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  const session = await getServerSession();

  if (!session) {
    redirect('/login');
  }

  return (
    <AuthProvider session={session}>
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
          <Link href="/" className="flex items-center gap-3">
            <Image
              src="/expertos-seguridad-logo.png"
              alt="Expertos Seguridad"
              width={160}
              height={96}
              priority
              className="h-9 w-auto"
            />
            <span className="hidden border-l border-slate-200 pl-3 text-sm font-semibold text-slate-900 sm:inline">
              Solicitudes de mantenimiento
            </span>
          </Link>

          <MainNav />

          <UserMenu />
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8">{children}</main>
    </AuthProvider>
  );
}
