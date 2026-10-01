import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Solicitudes de mantenimiento | Expertos Seguridad',
  description: 'Registro y seguimiento de solicitudes de mantenimiento de instalaciones y equipos.',
};

/**
 * Solo el armazón del documento. La estructura autenticada (encabezado, menú de usuario,
 * contexto de autenticación) vive en el layout del grupo `(authenticated)`, así que la página
 * de login se muestra sin nada de eso.
 */
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="es">
      <body className="min-h-screen">{children}</body>
    </html>
  );
}
