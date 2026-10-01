'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { ErrorMessage, Spinner } from '@/components/ui/Feedback';
import { ApiError, authApi } from '@/lib/api/client';
import { setClientSession } from '@/lib/auth/clientSession';

const PASSWORD_MIN = 8;
const PASSWORD_MAX = 128;

interface FormState {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

/**
 * Replica las reglas de la API para dar respuesta inmediata. El campo de confirmación existe
 * solo aquí: protege al usuario de un error de tipeo, así que el servidor no tiene por qué recibirlo.
 */
function validate(form: FormState): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!form.fullName.trim()) errors.fullName = 'Ingrese su nombre.';

  if (!form.email.trim()) {
    errors.email = 'Ingrese su correo.';
  } else if (!/^[^\s@]+@[^\s@]+$/.test(form.email.trim())) {
    errors.email = 'El correo no tiene un formato válido.';
  }

  if (form.password.length < PASSWORD_MIN || form.password.length > PASSWORD_MAX) {
    errors.password = `La contraseña debe tener entre ${PASSWORD_MIN} y ${PASSWORD_MAX} caracteres.`;
  } else if (!/\p{L}/u.test(form.password) || !/\d/.test(form.password)) {
    errors.password = 'La contraseña debe incluir al menos una letra y un número.';
  }

  if (form.confirmPassword !== form.password) {
    errors.confirmPassword = 'Las contraseñas no coinciden.';
  }

  return errors;
}

export function RegisterForm() {
  const router = useRouter();

  const [form, setForm] = useState<FormState>({ fullName: '', email: '', password: '', confirmPassword: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const update = (field: keyof FormState, value: string) => setForm((current) => ({ ...current, [field]: value }));

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitError(null);

    const validationErrors = validate(form);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    setIsSubmitting(true);
    try {
      // No se envía rol, porque no hay ninguno que enviar: la API siempre crea un solicitante.
      const result = await authApi.register({
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        password: form.password,
      });

      setClientSession(result);
      router.replace('/');
      router.refresh();
    } catch (error) {
      // Tanto un 400 como el 409 de un correo ya registrado traen errores por campo, así que
      // aparecen junto al campo que los causó.
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setErrors(
          Object.fromEntries(
            Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0]]),
          ),
        );
      }

      setSubmitError(error instanceof Error ? error.message : 'No fue posible completar el registro.');
      setIsSubmitting(false);
    }
  };

  const field = (
    id: keyof FormState,
    label: string,
    type: string,
    autoComplete: string,
    hint?: string,
  ) => (
    <div>
      <label className="field-label" htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        type={type}
        autoComplete={autoComplete}
        className="field-control"
        value={form[id]}
        onChange={(event) => update(id, event.target.value)}
        aria-invalid={Boolean(errors[id])}
        aria-describedby={errors[id] ? `${id}-error` : hint ? `${id}-hint` : undefined}
      />
      {errors[id] ? (
        <p id={`${id}-error`} className="field-error">
          {errors[id]}
        </p>
      ) : hint ? (
        <p id={`${id}-hint`} className="mt-1.5 text-xs text-slate-400">
          {hint}
        </p>
      ) : null}
    </div>
  );

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-4">
      {submitError ? <ErrorMessage title={submitError} /> : null}

      {field('fullName', 'Nombre completo', 'text', 'name')}
      {field('email', 'Correo', 'email', 'email')}
      {field(
        'password',
        'Contraseña',
        'password',
        'new-password',
        `Mínimo ${PASSWORD_MIN} caracteres, con al menos una letra y un número.`,
      )}
      {field('confirmPassword', 'Confirmar contraseña', 'password', 'new-password')}

      <Button type="submit" disabled={isSubmitting} className="w-full">
        Crear cuenta
      </Button>

      {isSubmitting ? <Spinner label="Creando su cuenta…" /> : null}
    </form>
  );
}
