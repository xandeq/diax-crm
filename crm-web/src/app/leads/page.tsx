'use client';

import {
    createLead,
    CustomerStatus,
    deleteLead,
    getLeads,
    Lead,
    updateLead
} from '@/services/leads';
import { zodResolver } from '@hookform/resolvers/zod';
import {
    ChevronLeft,
    ChevronRight,
    Edit2,
    Loader2,
    Plus,
    Search,
    Trash2
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';

// Schema de validação
const leadSchema = z.object({
  name: z.string().min(3, 'Nome deve ter no mínimo 3 caracteres'),
  email: z.string().email('Email inválido'),
  phone: z.string().optional(),
  companyName: z.string().optional(),
  personType: z.number(),
});

type LeadFormValues = z.infer<typeof leadSchema>;

export default function LeadsPage() {
  // Estados da Lista
  const [leads, setLeads] = useState<Lead[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  // Estados do Modal/Form
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingLead, setEditingLead] = useState<Lead | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<LeadFormValues>({
    resolver: zodResolver(leadSchema),
    defaultValues: {
      name: '',
      email: '',
      phone: '',
      companyName: '',
      personType: 0
    }
  });

  // Carregar Leads
  const fetchLeads = async () => {
    setLoading(true);
    try {
      const status = statusFilter === 'all' ? undefined : Number(statusFilter);
      const data = await getLeads(page, 10, search, status as CustomerStatus);
      setLeads(data.items);
      setTotalPages(data.totalPages);
    } catch (err) {
      console.error(err);
      setError('Erro ao carregar leads.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLeads();
  }, [page, search, statusFilter]);

  // Handlers
  const handleOpenCreate = () => {
    setEditingLead(null);
    reset({ name: '', email: '', phone: '', companyName: '', personType: 0 });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (lead: Lead) => {
    setEditingLead(lead);
    setValue('name', lead.name);
    setValue('email', lead.email);
    setValue('phone', lead.phone || '');
    setValue('companyName', lead.companyName || '');
    setValue('personType', lead.personType);
    setIsModalOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir este lead?')) return;

    try {
      await deleteLead(id);
      fetchLeads();
    } catch (err) {
      alert('Erro ao excluir lead.');
    }
  };

  const onSubmit = async (data: LeadFormValues) => {
    setSubmitting(true);
    setError(null);
    try {
      if (editingLead) {
        await updateLead(editingLead.id, data);
      } else {
        await createLead(data);
      }
      setIsModalOpen(false);
      fetchLeads();
    } catch (err) {
      setError('Erro ao salvar lead. Verifique os dados.');
    } finally {
      setSubmitting(false);
    }
  };

  // Status Badge Helper
  const getStatusBadge = (status: CustomerStatus) => {
    const styles = {
      [CustomerStatus.Lead]: 'bg-blue-100 text-blue-800 hover:bg-blue-100/80',
      [CustomerStatus.Contacted]: 'bg-yellow-100 text-yellow-800 hover:bg-yellow-100/80',
      [CustomerStatus.Qualified]: 'bg-green-100 text-green-800 hover:bg-green-100/80',
      [CustomerStatus.Lost]: 'bg-red-100 text-red-800 hover:bg-red-100/80',
    };
    const label = CustomerStatus[status] || 'Desconhecido';
    const style = styles[status as keyof typeof styles] || 'bg-gray-100 text-gray-800';

    return (
      <Badge variant="secondary" className={style}>
        {label}
      </Badge>
    );
  };

  return (
    <div className="space-y-6 p-8 pt-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight font-display text-slate-900">Gerenciamento de Leads</h1>
          <p className="text-slate-500">Visualize e gerencie seus potenciais clientes.</p>
        </div>
        <Button onClick={handleOpenCreate}>
          <Plus className="mr-2 h-4 w-4" /> Novo Lead
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 items-center">
        <div className="relative flex-1 w-full">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-slate-500" />
          <Input
            placeholder="Buscar por nome ou email..."
            className="pl-9"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="w-full sm:w-[200px]">
          <Select
            value={statusFilter}
            onValueChange={setStatusFilter}
          >
            <SelectTrigger>
              <SelectValue placeholder="Filtrar por status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos os Status</SelectItem>
              <SelectItem value={String(CustomerStatus.Lead)}>Lead</SelectItem>
              <SelectItem value={String(CustomerStatus.Contacted)}>Contacted</SelectItem>
              <SelectItem value={String(CustomerStatus.Qualified)}>Qualified</SelectItem>
              <SelectItem value={String(CustomerStatus.Lost)}>Lost</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Table */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nome</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Telefone</TableHead>
              <TableHead>Empresa</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Criado em</TableHead>
              <TableHead className="text-right">Ações</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={7} className="h-24 text-center">
                  <Loader2 className="h-6 w-6 animate-spin mx-auto" />
                </TableCell>
              </TableRow>
            ) : leads.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="h-24 text-center">
                  Nenhum lead encontrado.
                </TableCell>
              </TableRow>
            ) : (
              leads.map((lead) => (
                <TableRow key={lead.id}>
                  <TableCell className="font-medium">{lead.name}</TableCell>
                  <TableCell>{lead.email}</TableCell>
                  <TableCell>{lead.phone || '-'}</TableCell>
                  <TableCell>{lead.companyName || '-'}</TableCell>
                  <TableCell>{getStatusBadge(lead.status)}</TableCell>
                  <TableCell>
                    {new Date(lead.createdAt).toLocaleDateString('pt-BR')}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenEdit(lead)}
                        title="Editar"
                      >
                        <Edit2 className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(lead.id)}
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                        title="Excluir"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-end space-x-2 py-4">
        <div className="text-sm text-slate-500 mr-4">
            Página {page} de {totalPages}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setPage(p => Math.max(1, p - 1))}
          disabled={page === 1}
        >
          <ChevronLeft className="h-4 w-4" />
          Anterior
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setPage(p => Math.min(totalPages, p + 1))}
          disabled={page === totalPages}
        >
          Próximo
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>

      {/* Modal */}
      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{editingLead ? 'Editar Lead' : 'Novo Lead'}</DialogTitle>
          </DialogHeader>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="name">Nome Completo</Label>
              <Input
                id="name"
                {...register('name')}
                placeholder="Ex: João Silva"
              />
              {errors.name && <p className="text-xs text-red-500">{errors.name.message}</p>}
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  {...register('email')}
                  placeholder="joao@empresa.com"
                />
                {errors.email && <p className="text-xs text-red-500">{errors.email.message}</p>}
              </div>

              <div className="space-y-2">
                <Label htmlFor="phone">Telefone</Label>
                <Input
                  id="phone"
                  {...register('phone')}
                  placeholder="(11) 99999-9999"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="companyName">Empresa</Label>
              <Input
                id="companyName"
                {...register('companyName')}
                placeholder="Empresa LTDA"
              />
            </div>

            {error && (
              <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">
                {error}
              </div>
            )}

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsModalOpen(false)}
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={submitting}
              >
                {submitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {editingLead ? 'Salvar Alterações' : 'Criar Lead'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
