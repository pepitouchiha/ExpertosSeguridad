import Link from 'next/link';
import { redirect } from 'next/navigation';
import { CreateRequestForm } from '@/components/requests/CreateRequestForm';
import { getServerSession } from '@/lib/auth/serverSession';

export const metadata = {
  title: 'Nueva solicitud | Expertos Seguridad',
};

export default async function NewRequestPage() {
  const session = (await getServerSession())!;

  // Solo los solicitantes abren solicitudes: el personal las atiende y los administradores las
  // supervisan. La API también lo exige; enviar a los demás de vuelta evita un formulario que
  // sería rechazado.
  if (session.user.role !== 'Requester') {
    redirect('/');
  }

  return (
    <div className="mx-auto max-w-3xl space-y-5">
      <div>
        <Link href="/" className="text-sm text-brand-700 hover:underline">
          ← Volver al panel
        </Link>
        <h1 className="mt-2 text-xl font-semibold text-slate-900">Registrar solicitud de mantenimiento</h1>
        <p className="text-sm text-slate-500">
          Describa el requerimiento con el mayor detalle posible para facilitar su atención.
        </p>
      </div>

      <CreateRequestForm />
    </div>
  );
}
