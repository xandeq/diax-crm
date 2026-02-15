'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { BlogPost } from '@/types/blog';

const schema = z.object({
  title: z.string().min(3, 'Título deve ter no mínimo 3 caracteres').max(200),
  slug: z.string().max(250).regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug inválido').optional().or(z.literal('')),
  contentHtml: z.string().min(10, 'Conteúdo é obrigatório'),
  excerpt: z.string().min(10).max(500, 'Resumo deve ter no máximo 500 caracteres'),
  metaTitle: z.string().max(70).optional().or(z.literal('')),
  metaDescription: z.string().max(160).optional().or(z.literal('')),
  keywords: z.string().max(500).optional().or(z.literal('')),
  authorName: z.string().min(3, 'Nome do autor é obrigatório').max(100),
  featuredImageUrl: z.string().url('URL inválida').optional().or(z.literal('')),
  category: z.string().max(100).optional().or(z.literal('')),
  tags: z.string().max(500).optional().or(z.literal('')),
  publishImmediately: z.boolean().optional()
});

type FormValues = z.infer<typeof schema>;

interface BlogPostFormProps {
  initialData?: BlogPost;
  onSubmit: (data: FormValues) => Promise<void>;
  submitLabel?: string;
}

export function BlogPostForm({ initialData, onSubmit, submitLabel = 'Salvar' }: BlogPostFormProps) {
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: initialData?.title || '',
      slug: initialData?.slug || '',
      contentHtml: initialData?.contentHtml || '',
      excerpt: initialData?.excerpt || '',
      metaTitle: initialData?.metaTitle || '',
      metaDescription: initialData?.metaDescription || '',
      keywords: initialData?.keywords || '',
      authorName: initialData?.authorName || '',
      featuredImageUrl: initialData?.featuredImageUrl || '',
      category: initialData?.category || '',
      tags: initialData?.tagList?.join(', ') || '',
      publishImmediately: false
    }
  });

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
      {/* Conteúdo Principal */}
      <div className="grid gap-4">
        <div>
          <Label htmlFor="title">Título *</Label>
          <Input {...form.register('title')} id="title" />
          {form.formState.errors.title && (
            <p className="text-sm text-red-600 mt-1">{form.formState.errors.title.message}</p>
          )}
        </div>

        <div>
          <Label htmlFor="slug">Slug (opcional - auto-gerado se vazio)</Label>
          <Input {...form.register('slug')} id="slug" placeholder="meu-artigo-incrivel" />
          {form.formState.errors.slug && (
            <p className="text-sm text-red-600 mt-1">{form.formState.errors.slug.message}</p>
          )}
        </div>

        <div>
          <Label htmlFor="contentHtml">Conteúdo HTML *</Label>
          <Textarea
            {...form.register('contentHtml')}
            id="contentHtml"
            rows={15}
            className="font-mono text-sm"
          />
          {form.formState.errors.contentHtml && (
            <p className="text-sm text-red-600 mt-1">{form.formState.errors.contentHtml.message}</p>
          )}
        </div>

        <div>
          <Label htmlFor="excerpt">Resumo *</Label>
          <Textarea {...form.register('excerpt')} id="excerpt" rows={3} />
          {form.formState.errors.excerpt && (
            <p className="text-sm text-red-600 mt-1">{form.formState.errors.excerpt.message}</p>
          )}
        </div>
      </div>

      {/* SEO */}
      <div className="border-t pt-6">
        <h3 className="text-lg font-semibold mb-4">SEO</h3>
        <div className="grid gap-4">
          <div>
            <Label htmlFor="metaTitle">Meta Title (máx 70 chars)</Label>
            <Input {...form.register('metaTitle')} id="metaTitle" />
          </div>

          <div>
            <Label htmlFor="metaDescription">Meta Description (máx 160 chars)</Label>
            <Textarea {...form.register('metaDescription')} id="metaDescription" rows={2} />
          </div>

          <div>
            <Label htmlFor="keywords">Keywords (separadas por vírgula)</Label>
            <Input {...form.register('keywords')} id="keywords" />
          </div>
        </div>
      </div>

      {/* Metadata */}
      <div className="border-t pt-6">
        <h3 className="text-lg font-semibold mb-4">Metadados</h3>
        <div className="grid gap-4">
          <div>
            <Label htmlFor="authorName">Autor *</Label>
            <Input {...form.register('authorName')} id="authorName" />
          </div>

          <div>
            <Label htmlFor="category">Categoria</Label>
            <Input {...form.register('category')} id="category" />
          </div>

          <div>
            <Label htmlFor="tags">Tags (separadas por vírgula)</Label>
            <Input {...form.register('tags')} id="tags" placeholder="SEO, Marketing, Estratégia" />
          </div>

          <div>
            <Label htmlFor="featuredImageUrl">URL da Imagem Destacada</Label>
            <Input {...form.register('featuredImageUrl')} id="featuredImageUrl" type="url" />
          </div>
        </div>
      </div>

      {/* Publicação */}
      {!initialData && (
        <div className="border-t pt-6">
          <div className="flex items-center gap-2">
            <Checkbox
              id="publishImmediately"
              checked={form.watch('publishImmediately')}
              onCheckedChange={(checked) => form.setValue('publishImmediately', checked as boolean)}
            />
            <Label htmlFor="publishImmediately" className="cursor-pointer">
              Publicar imediatamente
            </Label>
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex gap-4">
        <Button type="submit" disabled={form.formState.isSubmitting}>
          {form.formState.isSubmitting ? 'Salvando...' : submitLabel}
        </Button>
        <Button type="button" variant="outline" onClick={() => window.history.back()}>
          Cancelar
        </Button>
      </div>
    </form>
  );
}
