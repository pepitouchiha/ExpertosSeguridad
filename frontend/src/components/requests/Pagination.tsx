import Link from 'next/link';

interface PaginationProps {
  page: number;
  totalPages: number;
  totalItems: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  /** Current query string without the page parameter, so links keep the active filters. */
  baseParams: URLSearchParams;
}

export function Pagination({
  page,
  totalPages,
  totalItems,
  hasPreviousPage,
  hasNextPage,
  baseParams,
}: PaginationProps) {
  const hrefForPage = (target: number) => {
    const params = new URLSearchParams(baseParams.toString());
    params.set('page', String(target));
    return `/?${params.toString()}`;
  };

  const linkClass = 'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50';
  const disabledClass = 'rounded-md border border-slate-200 px-3 py-1.5 text-sm text-slate-300';

  return (
    <nav
      aria-label="Paginación de solicitudes"
      className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 px-4 py-3"
    >
      <p className="text-sm text-slate-500">
        {totalItems === 0
          ? 'Sin resultados'
          : `Página ${page} de ${totalPages} · ${totalItems} solicitud${totalItems === 1 ? '' : 'es'}`}
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
