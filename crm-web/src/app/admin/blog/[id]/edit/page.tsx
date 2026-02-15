'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { blogService } from '@/services/blogService';
import { BlogPostForm } from '@/components/admin/blog/BlogPostForm';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import type { BlogPost } from '@/types/blog';

export default function EditBlogPostPage({ params }: { params: { id: string } }) {
  const router = useRouter();
  const [post, setPost] = useState<BlogPost | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadPost();
  }, [params.id]);

  const loadPost = async () => {
    try {
      const data = await blogService.getById(params.id);
      setPost(data);
    } catch (error) {
      toast.error('Erro ao carregar post');
      router.push('/admin/blog');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (data: any) => {
    try {
      await blogService.update(params.id, data);
      toast.success('Post atualizado com sucesso!');
      router.push('/admin/blog');
    } catch (error: any) {
      toast.error(error.message || 'Erro ao atualizar post');
      throw error;
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!post) {
    return null;
  }

  return (
    <div className="container mx-auto py-8">
      <Card>
        <CardHeader>
          <CardTitle>Editar Post</CardTitle>
        </CardHeader>
        <CardContent>
          <BlogPostForm
            initialData={post}
            onSubmit={handleSubmit}
            submitLabel="Atualizar Post"
          />
        </CardContent>
      </Card>
    </div>
  );
}
