'use client';

import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useState } from 'react';

export function useConfirmDialog() {
  const [state, setState] = useState<{ message: string; onConfirm: () => void } | null>(null);

  const showConfirm = (message: string, onConfirm: () => void) => {
    setState({
      message,
      onConfirm: () => {
        setState(null);
        onConfirm();
      },
    });
  };

  const confirmDialogNode = state ? (
    <ConfirmDialog
      open
      description={state.message}
      onConfirm={state.onConfirm}
      onClose={() => setState(null)}
    />
  ) : null;

  return { showConfirm, confirmDialogNode };
}
