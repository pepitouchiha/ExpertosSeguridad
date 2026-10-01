import Image from 'next/image';
import Link from 'next/link';
import { RegisterForm } from '@/components/auth/RegisterForm';

export const metadata = {
  title: 'Crear cuenta | Expertos Seguridad',
};

export default function RegisterPage() {
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
            className="h-auto w-40"
          />
          <h1 className="mt-5 text-lg font-semibold text-slate-900">Crear cuenta de cliente</h1>
          <p className="mt-1 text-sm text-slate-500">
            Registre sus solicitudes de mantenimiento y siga su atención en línea.
          </p>
        </div>

        <div className="card p-5 sm:p-6">
          <RegisterForm />
        </div>

        <p className="mt-5 text-center text-sm text-slate-500">
          ¿Ya tiene una cuenta?{' '}
          <Link href="/login" className="font-medium text-brand-700 hover:underline">
            Inicie sesión
          </Link>
        </p>
      </div>
    </div>
  );
}
