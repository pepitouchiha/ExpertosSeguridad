import type { Metadata } from 'next';
import Link from 'next/link';
import { ActorProvider } from '@/components/actor/ActorProvider';
import { ActorSelector } from '@/components/actor/ActorSelector';
import { usersApi } from '@/lib/api/client';
import type { User } from '@/lib/api/types';
import './globals.css';

export const metadata: Metadata = {
  title: 'Solicitudes de mantenimiento | Expertos Seguridad',
  description: 'Registro y seguimiento de solicitudes de mantenimiento de instalaciones y equipos.',
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  // The user catalogue is fetched once on the server; if the API is down the shell still
  // renders so the pages below can show their own error state.
  let users: User[] = [];
  try {
    users = await usersApi.getAll();
  } catch {
    users = [];
  }

  return (
    <html lang="es">
      <body className="min-h-screen">
        <ActorProvider users={users}>
          <header className="border-b border-slate-200 bg-white">
            <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
              <Link href="/" className="flex flex-col">
                <span className="text-base font-semibold text-slate-900">Solicitudes de mantenimiento</span>
                <span className="text-xs text-slate-500">Expertos Seguridad LTDA</span>
              </Link>
              {users.length > 0 ? <ActorSelector /> : null}
            </div>
          </header>

          <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8">{children}</main>
        </ActorProvider>
      </body>
    </html>
  );
}
