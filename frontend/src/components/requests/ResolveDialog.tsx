'use client';

import { useEffect, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/AuthProvider';
import { Button } from '@/components/ui/Button';
import { ErrorMessage, Spinner } from '@/components/ui/Feedback';
import { ApiError, maintenanceRequestsApi } from '@/lib/api/client';

const TITLE_MIN = 5;
const TITLE_MAX = 120;
const DESCRIPTION_MIN = 10;
const DESCRIPTION_MAX = 2000;

function validate(title: string, description: string): Record<string, string> {
  const errors: Record<string, string> = {};

  if (title.trim().length < TITLE_MIN || title.trim().length > TITLE_MAX) {
    errors.title = `El título debe tener entre ${TITLE_MIN} y ${TITLE_MAX} caracteres.`;
  }

  if (description.trim().length < DESCRIPTION_MIN || description.trim().length > DESCRIPTION_MAX) {
    errors.description = `La descripción debe tener entre ${DESCRIPTION_MIN} y ${DESCRIPTION_MAX} caracteres.`;
  }

  return errors;
}

/**
 * La respuesta que el responsable le da al solicitante al cerrar una solicitud. Construido sobre
 * el elemento nativo <dialog> abierto con showModal(): el foco queda atrapado dentro, Esc lo cierra
 * y la página de fondo queda inerte, sin una librería de modales.
 *
 * Quién responde se muestra, no se elige: la API registra al usuario autenticado, que tiene que
 * ser el responsable. La validación replica la de la API para dar respuesta inmediata; el
 * servidor vuelve a verificar y sus errores por campo caen en los mismos campos.
 */
export function ResolveDialog({
  open,
  requestId,
  requestTitle,
  onClose,
  onResolved,
}: {
  open: boolean;
  requestId: string;
  requestTitle: string;
  onClose: () => void;
  onResolved: () => void;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const { user, accessToken } = useAuth();

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // El estado abierto del diálogo le pertenece al elemento del DOM; esto lo mantiene al día con la prop.
  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitError(null);

    const validationErrors = validate(title, description);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setIsSubmitting(true);
    try {
      await maintenanceRequestsApi.resolve(
        requestId,
        { title: title.trim(), description: description.trim() },
        accessToken,
      );
      onResolved();
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setErrors(
          Object.fromEntries(Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0]])),
        );
      }
      setSubmitError(error instanceof Error ? error.message : 'No fue posible resolver la solicitud.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="resolve-title"
      // Se dispara con Esc y también con close(): el estado del padre debe seguirlo en ambos casos.
      onClose={onClose}
      className="w-[min(36rem,calc(100vw-2rem))] rounded-lg border border-slate-200 p-0 shadow-xl backdrop:bg-slate-900/40"
    >
      <form onSubmit={handleSubmit} noValidate className="space-y-4 p-5 sm:p-6">
        <div>
          <h2 id="resolve-title" className="text-base font-semibold text-slate-900">
            Resolver solicitud
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            «{requestTitle}». La respuesta queda registrada y el solicitante podrá leerla en el detalle.
          </p>
        </div>

        {submitError ? <ErrorMessage title={submitError} /> : null}

        <div>
          <span className="field-label">Responde</span>
          <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">{user.name}</p>
        </div>

        <div>
          <label className="field-label" htmlFor="resolution-title">
            Título de la respuesta
          </label>
          <input
            id="resolution-title"
            className="field-control"
            value={title}
            maxLength={TITLE_MAX}
            placeholder="Ej: Compresor reemplazado"
            onChange={(event) => setTitle(event.target.value)}
            aria-invalid={Boolean(errors.title)}
            aria-describedby={errors.title ? 'resolution-title-error' : undefined}
          />
          {errors.title ? (
            <p id="resolution-title-error" className="field-error">
              {errors.title}
            </p>
          ) : null}
        </div>

        <div>
          <label className="field-label" htmlFor="resolution-description">
            Descripción
          </label>
          <textarea
            id="resolution-description"
            rows={5}
            className="field-control"
            value={description}
            maxLength={DESCRIPTION_MAX}
            placeholder="Qué se hizo para atender la solicitud y, si aplica, qué debe tener en cuenta el solicitante."
            onChange={(event) => setDescription(event.target.value)}
            aria-invalid={Boolean(errors.description)}
            aria-describedby={errors.description ? 'resolution-description-error' : 'resolution-description-hint'}
          />
          {errors.description ? (
            <p id="resolution-description-error" className="field-error">
              {errors.description}
            </p>
          ) : (
            <p id="resolution-description-hint" className="mt-1.5 text-xs text-slate-400">
              {description.trim().length} / {DESCRIPTION_MAX} caracteres.
            </p>
          )}
        </div>

        <p className="rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-900">
          Resolver es un estado final: después la solicitud no se podrá reabrir ni modificar.
        </p>

        <div className="flex flex-wrap items-center gap-3">
          <Button type="submit" disabled={isSubmitting}>
            Resolver y enviar respuesta
          </Button>
          <Button type="button" variant="secondary" disabled={isSubmitting} onClick={onClose}>
            Cancelar
          </Button>
          {isSubmitting ? <Spinner label="Enviando…" /> : null}
        </div>
      </form>
    </dialog>
  );
}
