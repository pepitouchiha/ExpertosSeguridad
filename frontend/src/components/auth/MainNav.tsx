'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from './AuthProvider';

/**
 * Secciones de la aplicación. Solo los administradores tienen más de una, así que para los
 * demás la navegación no se muestra, en lugar de mostrar un único enlace solitario.
 */
export function MainNav() {
  const { isAdmin } = useAuth();
  const pathname = usePathname();

  if (!isAdmin) {
    return null;
  }

  const links = [
    { href: '/', label: 'Solicitudes', active: pathname === '/' || pathname.startsWith('/requests') },
    { href: '/admin/users', label: 'Usuarios y roles', active: pathname.startsWith('/admin') },
  ];

  return (
    <nav aria-label="Secciones" className="flex gap-1">
      {links.map((link) => (
        <Link
          key={link.href}
          href={link.href}
          aria-current={link.active ? 'page' : undefined}
          className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
            link.active ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          {link.label}
        </Link>
      ))}
    </nav>
  );
}
