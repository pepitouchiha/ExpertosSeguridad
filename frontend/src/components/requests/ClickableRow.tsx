'use client';

import { useRouter } from 'next/navigation';
import type { ReactNode } from 'react';

/**
 * Una fila de tabla que abre su solicitud al hacer clic en cualquier parte. Un <tr> no puede
 * ser un enlace, así que el título conserva su <a> real para teclado y lectores de pantalla, y
 * la fila agrega el atajo para el ratón. Los clics sobre el enlace mismo, o con una tecla
 * modificadora (abrir en otra pestaña), se le dejan al navegador.
 */
export function ClickableRow({ href, className, children }: { href: string; className?: string; children: ReactNode }) {
  const router = useRouter();

  return (
    <tr
      className={`cursor-pointer ${className ?? ''}`}
      onClick={(event) => {
        if ((event.target as HTMLElement).closest('a')) return;
        if (event.metaKey || event.ctrlKey) {
          window.open(href, '_blank', 'noopener');
          return;
        }
        router.push(href);
      }}
    >
      {children}
    </tr>
  );
}
