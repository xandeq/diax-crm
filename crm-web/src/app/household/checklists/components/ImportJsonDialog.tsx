'use client';

import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { checklistService } from '@/services/checklistService';
import { AlertCircle, CheckCircle2 } from 'lucide-react';
import { useState } from 'react';

interface ImportJsonDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onImportSuccess: () => void;
}

export function ImportJsonDialog({ isOpen, onClose, onImportSuccess }: ImportJsonDialogProps) {
  const [jsonInput, setJsonInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleImport = async () => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);

    try {
      let parsed;
      try {
        parsed = JSON.parse(jsonInput);
      } catch (e) {
        throw new Error('O texto inserido não é um JSON válido.');
      }

      const items = Array.isArray(parsed) ? parsed : (parsed.items || parsed.Items);
      if (!items || !Array.isArray(items)) {
        throw new Error('O JSON deve ser uma lista de itens ou conter uma propriedade "items".');
      }

      const result = await checklistService.importItems(items);
      setSuccess(`${result.importedCount} itens importados com sucesso!`);
      setJsonInput('');
      setTimeout(() => {
        onImportSuccess();
        onClose();
        setSuccess(null);
      }, 1500);

    } catch (err: any) {
      setError(err.message || 'Erro ao importar itens.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>Importar Itens via JSON</DialogTitle>
          <DialogDescription>
            Cole o JSON gerado no campo abaixo. Novas categorias serão criadas automaticamente.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
            {error && (
                <div className="bg-red-50 text-red-600 p-3 rounded-md text-sm flex items-center gap-2">
                    <AlertCircle className="h-4 w-4" />
                    {error}
                </div>
            )}
             {success && (
                <div className="bg-green-50 text-green-600 p-3 rounded-md text-sm flex items-center gap-2">
                    <CheckCircle2 className="h-4 w-4" />
                    {success}
                </div>
            )}
            <Textarea
                placeholder='[ { "title": "Arroz", "category": "Alimentação", "priority": "High" } ]'
                value={jsonInput}
                onChange={(e) => setJsonInput(e.target.value)}
                className="h-64 font-mono text-xs"
            />
            <div className="text-xs text-slate-500">
                <p>Formato esperado: Lista de objetos com <code>title</code> e <code>category</code>.</p>
                <p>Campos opcionais: <code>description</code>, <code>estimatedPrice</code>, <code>quantity</code>, <code>priority</code>.</p>
            </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancelar</Button>
          <Button onClick={handleImport} disabled={isLoading || !jsonInput.trim()}>
            {isLoading ? 'Importando...' : 'Importar ITENS'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
