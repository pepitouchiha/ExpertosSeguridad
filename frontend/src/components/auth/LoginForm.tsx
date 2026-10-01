'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { ErrorMessage, Spinner } from '@/components/ui/Feedback';
import { ApiError, authApi } from '@/lib/api/client';
import { setClientSession } from '@/lib/auth/clientSession';

export function LoginForm({ redirectTo = '/' }: { redirectTo?: string }) {
  const router = useRouter();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitError(null);
    setErrors({});

    const validationErrors: Record<string, string> = {};
    if (!email.trim()) validationErrors.email = 'Ingrese su correo.';
    if (!password) validationErrors.password = 'Ingrese su contraseña.';

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await authApi.login(email.trim(), password);

      setClientSession(result);
      // refresh() vuelve a ejecutar el layout del servidor para que tome la cookie recién escrita.
      router.replace(redirectTo);
      router.refresh();
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setErrors(
          Object.fromEntries(
            Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0]]),
          ),
        );
      }

      setSubmitError(
        error instanceof Error ? error.message : 'No fue posible iniciar sesión. Intente nuevamente.',
      );
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-4">
      {submitError ? <ErrorMessage title={submitError} /> : null}

      <div>
        <label className="field-label" htmlFor="email">
          Correo
        </label>
        <input
          id="email"
          type="email"
          autoComplete="username"
          className="field-control"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          aria-invalid={Boolean(errors.email)}
          aria-describedby={errors.email ? 'email-error' : undefined}
        />
        {errors.email ? (
          <p id="email-error" className="field-error">
            {errors.email}
          </p>
        ) : null}
      </div>

      <div>
        <label className="field-label" htmlFor="password">
          Contraseña
        </label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          className="field-control"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          aria-invalid={Boolean(errors.password)}
          aria-describedby={errors.password ? 'password-error' : undefined}
        />
        {errors.password ? (
          <p id="password-error" className="field-error">
            {errors.password}
          </p>
        ) : null}
      </div>

      <div className="flex items-center gap-3 pt-1">
        <Button type="submit" disabled={isSubmitting} className="w-full justify-center">
          Iniciar sesión
        </Button>
      </div>

      {isSubmitting ? <Spinner label="Verificando credenciales…" /> : null}
    </form>
  );
}
