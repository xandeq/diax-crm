import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { Search, X } from 'lucide-react';

interface FiltersProps {
  search: string;
  status?: number;
  category: string;
  onSearchChange: (value: string) => void;
  onStatusChange: (value: string) => void;
  onCategoryChange: (value: string) => void;
  onClearFilters: () => void;
}

export function BlogPostFilters({
  search,
  status,
  category,
  onSearchChange,
  onStatusChange,
  onCategoryChange,
  onClearFilters
}: FiltersProps) {
  return (
    <div className="flex gap-4 mb-6">
      <div className="flex-1">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Buscar por título..."
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
            className="pl-10"
          />
        </div>
      </div>

      <Select value={status?.toString() ?? ''} onValueChange={onStatusChange}>
        <SelectTrigger className="w-[180px]">
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="">Todos</SelectItem>
          <SelectItem value="0">Rascunho</SelectItem>
          <SelectItem value="1">Publicado</SelectItem>
          <SelectItem value="2">Arquivado</SelectItem>
        </SelectContent>
      </Select>

      <Input
        placeholder="Categoria"
        value={category}
        onChange={(e) => onCategoryChange(e.target.value)}
        className="w-[200px]"
      />

      <Button variant="outline" onClick={onClearFilters}>
        <X className="h-4 w-4 mr-2" />
        Limpar
      </Button>
    </div>
  );
}
