'use client';

import { Button } from '@/components/ui/Button';
import { ErrorMessage } from '@/components/ui/Feedback';

export default function GlobalError({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <div className="mx-auto max-w-lg space-y-4 py-10">
      <ErrorMessage title="Ocurrió un error al mostrar esta página.">{error.message}</ErrorMessage>
      <Button onClick={reset}>Reintentar</Button>
    </div>
  );
}
