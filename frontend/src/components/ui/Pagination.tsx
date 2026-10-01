import Link from 'next/link';

interface PaginationProps {
  page: number;
  totalPages: number;
  totalItems: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  /** Query string actual sin el parámetro de página, para que los enlaces conserven los filtros activos. */
  baseParams: URLSearchParams;
  /** Ruta a la que apuntan los enlaces; el listado de solicitudes vive en la raíz. */
  basePath?: string;
  /** Sustantivo de la línea de resumen, en singular y en plural. */
  noun?: { one: string; many: string };
}

export function Pagination({
  page,
  totalPages,
  totalItems,
  hasPreviousPage,
  hasNextPage,
  baseParams,
  basePath = '/',
  noun = { one: 'solicitud', many: 'solicitudes' },
}: PaginationProps) {
  const hrefForPage = (target: number) => {
    const params = new URLSearchParams(baseParams.toString());
    params.set('page', String(target));
    return `${basePath}?${params.toString()}`;
  };

  const linkClass = 'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50';
  const disabledClass = 'rounded-md border border-slate-200 px-3 py-1.5 text-sm text-slate-300';

  return (
    <nav
      aria-label={`Paginación de ${noun.many}`}
      className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 px-4 py-3"
    >
      <p className="text-sm text-slate-500">
        {totalItems === 0
          ? 'Sin resultados'
          : `Página ${page} de ${totalPages} · ${totalItems} ${totalItems === 1 ? noun.one : noun.many}`}
      </p>

      <div className="flex gap-2">
        {hasPreviousPage ? (
          <Link href={hrefForPage(page - 1)} className={linkClass} rel="prev">
            Anterior
          </Link>
        ) : (
          <span className={disabledClass} aria-disabled>
            Anterior
          </span>
        )}

        {hasNextPage ? (
          <Link href={hrefForPage(page + 1)} className={linkClass} rel="next">
            Siguiente
          </Link>
        ) : (
          <span className={disabledClass} aria-disabled>
            Siguiente
          </span>
        )}
      </div>
    </nav>
  );
}
