'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { blogService } from '@/services/blogService';
import { BlogPostTable } from '@/components/admin/blog/BlogPostTable';
import { BlogPostFilters } from '@/components/admin/blog/BlogPostFilters';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Plus } from 'lucide-react';
import { toast } from 'sonner';
import type { BlogPost, BlogFilters } from '@/types/blog';

export default function BlogListPage() {
  const router = useRouter();
  const [posts, setPosts] = useState<BlogPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<BlogFilters>({
    page: 1,
    pageSize: 10,
    search: '',
    category: ''
  });
  const [totalCount, setTotalCount] = useState(0);

  useEffect(() => {
    loadPosts();
  }, [filters]);

  const loadPosts = async () => {
    setLoading(true);
    try {
      const response = await blogService.getAll(filters);
      setPosts(response.items);
      setTotalCount(response.totalCount);
    } catch (error) {
      toast.error('Erro ao carregar posts');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handlePublish = async (id: string) => {
    try {
      await blogService.publish(id);
      toast.success('Post publicado com sucesso!');
      loadPosts();
    } catch (error) {
      toast.error('Erro ao publicar post');
    }
  };

  const handleArchive = async (id: string) => {
    try {
      await blogService.archive(id);
      toast.success('Post arquivado com sucesso!');
      loadPosts();
    } catch (error) {
      toast.error('Erro ao arquivar post');
    }
  };

  const handleToggleFeatured = async (id: string) => {
    try {
      await blogService.toggleFeatured(id);
      toast.success('Status de destaque alterado!');
      loadPosts();
    } catch (error) {
      toast.error('Erro ao alterar destaque');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir este post? Esta ação não pode ser desfeita.')) {
      return;
    }

    try {
      await blogService.delete(id);
      toast.success('Post excluído com sucesso!');
      loadPosts();
    } catch (error) {
      toast.error('Erro ao excluir post');
    }
  };

  return (
    <div className="container mx-auto py-8">
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
            onStatusChange={(status) => setFilters({ ...filters, status: status ? Number(status) : undefined, page: 1 })}
            onCategoryChange={(category) => setFilters({ ...filters, category, page: 1 })}
            onClearFilters={() => setFilters({ page: 1, pageSize: 10, search: '', category: '' })}
          />

          <BlogPostTable
            data={posts}
            loading={loading}
            pagination={{
              page: filters.page || 1,
              pageSize: filters.pageSize || 10,
              totalCount,
              onPageChange: (page) => setFilters({ ...filters, page }),
              onPageSizeChange: (pageSize) => setFilters({ ...filters, pageSize, page: 1 })
            }}
            onEdit={(id) => router.push(`/admin/blog/${id}/edit`)}
            onPublish={handlePublish}
            onArchive={handleArchive}
            onToggleFeatured={handleToggleFeatured}
            onDelete={handleDelete}
          />
        </CardContent>
      </Card>
    </div>
  );
}
