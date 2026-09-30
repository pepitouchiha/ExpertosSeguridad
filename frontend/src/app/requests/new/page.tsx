import Link from 'next/link';
import { CreateRequestForm } from '@/components/requests/CreateRequestForm';

export const metadata = {
  title: 'Nueva solicitud | Expertos Seguridad',
};

export default function NewRequestPage() {
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
