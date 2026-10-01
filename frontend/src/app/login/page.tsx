import Image from 'next/image';
import Link from 'next/link';
import { LoginForm } from '@/components/auth/LoginForm';

export const metadata = {
  title: 'Iniciar sesión | Expertos Seguridad',
};

type SearchParams = Record<string, string | string[] | undefined>;

/**
 * Solo se acepta como destino una ruta dentro de esta aplicación, para que un `?next=` armado
 * a propósito no convierta la página de login en una redirección abierta hacia otro sitio.
 */
function safeRedirect(value: string | string[] | undefined): string {
  const target = Array.isArray(value) ? value[0] : value;

  if (!target || !target.startsWith('/') || target.startsWith('//')) {
    return '/';
  }

  return target;
}

export default async function LoginPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const params = await searchParams;
  const redirectTo = safeRedirect(params.next);
  // Lo fija /logout cuando la API dejó de aceptar el token: la cuenta se desactivó o cambió su
  // rol. Sin esto, el usuario llegaría aquí sin saber por qué.
  const sessionEnded = params.reason === 'session';

  return (
    <div className="flex min-h-screen items-center justify-center px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex flex-col items-center text-center">
          <Image
            src="/expertos-seguridad-logo.png"
            alt="Expertos Seguridad"
            width={220}
            height={132}
            priority
            className="h-auto w-44"
          />
          <h1 className="mt-5 text-lg font-semibold text-slate-900">Solicitudes de mantenimiento</h1>
          <p className="mt-1 text-sm text-slate-500">Ingrese con su cuenta para continuar.</p>
        </div>

        {sessionEnded ? (
          <div role="status" className="mb-4 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
            Su sesión terminó porque cambiaron los permisos de su cuenta. Inicie sesión nuevamente.
          </div>
        ) : null}

        <div className="card p-5 sm:p-6">
          <LoginForm redirectTo={redirectTo} />
        </div>

        <p className="mt-5 text-center text-sm text-slate-500">
          ¿Es cliente y aún no tiene cuenta?{' '}
          <Link href="/register" className="font-medium text-brand-700 hover:underline">
            Regístrese
          </Link>
        </p>

        <p className="mt-4 text-center text-xs text-slate-400">
          Expertos Seguridad LTDA · Gestión de mantenimiento
        </p>
      </div>
    </div>
  );
}
