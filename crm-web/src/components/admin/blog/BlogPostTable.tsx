'use client';

import { useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { FinancialGrid } from '@/components/finance/FinancialGrid';
import { StatusBadge } from './StatusBadge';
import { Button } from '@/components/ui/button';
import { Edit, Star, StarOff, Archive, Trash2 } from 'lucide-react';
import { BlogPost } from '@/types/blog';
import { formatDate } from '@/lib/date-utils';

interface BlogPostTableProps {
  data: BlogPost[];
  loading: boolean;
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    onPageChange: (page: number) => void;
    onPageSizeChange: (pageSize: number) => void;
  };
  onEdit: (id: string) => void;
  onPublish: (id: string) => void;
  onArchive: (id: string) => void;
  onToggleFeatured: (id: string) => void;
  onDelete: (id: string) => void;
}

export function BlogPostTable({
  data,
  loading,
  pagination,
  onEdit,
  onPublish,
  onArchive,
  onToggleFeatured,
  onDelete
}: BlogPostTableProps) {
  const columns = useMemo<ColumnDef<BlogPost>[]>(() => [
    {
      accessorKey: 'title',
      header: 'Título',
      cell: ({ row }) => (
        <div className="max-w-md">
          <div className="font-medium">{row.original.title}</div>
          <div className="text-sm text-slate-500">{row.original.slug}</div>
        </div>
      )
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => (
        <StatusBadge
          status={row.original.status}
          statusDescription={row.original.statusDescription}
        />
      )
    },
    {
      accessorKey: 'category',
      header: 'Categoria',
      cell: ({ row }) => row.original.category || '-'
    },
    {
      accessorKey: 'authorName',
      header: 'Autor'
    },
    {
      accessorKey: 'viewCount',
      header: 'Views',
      cell: ({ row }) => row.original.viewCount.toLocaleString('pt-BR')
    },
    {
      accessorKey: 'isFeatured',
      header: 'Destaque',
      cell: ({ row }) => (
        row.original.isFeatured ? (
          <Star className="h-4 w-4 text-yellow-500 fill-yellow-500" />
        ) : null
      )
    },
    {
      accessorKey: 'publishedAt',
      header: 'Publicado em',
      cell: ({ row }) => row.original.publishedAt ? formatDate(row.original.publishedAt) : '-'
    },
    {
      accessorKey: 'createdAt',
      header: 'Criado em',
      cell: ({ row }) => formatDate(row.original.createdAt)
    },
    {
      id: 'actions',
      header: 'Ações',
      cell: ({ row }) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => onEdit(row.original.id)}>
            <Edit className="h-4 w-4" />
          </Button>

          {row.original.status !== 'Published' && (
            <Button variant="ghost" size="sm" onClick={() => onPublish(row.original.id)}>
              Publicar
            </Button>
          )}

          {row.original.status !== 'Archived' && (
            <Button variant="ghost" size="sm" onClick={() => onArchive(row.original.id)}>
              <Archive className="h-4 w-4" />
            </Button>
          )}

          <Button
            variant="ghost"
            size="sm"
            onClick={() => onToggleFeatured(row.original.id)}
          >
            {row.original.isFeatured ? (
              <StarOff className="h-4 w-4" />
            ) : (
              <Star className="h-4 w-4" />
            )}
          </Button>

          <Button
            variant="ghost"
            size="sm"
            onClick={() => onDelete(row.original.id)}
            className="text-red-600 hover:text-red-700"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      )
    }
  ], [onEdit, onPublish, onArchive, onToggleFeatured, onDelete]);

  return (
    <FinancialGrid
      columns={columns}
      data={data}
      pageCount={Math.ceil(pagination.totalCount / pagination.pageSize)}
      page={pagination.page}
      pageSize={pagination.pageSize}
      onPageChange={pagination.onPageChange}
      onPageSizeChange={pagination.onPageSizeChange}
      loading={loading}
      getRowId={(row) => row.id}
    />
  );
}
