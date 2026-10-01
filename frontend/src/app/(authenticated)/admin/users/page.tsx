import { redirect } from 'next/navigation';
import { Suspense } from 'react';
import { UserFilters } from '@/components/admin/UserFilters';
import { UserRow } from '@/components/admin/UserRow';
import { EmptyState, ErrorMessage, SkeletonRows } from '@/components/ui/Feedback';
import { Pagination } from '@/components/ui/Pagination';
import { adminApi } from '@/lib/api/client';
import { getServerSession, redirectIfSessionEnded } from '@/lib/auth/serverSession';
import type { UserFilters as UserFilterValues, UserRole } from '@/lib/api/types';
import { USER_ROLES } from '@/lib/api/types';

export const metadata = {
  title: 'Usuarios y roles | Expertos Seguridad',
};

type SearchParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined): string | undefined =>
  Array.isArray(value) ? value[0] : value;

/** Solo se reenvían valores que la API conoce, para que una URL editada a mano no rompa la petición. */
function parseFilters(searchParams: SearchParams): UserFilterValues {
  const role = first(searchParams.role);
  const isActive = first(searchParams.isActive);
  const page = Number(first(searchParams.page) ?? '1');

  return {
    page: Number.isFinite(page) && page > 0 ? page : 1,
    pageSize: 10,
    role: USER_ROLES.includes(role as UserRole) ? (role as UserRole) : undefined,
    isActive: isActive === 'true' ? true : isActive === 'false' ? false : undefined,
    search: first(searchParams.search) || undefined,
  };
}

async function UsersList({ filters, token }: { filters: UserFilterValues; token: string }) {
  let result;
  try {
    result = await adminApi.searchUsers(filters, token);
  } catch (error) {
    redirectIfSessionEnded(error);

    return (
      <div className="p-4">
        <ErrorMessage title="No fue posible cargar los usuarios.">
          {error instanceof Error ? error.message : null}
        </ErrorMessage>
      </div>
    );
  }

  if (result.items.length === 0) {
    return <EmptyState title="No hay usuarios que coincidan con los criterios." description="Ajuste los filtros." />;
  }

  const baseParams = new URLSearchParams();
  if (filters.role) baseParams.set('role', filters.role);
  if (filters.isActive !== undefined) baseParams.set('isActive', String(filters.isActive));
  if (filters.search) baseParams.set('search', filters.search);

  return (
    <>
      <ul className="divide-y divide-slate-100">
        {result.items.map((user) => (
          // La clave incluye también el rol y el estado, para que la fila reinicie su estado local cuando
          // se aplica un cambio y los datos de la página vuelven actualizados.
          <UserRow key={`${user.id}-${user.role}-${user.isActive}`} user={user} />
        ))}
      </ul>
      <Pagination
        page={result.page}
        totalPages={result.totalPages}
        totalItems={result.totalItems}
        hasPreviousPage={result.hasPreviousPage}
        hasNextPage={result.hasNextPage}
        baseParams={baseParams}
        basePath="/admin/users"
        noun={{ one: 'usuario', many: 'usuarios' }}
      />
    </>
  );
}

export default async function AdminUsersPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const session = (await getServerSession())!;

  // La API responde 403 a cualquier otro; enviarlo al inicio evita mostrar una página de errores.
  if (session.user.role !== 'Admin') {
    redirect('/');
  }

  const filters = parseFilters(await searchParams);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-slate-900">Usuarios y roles</h1>
        <p className="text-sm text-slate-500">
          Los clientes se registran como solicitantes. Desde aquí se promueve al personal y se desactivan cuentas.
        </p>
      </div>

      <dl className="grid gap-3 text-sm sm:grid-cols-3">
        <div className="card px-4 py-3">
          <dt className="font-medium text-slate-900">Solicitante</dt>
          <dd className="mt-0.5 text-slate-500">Registra solicitudes y sigue solo las suyas.</dd>
        </div>
        <div className="card px-4 py-3">
          <dt className="font-medium text-slate-900">Personal</dt>
          <dd className="mt-0.5 text-slate-500">Ve todas las solicitudes, las asigna y cambia su estado.</dd>
        </div>
        <div className="card px-4 py-3">
          <dt className="font-medium text-slate-900">Administrador</dt>
          <dd className="mt-0.5 text-slate-500">Gestiona cuentas y roles; supervisa sin operar las solicitudes.</dd>
        </div>
      </dl>

      <UserFilters />

      <section className="card overflow-hidden">
        <Suspense key={JSON.stringify(filters)} fallback={<SkeletonRows />}>
          <UsersList filters={filters} token={session.accessToken} />
        </Suspense>
      </section>
    </div>
  );
}
