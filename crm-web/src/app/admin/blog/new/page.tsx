'use client';

import { useRouter } from 'next/navigation';
import { blogService } from '@/services/blogService';
import { BlogPostForm } from '@/components/admin/blog/BlogPostForm';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { toast } from 'sonner';

export default function NewBlogPostPage() {
  const router = useRouter();

  const handleSubmit = async (data: any) => {
    try {
      await blogService.create(data);
      toast.success('Post criado com sucesso!');
      router.push('/admin/blog');
    } catch (error: any) {
      toast.error(error.message || 'Erro ao criar post');
      throw error;
    }
  };

  return (
    <div className="container mx-auto py-8">
      <Card>
        <CardHeader>
          <CardTitle>Novo Post</CardTitle>
        </CardHeader>
        <CardContent>
          <BlogPostForm onSubmit={handleSubmit} submitLabel="Criar Post" />
        </CardContent>
      </Card>
    </div>
  );
}
