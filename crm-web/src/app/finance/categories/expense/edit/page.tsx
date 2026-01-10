'use client';

import { financeService } from '@/services/finance';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

const editCategorySchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório'),
  isActive: z.boolean(),
});

type EditCategoryForm = z.infer<typeof editCategorySchema>;

function EditExpenseCategoryContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const id = searchParams.get('id');
  const [loading, setLoading] = useState(true);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<EditCategoryForm>({
    resolver: zodResolver(editCategorySchema),
    defaultValues: {
      isActive: true,
    },
  });

  useEffect(() => {
    if (id) {
      loadCategory();
    }
  }, [id]);

  const loadCategory = async () => {
    if (!id) return;
    try {
      const category = await financeService.getExpenseCategoryById(id);
      setValue('name', category.name);
      setValue('isActive', category.isActive);
    } catch (err) {
      console.error(err);
      alert('Erro ao carregar categoria');
      router.push('/finance/categories/expense');
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (data: EditCategoryForm) => {
    if (!id) return;
    try {
      await financeService.updateExpenseCategory(id, data);
      router.push('/finance/categories/expense');
    } catch (err) {
      console.error(err);
      alert('Erro ao atualizar categoria');
    }
  };

  if (!id) {
    return <div className="p-8 text-red-600">ID da categoria não informado</div>;
  }

  if (loading) return <div className="p-8">Carregando...</div>;

  return (
    <div className="p-8 max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Editar Categoria de Despesa</h1>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">Nome</label>
          <input
            type="text"
            {...register('name')}
            className="w-full border rounded p-2"
          />
          {errors.name && <p className="text-red-600 text-sm">{errors.name.message}</p>}
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="isActive"
            {...register('isActive')}
            className="rounded"
          />
          <label htmlFor="isActive" className="text-sm font-medium">Categoria Ativa</label>
        </div>

        <div className="flex gap-4">
          <button
            type="submit"
            disabled={isSubmitting}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {isSubmitting ? 'Salvando...' : 'Salvar'}
          </button>
          <button
            type="button"
            onClick={() => router.push('/finance/categories/expense')}
            className="bg-gray-300 text-gray-700 px-4 py-2 rounded hover:bg-gray-400"
          >
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

export default function EditExpenseCategoryPage() {
  return (
    <Suspense fallback={<div className="p-8">Carregando...</div>}>
      <EditExpenseCategoryContent />
    </Suspense>
  );
}
