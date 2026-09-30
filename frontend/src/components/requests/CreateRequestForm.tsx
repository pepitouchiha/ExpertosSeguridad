'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useActor } from '@/components/actor/ActorProvider';
import { Button } from '@/components/ui/Button';
import { ErrorMessage, Spinner } from '@/components/ui/Feedback';
import { ApiError, maintenanceRequestsApi } from '@/lib/api/client';
import { REQUEST_CATEGORIES, REQUEST_PRIORITIES } from '@/lib/api/types';
import type { RequestCategory, RequestPriority } from '@/lib/api/types';
import { categoryLabels, priorityLabels } from '@/lib/labels';

const TITLE_MIN = 5;
const TITLE_MAX = 120;
const DESCRIPTION_MIN = 10;
const DESCRIPTION_MAX = 2000;

interface FormState {
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  requesterId: string;
}

/**
 * Client-side validation mirrors the API bounds to give immediate feedback, but it is only a
 * convenience: the server validates again and its field errors are surfaced here, so the
 * backend stays the authority.
 */
function validate(form: FormState): Record<string, string> {
  const errors: Record<string, string> = {};
  const title = form.title.trim();
  const description = form.description.trim();

  if (title.length < TITLE_MIN || title.length > TITLE_MAX) {
    errors.title = `El título debe tener entre ${TITLE_MIN} y ${TITLE_MAX} caracteres.`;
  }

  if (description.length < DESCRIPTION_MIN || description.length > DESCRIPTION_MAX) {
    errors.description = `La descripción debe tener entre ${DESCRIPTION_MIN} y ${DESCRIPTION_MAX} caracteres.`;
  }

  if (!form.requesterId) {
    errors.requesterId = 'Seleccione el solicitante.';
  }

  return errors;
}

export function CreateRequestForm() {
  const router = useRouter();
  const { actor, users } = useActor();

  const [form, setForm] = useState<FormState>({
    title: '',
    description: '',
    category: 'Infrastructure',
    priority: 'Medium',
    requesterId: actor?.id ?? '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const update = <K extends keyof FormState>(field: K, value: FormState[K]) =>
    setForm((current) => ({ ...current, [field]: value }));

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
      const created = await maintenanceRequestsApi.create(
        {
          title: form.title.trim(),
          description: form.description.trim(),
          category: form.category,
          priority: form.priority,
          requesterId: form.requesterId,
        },
        actor.id,
      );

      router.push(`/requests/${created.id}`);
      router.refresh();
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setErrors(
          Object.fromEntries(
            Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0]]),
          ),
        );
      }
      setSubmitError(error instanceof Error ? error.message : 'No fue posible registrar la solicitud.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} noValidate className="card space-y-4 p-4 sm:p-6">
      {submitError ? <ErrorMessage title={submitError} /> : null}

      <div>
        <label className="field-label" htmlFor="title">
          Título
        </label>
        <input
          id="title"
          className="field-control"
          value={form.title}
          maxLength={TITLE_MAX}
          onChange={(event) => update('title', event.target.value)}
          aria-invalid={Boolean(errors.title)}
          aria-describedby={errors.title ? 'title-error' : undefined}
        />
        {errors.title ? (
          <p id="title-error" className="field-error">
            {errors.title}
          </p>
        ) : (
          <p className="mt-1.5 text-xs text-slate-400">Entre {TITLE_MIN} y {TITLE_MAX} caracteres.</p>
        )}
      </div>

      <div>
        <label className="field-label" htmlFor="description">
          Descripción
        </label>
        <textarea
          id="description"
          rows={5}
          className="field-control"
          value={form.description}
          maxLength={DESCRIPTION_MAX}
          onChange={(event) => update('description', event.target.value)}
          aria-invalid={Boolean(errors.description)}
          aria-describedby={errors.description ? 'description-error' : undefined}
        />
        {errors.description ? (
          <p id="description-error" className="field-error">
            {errors.description}
          </p>
        ) : (
          <p className="mt-1.5 text-xs text-slate-400">
            {form.description.trim().length} / {DESCRIPTION_MAX} caracteres.
          </p>
        )}
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <div>
          <label className="field-label" htmlFor="category">
            Categoría
          </label>
          <select
            id="category"
            className="field-control"
            value={form.category}
            onChange={(event) => update('category', event.target.value as RequestCategory)}
          >
            {REQUEST_CATEGORIES.map((category) => (
              <option key={category} value={category}>
                {categoryLabels[category]}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="field-label" htmlFor="priority">
            Prioridad
          </label>
          <select
            id="priority"
            className="field-control"
            value={form.priority}
            onChange={(event) => update('priority', event.target.value as RequestPriority)}
          >
            {REQUEST_PRIORITIES.map((priority) => (
              <option key={priority} value={priority}>
                {priorityLabels[priority]}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="field-label" htmlFor="requesterId">
            Solicitante
          </label>
          <select
            id="requesterId"
            className="field-control"
            value={form.requesterId}
            onChange={(event) => update('requesterId', event.target.value)}
            aria-invalid={Boolean(errors.requesterId)}
          >
            {users.map((user) => (
              <option key={user.id} value={user.id}>
                {user.name}
              </option>
            ))}
          </select>
          {errors.requesterId ? <p className="field-error">{errors.requesterId}</p> : null}
        </div>
      </div>

      <div className="flex items-center gap-3 pt-2">
        <Button type="submit" disabled={isSubmitting}>
          Registrar solicitud
        </Button>
        <Button type="button" variant="secondary" onClick={() => router.push('/')} disabled={isSubmitting}>
          Cancelar
        </Button>
        {isSubmitting ? <Spinner label="Registrando…" /> : null}
      </div>

      <p className="text-xs text-slate-400">
        La solicitud se crea en estado «Pendiente» y con la fecha asignada por el servidor.
      </p>
    </form>
  );
}
