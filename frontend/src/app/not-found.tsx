import Link from 'next/link';

export default function NotFound() {
  return (
    <div className="mx-auto max-w-lg space-y-3 py-16 text-center">
      <h1 className="text-lg font-semibold text-slate-900">Solicitud no encontrada</h1>
      <p className="text-sm text-slate-500">
        La solicitud que intenta consultar no existe o fue eliminada.
      </p>
      <Link href="/" className="inline-block text-sm text-brand-700 hover:underline">
        Volver al panel
      </Link>
    </div>
  );
}
