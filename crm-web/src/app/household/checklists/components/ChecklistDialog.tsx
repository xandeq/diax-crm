'use client';

import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { checklistService } from '@/services/checklistService';
import {
    ChecklistCategory,
    ChecklistItem,
    ChecklistPriority,
    CreateChecklistItemRequest,
    UpdateChecklistItemRequest
} from '@/types/household';
import { useEffect, useState } from 'react';

interface ChecklistDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: () => void;
  categories: ChecklistCategory[];
  itemToEdit?: ChecklistItem | null;
  defaultCategoryId?: string | null;
}

export function ChecklistDialog({
  isOpen,
  onClose,
  onSave,
  categories,
  itemToEdit,
  defaultCategoryId
}: ChecklistDialogProps) {
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState<Partial<ChecklistItem>>({
    title: '',
    description: '',
    categoryId: defaultCategoryId || (categories.length > 0 ? categories[0].id : ''),
    priority: ChecklistPriority.Medium,
    quantity: 1,
    estimatedPrice: undefined,
    storeOrLink: ''
  });

  useEffect(() => {
    if (itemToEdit) {
      setFormData(itemToEdit);
    } else {
      setFormData({
        title: '',
        description: '',
        categoryId: defaultCategoryId || (categories.length > 0 ? categories[0].id : ''),
        priority: ChecklistPriority.Medium,
        quantity: 1,
        estimatedPrice: undefined,
        storeOrLink: ''
      });
    }
  }, [itemToEdit, defaultCategoryId, categories, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      if (itemToEdit) {
        await checklistService.updateItem(itemToEdit.id, formData as UpdateChecklistItemRequest);
      } else {
        await checklistService.createItem(formData as CreateChecklistItemRequest);
      }
      onSave();
    } catch (error) {
      alert('Erro ao salvar item.');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (field: string, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{itemToEdit ? 'Editar Item' : 'Novo Item na Lista'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="title">O que você precisa?</Label>
            <Input
              id="title"
              placeholder="Ex: Arroz Tio João 5kg"
              value={formData.title || ''}
              onChange={(e) => handleChange('title', e.target.value)}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>Categoria</Label>
              <Select
                value={formData.categoryId}
                onValueChange={(v) => handleChange('categoryId', v)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione..." />
                </SelectTrigger>
                <SelectContent>
                  {categories.map(c => (
                    <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-2">
              <Label>Prioridade</Label>
              <Select
                value={formData.priority?.toString()}
                onValueChange={(v) => handleChange('priority', parseInt(v))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0 text-slate-400">Baixa</SelectItem>
                  <SelectItem value="1 text-blue-500">Média</SelectItem>
                  <SelectItem value="2 text-orange-500">Alta</SelectItem>
                  <SelectItem value="3 text-red-600">Urgente</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="quantity">Quantidade</Label>
              <Input
                id="quantity"
                type="number"
                min="1"
                value={formData.quantity || 1}
                onChange={(e) => handleChange('quantity', parseInt(e.target.value))}
              />
            </div>
            <div className="grid gap-2 col-span-2">
              <Label htmlFor="price">Estimativa de Preço (R$)</Label>
              <Input
                id="price"
                type="number"
                step="0.01"
                placeholder="0,00"
                value={formData.estimatedPrice || ''}
                onChange={(e) => handleChange('estimatedPrice', e.target.value ? parseFloat(e.target.value) : undefined)}
              />
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="link">Loja ou Link</Label>
            <Input
              id="link"
              placeholder="https://... ou Nome do Mercado"
              value={formData.storeOrLink || ''}
              onChange={(e) => handleChange('storeOrLink', e.target.value)}
            />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="desc">Observações</Label>
            <Textarea
              id="desc"
              placeholder="Ex: Marca específica, tamanho, etc."
              value={formData.description || ''}
              onChange={(e) => handleChange('description', e.target.value)}
            />
          </div>

          <DialogFooter className="pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={loading}>
              Cancelar
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Salvando...' : (itemToEdit ? 'Atualizar Item' : 'Adicionar à Lista')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
