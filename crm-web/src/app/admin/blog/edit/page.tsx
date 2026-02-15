'use client';

import { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { blogService } from '@/services/blogService';
import { BlogPostForm } from '@/components/admin/blog/BlogPostForm';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import type { BlogPost } from '@/types/blog';

function EditBlogPostContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const id = searchParams.get('id');
  const [post, setPost] = useState<BlogPost | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadPost();
  }, [id]);

  const loadPost = async () => {
    if (!id) return;
    try {
      const data = await blogService.getById(id);
      setPost(data);
    } catch (error) {
      toast.error('Erro ao carregar post');
      router.push('/admin/blog');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (data: any) => {
    if (!id) return;
    try {
      await blogService.update(id, data);
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

export default function EditBlogPostPage() {
  return (
    <Suspense fallback={<div className="flex justify-center items-center h-screen"><Loader2 className="h-8 w-8 animate-spin" /></div>}>
      <EditBlogPostContent />
    </Suspense>
  );
}
