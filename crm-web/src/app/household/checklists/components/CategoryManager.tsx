'use client';

import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { checklistService } from '@/services/checklistService';
import { ChecklistCategory, CreateChecklistCategoryRequest } from '@/types/household';
import { Plus, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';

interface CategoryManagerProps {
  isOpen: boolean;
  onClose: () => void;
  onRefresh: () => void;
}

export function CategoryManager({ isOpen, onClose, onRefresh }: CategoryManagerProps) {
  const [categories, setCategories] = useState<ChecklistCategory[]>([]);
  const [loading, setLoading] = useState(false);
  const [newCat, setNewCat] = useState<CreateChecklistCategoryRequest>({
    name: '',
    color: '#6366f1',
    displayOrder: 0
  });
  const [editingId, setEditingId] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) loadCategories();
  }, [isOpen]);

  const loadCategories = async () => {
    try {
      const data = await checklistService.getCategories();
      setCategories(data);
    } catch (error) {
      console.error(error);
    }
  };

  const handleAdd = async () => {
    if (!newCat.name) return;
    setLoading(true);
    try {
      await checklistService.createCategory(newCat);
      setNewCat({ name: '', color: '#6366f1', displayOrder: categories.length });
      loadCategories();
      onRefresh();
    } catch (error) {
      alert('Erro ao adicionar categoria');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir esta categoria? Itens nela podem ficar sem categoria.')) return;
    try {
      await checklistService.deleteCategory(id);
      loadCategories();
      onRefresh();
    } catch (error) {
      alert('Erro ao excluir categoria');
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Gerenciar Categorias</DialogTitle>
        </DialogHeader>

        <div className="space-y-6 pt-4">
          {/* New Category Form */}
          <div className="flex gap-2 items-end">
            <div className="grid gap-2 flex-1">
              <Label htmlFor="new-cat">Nova Categoria</Label>
              <Input
                id="new-cat"
                placeholder="Ex: Supermercado"
                value={newCat.name}
                onChange={(e) => setNewCat(prev => ({ ...prev, name: e.target.value }))}
              />
            </div>
            <div className="grid gap-2 w-16">
              <Label>Cor</Label>
              <Input
                type="color"
                className="p-1 h-10 w-full"
                value={newCat.color}
                onChange={(e) => setNewCat(prev => ({ ...prev, color: e.target.value }))}
              />
            </div>
            <Button onClick={handleAdd} disabled={loading || !newCat.name}>
              <Plus className="h-4 w-4" />
            </Button>
          </div>

          <div className="space-y-2 max-h-[300px] overflow-y-auto pr-2">
            {categories.map((cat) => (
              <div
                key={cat.id}
                className="flex items-center justify-between p-3 border border-slate-100 rounded-lg group hover:border-slate-200 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <div
                    className="w-3 h-3 rounded-full"
                    style={{ backgroundColor: cat.color || '#ccc' }}
                  />
                  <span className="font-medium text-slate-700">{cat.name}</span>
                </div>
                <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-slate-400 hover:text-red-500"
                    onClick={() => handleDelete(cat.id)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            ))}
            {categories.length === 0 && (
              <p className="text-center text-slate-400 text-sm py-4">Nenhuma categoria cadastrada.</p>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
