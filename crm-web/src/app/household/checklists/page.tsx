'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { checklistService } from '@/services/checklistService';
import { ChecklistCategory } from '@/types/household';
import { FileJson, Plus, Settings2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { CategoryManager } from './components/CategoryManager';
import { ChecklistDialog } from './components/ChecklistDialog';
import { ChecklistTable } from './components/ChecklistTable';
import { ImportJsonDialog } from './components/ImportJsonDialog';

export default function ChecklistsPage() {
  const [categories, setCategories] = useState<ChecklistCategory[]>([]);
  const [activeCategoryId, setActiveCategoryId] = useState<string | null>(null);
  const [isItemDialogOpen, setIsItemDialogOpen] = useState(false);
  const [isImportDialogOpen, setIsImportDialogOpen] = useState(false);
  const [isCatManagerOpen, setIsCatManagerOpen] = useState(false);
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  const [categoriesLoaded, setCategoriesLoaded] = useState(false);

  useEffect(() => {
    if (!categoriesLoaded) {
      loadCategories();
    }
  }, [categoriesLoaded]);

  const loadCategories = async () => {
    try {
      const data = await checklistService.getCategories();
      setCategories(data);
      setCategoriesLoaded(true);
    } catch (error) {
      console.error('Erro ao carregar categorias:', error);
    }
  };

  const reloadCategories = async () => {
    try {
      const data = await checklistService.getCategories();
      setCategories(data);
    } catch (error) {
      console.error('Erro ao carregar categorias:', error);
    }
  };

  const handleRefresh = () => setRefreshTrigger(prev => prev + 1);

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-900">Listas e Compras</h1>
          <p className="text-slate-500">Gerencie suas listas de supermercado, farmácia e rotinas.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setIsImportDialogOpen(true)}>
            <FileJson className="mr-2 h-4 w-4" /> Importar JSON
          </Button>
          <Button variant="outline" onClick={() => setIsCatManagerOpen(true)}>
            <Settings2 className="mr-2 h-4 w-4" /> Categorias
          </Button>
          <Button onClick={() => setIsItemDialogOpen(true)}>
            <Plus className="mr-2 h-4 w-4" /> Novo Item
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-6">
        {/* Category Tabs/Grid */}
        <div className="flex flex-wrap gap-2">
          <Button
            variant={activeCategoryId === null ? "default" : "outline"}
            onClick={() => setActiveCategoryId(null)}
            className="rounded-full"
          >
            Todos
          </Button>
          {categories.map((cat) => (
            <Button
              key={cat.id}
              variant={activeCategoryId === cat.id ? "default" : "outline"}
              onClick={() => setActiveCategoryId(cat.id)}
              className="rounded-full flex items-center gap-2"
              style={{
                borderColor: activeCategoryId === cat.id ? cat.color : undefined,
                backgroundColor: activeCategoryId === cat.id ? cat.color : undefined,
                color: activeCategoryId === cat.id ? '#fff' : undefined,
              }}
            >
              {cat.icon && <span>{cat.icon}</span>}
              {cat.name}
            </Button>
          ))}
        </div>

        <Card>
          <CardContent className="p-0">
            <ChecklistTable
              categoryId={activeCategoryId}
              refreshTrigger={refreshTrigger}
              onRefresh={handleRefresh}
              categories={categories}
            />
          </CardContent>
        </Card>
      </div>

      <ChecklistDialog
        isOpen={isItemDialogOpen}
        onClose={() => setIsItemDialogOpen(false)}
        onSave={() => {
          setIsItemDialogOpen(false);
          handleRefresh();
        }}
        categories={categories}
        defaultCategoryId={activeCategoryId}
      />

      <ImportJsonDialog
        isOpen={isImportDialogOpen}
        onClose={() => setIsImportDialogOpen(false)}
        onImportSuccess={handleRefresh}
      />

      <CategoryManager
        isOpen={isCatManagerOpen}
        onClose={() => setIsCatManagerOpen(false)}
        onRefresh={() => {
          reloadCategories();
          handleRefresh();
        }}
      />
    </div>
  );
}
