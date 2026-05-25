'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { blogService } from '@/services/blogService';
import { BlogPostTable } from '@/components/admin/blog/BlogPostTable';
import { BlogPostFilters } from '@/components/admin/blog/BlogPostFilters';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { Plus } from 'lucide-react';
import { toast } from 'sonner';
import type { BlogFilters } from '@/types/blog';

export default function BlogListPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { showConfirm, confirmDialogNode } = useConfirmDialog();
  const [filters, setFilters] = useState<BlogFilters>({
    page: 1,
    pageSize: 10,
    search: '',
    category: '',
  });

  const { data, isLoading } = useQuery({
    queryKey: ['blog-posts', filters],
    queryFn: () => blogService.getAll(filters),
  });

  const posts = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['blog-posts'] });

  const publishMutation = useMutation({
    mutationFn: (id: string) => blogService.publish(id),
    onSuccess: () => { toast.success('Post publicado com sucesso!'); invalidate(); },
    onError: () => toast.error('Erro ao publicar post'),
  });

  const archiveMutation = useMutation({
    mutationFn: (id: string) => blogService.archive(id),
    onSuccess: () => { toast.success('Post arquivado com sucesso!'); invalidate(); },
    onError: () => toast.error('Erro ao arquivar post'),
  });

  const featuredMutation = useMutation({
    mutationFn: (id: string) => blogService.toggleFeatured(id),
    onSuccess: () => { toast.success('Status de destaque alterado!'); invalidate(); },
    onError: () => toast.error('Erro ao alterar destaque'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => blogService.delete(id),
    onSuccess: () => { toast.success('Post excluído com sucesso!'); invalidate(); },
    onError: () => toast.error('Erro ao excluir post'),
  });

  const handleDelete = (id: string) => {
    showConfirm(
      'Tem certeza que deseja excluir este post? Esta ação não pode ser desfeita.',
      () => deleteMutation.mutate(id),
    );
  };

  return (
    <div className="container mx-auto py-8">
      {confirmDialogNode}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Blog Posts</CardTitle>
          <Button onClick={() => router.push('/admin/blog/new')}>
            <Plus className="h-4 w-4 mr-2" />
            Novo Post
          </Button>
        </CardHeader>
        <CardContent>
          <BlogPostFilters
            search={filters.search || ''}
            status={filters.status}
            category={filters.category || ''}
            onSearchChange={(search) => setFilters({ ...filters, search, page: 1 })}
            onStatusChange={(status) =>
              setFilters({ ...filters, status: status === 'all' ? undefined : Number(status), page: 1 })
            }
            onCategoryChange={(category) => setFilters({ ...filters, category, page: 1 })}
            onClearFilters={() => setFilters({ page: 1, pageSize: 10, search: '', category: '' })}
          />

          <BlogPostTable
            data={posts}
            loading={isLoading}
            pagination={{
              page: filters.page || 1,
              pageSize: filters.pageSize || 10,
              totalCount,
              onPageChange: (page) => setFilters({ ...filters, page }),
              onPageSizeChange: (pageSize) => setFilters({ ...filters, pageSize, page: 1 }),
            }}
            onEdit={(id) => router.push(`/admin/blog/edit?id=${id}`)}
            onPublish={(id) => publishMutation.mutate(id)}
            onArchive={(id) => archiveMutation.mutate(id)}
            onToggleFeatured={(id) => featuredMutation.mutate(id)}
            onDelete={handleDelete}
          />
        </CardContent>
      </Card>
    </div>
  );
}
