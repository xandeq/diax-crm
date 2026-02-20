'use client';

import { EmailCampaignComposerModal } from '@/components/email/EmailCampaignComposerModal';
import { apiFetch } from '@/services/api';
import {
    createLead,
    CustomerStatus,
    deleteLead,
    getLeads,
    Lead,
    updateLead
} from '@/services/leads';
import { zodResolver } from '@hookform/resolvers/zod';
import { ColumnDef } from '@tanstack/react-table';
import {
    Building2,
    CheckCircle,
    Clock,
    Edit2,
    Loader2,
    Mail,
    MessageCircle,
    Plus,
    Search,
    Trash2,
    Upload,
    User,
    XCircle
} from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

import { DataTable } from '@/components/data-table/DataTable';
import { TableActions } from '@/components/data-table/TableActions';
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
import { exportToCSV } from '@/lib/export';

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
  const [selectedRows, setSelectedRows] = useState<Lead[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  // Estados do Modal/Form
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isComposerOpen, setIsComposerOpen] = useState(false);
  const [composerRecipients, setComposerRecipients] = useState<Array<{ id: string; name: string; email: string }>>([]);
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
      const data = await getLeads(page, pageSize, search, status as CustomerStatus);
      setLeads(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (err) {
      console.error(err);
      setError('Erro ao carregar leads.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLeads();
  }, [page, pageSize, search, statusFilter]);

  // Reset para página 1 quando filtros mudam
  useEffect(() => {
    setPage(1);
  }, [search, statusFilter]);

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

  const handleBulkDelete = async () => {
    if (!confirm(`Deletar ${selectedRows.length} lead(s)?`)) return;

    try {
      // TODO: Implementar endpoint bulk delete no backend
      for (const lead of selectedRows) {
        await deleteLead(lead.id);
      }
      fetchLeads();
      setSelectedRows([]);
    } catch (err) {
      alert('Erro ao deletar leads.');
    }
  };

  const handleConvert = async (lead: Lead) => {
    if (!confirm(`Converter "${lead.name}" para Cliente?`)) return;

    try {
      await apiFetch(`/customers/${lead.id}/convert`, { method: 'POST' });
      alert(`${lead.name} convertido para cliente com sucesso!`);
      fetchLeads();
    } catch (err: any) {
      alert(`Erro ao converter: ${err.message}`);
    }
  };

  const handleBulkConvert = async () => {
    if (!confirm(`Converter ${selectedRows.length} lead(s) para Cliente?`)) return;

    try {
      let converted = 0;
      for (const lead of selectedRows) {
        try {
          await apiFetch(`/customers/${lead.id}/convert`, { method: 'POST' });
          converted++;
        } catch (err) {
          console.error(`Erro ao converter ${lead.name}:`, err);
        }
      }
      alert(`${converted} de ${selectedRows.length} leads convertidos com sucesso!`);
      fetchLeads();
      setSelectedRows([]);
    } catch (err) {
      alert('Erro ao converter leads.');
    }
  };

  const handleWhatsApp = async (lead: Lead) => {
    if (!lead.phone && !lead.whatsApp) {
      alert('Este lead não possui número de telefone/WhatsApp cadastrado.');
      return;
    }

    const phone = (lead.whatsApp || lead.phone || '').replace(/\D/g, '');
    const message = encodeURIComponent(`Olá ${lead.name}, tudo bem?`);

    // Abre WhatsApp Web
    window.open(`https://wa.me/55${phone}?text=${message}`, '_blank');

    // Registra o contato
    try {
      await apiFetch(`/customers/${lead.id}/contact`, { method: 'POST' });
      fetchLeads();
    } catch (err) {
      console.error('Erro ao registrar contato:', err);
    }
  };

  const handleEmail = (lead: Lead) => {
    setComposerRecipients([{ id: lead.id, name: lead.name, email: lead.email }]);
    setIsComposerOpen(true);
  };

  const handleBulkEmail = () => {
    if (selectedRows.length === 0) {
      alert('Selecione ao menos um lead.');
      return;
    }

    setComposerRecipients(
      selectedRows.map(lead => ({
        id: lead.id,
        name: lead.name,
        email: lead.email,
      }))
    );
    setIsComposerOpen(true);
  };

  const handleExport = () => {
    const dataToExport = selectedRows.length > 0 ? selectedRows : leads;

    exportToCSV(
      dataToExport.map(l => ({
        Nome: l.name,
        Email: l.email,
        Telefone: l.phone || '',
        WhatsApp: l.whatsApp || '',
        Empresa: l.companyName || '',
        Status: CustomerStatus[l.status],
        'Tipo Pessoa': l.personType === 0 ? 'Física' : 'Jurídica',
        'Criado em': new Date(l.createdAt).toLocaleDateString('pt-BR'),
      })),
      `leads-${new Date().toISOString().split('T')[0]}`
    );
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

  // Status Badge Helper com ícones
  const getStatusBadge = (status: CustomerStatus) => {
    const configs: Record<number, { style: string; icon: React.ReactNode; label: string }> = {
      [CustomerStatus.Lead]: {
        style: 'bg-blue-100 text-blue-800',
        icon: <Clock className="h-3 w-3" />,
        label: 'Lead'
      },
      [CustomerStatus.Contacted]: {
        style: 'bg-yellow-100 text-yellow-800',
        icon: <Clock className="h-3 w-3" />,
        label: 'Contatado'
      },
      [CustomerStatus.Qualified]: {
        style: 'bg-green-100 text-green-800',
        icon: <CheckCircle className="h-3 w-3" />,
        label: 'Qualificado'
      },
      [CustomerStatus.Lost]: {
        style: 'bg-red-100 text-red-800',
        icon: <XCircle className="h-3 w-3" />,
        label: 'Perdido'
      },
    };

    const config = configs[status] || {
      style: 'bg-gray-100 text-gray-800',
      icon: null,
      label: 'Desconhecido'
    };

    return (
      <Badge className={`flex items-center gap-1 ${config.style}`}>
        {config.icon}
        <span>{config.label}</span>
      </Badge>
    );
  };

  // Definição das colunas
  const columns: ColumnDef<Lead>[] = [
    {
      accessorKey: 'name',
      header: 'Nome',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-slate-900 flex items-center gap-2">
            {row.original.personType === 1 ? (
              <Building2 className="h-3.5 w-3.5 text-slate-400" />
            ) : (
              <User className="h-3.5 w-3.5 text-slate-400" />
            )}
            {row.original.name}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'email',
      header: 'Email',
      cell: ({ row }) => (
        <span className="text-slate-600">{row.original.email}</span>
      ),
    },
    {
      accessorKey: 'phone',
      header: 'Telefone',
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="text-slate-600">{row.original.phone || '-'}</span>
          {row.original.whatsApp && (
            <span className="text-xs text-green-600 flex items-center gap-1">
              <span className="inline-block w-1.5 h-1.5 bg-green-500 rounded-full"></span>
              WA: {row.original.whatsApp}
            </span>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'companyName',
      header: 'Empresa',
      cell: ({ row }) => (
        <span className="text-slate-600">{row.original.companyName || '-'}</span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      accessorKey: 'createdAt',
      header: 'Criado em',
      cell: ({ row }) => (
        <span className="text-slate-600">
          {new Date(row.original.createdAt).toLocaleDateString('pt-BR')}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Ações',
      cell: ({ row }) => (
        <div className="flex justify-end gap-1">
          <button
            onClick={() => handleOpenEdit(row.original)}
            className="p-1 hover:bg-slate-200 rounded-md text-slate-600 transition-colors"
            title="Editar"
          >
            <Edit2 className="h-4 w-4" />
          </button>
          {row.original.status !== CustomerStatus.Customer && (
            <button
              onClick={() => handleConvert(row.original)}
              className="p-1 hover:bg-green-100 rounded-md text-green-600 transition-colors"
              title="Converter para Cliente"
            >
              <CheckCircle className="h-4 w-4" />
            </button>
          )}
          <button
            onClick={() => handleWhatsApp(row.original)}
            disabled={!row.original.phone && !row.original.whatsApp}
            className="p-1 hover:bg-green-100 rounded-md text-green-600 transition-colors disabled:opacity-30"
            title="Enviar WhatsApp"
          >
            <MessageCircle className="h-4 w-4" />
          </button>
          <button
            onClick={() => handleEmail(row.original)}
            className="p-1 hover:bg-blue-100 rounded-md text-blue-600 transition-colors"
            title="Enviar Email"
          >
            <Mail className="h-4 w-4" />
          </button>
          <button
            onClick={() => handleDelete(row.original.id)}
            className="p-1 hover:bg-red-100 rounded-md text-red-600 transition-colors"
            title="Excluir"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6 p-8 pt-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight font-display text-slate-900">Gerenciamento de Leads</h1>
          <p className="text-slate-500">Visualize e gerencie seus potenciais clientes.</p>
        </div>
        <div className="flex gap-2">
          <Link href="/leads/import">
            <Button variant="outline">
              <Upload className="mr-2 h-4 w-4" /> Importar
            </Button>
          </Link>
          <Button onClick={handleOpenCreate}>
            <Plus className="mr-2 h-4 w-4" /> Novo Lead
          </Button>
        </div>
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

      {/* Bulk Actions */}
      <TableActions
        selectedCount={selectedRows.length}
        selectedRows={selectedRows}
        onDelete={handleBulkDelete}
        onExport={handleExport}
        onClearSelection={() => setSelectedRows([])}
        customActions={
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              onClick={handleBulkEmail}
              className="h-8 bg-blue-600 hover:bg-blue-700 text-white"
            >
              <Mail className="h-3.5 w-3.5 mr-1.5" />
              Enviar E-mail
            </Button>
            <Button
              size="sm"
              onClick={handleBulkConvert}
              className="h-8 bg-green-600 hover:bg-green-700 text-white"
            >
              <CheckCircle className="h-3.5 w-3.5 mr-1.5" />
              Converter Selecionados
            </Button>
          </div>
        }
      />

      {/* DataTable */}
      <DataTable
        columns={columns}
        data={leads}
        loading={loading}
        selectable={true}
        onSelectionChange={setSelectedRows}
        pageSize={pageSize}
      />

      {/* Paginação */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-white px-4 py-3 rounded-lg border border-slate-200 shadow-sm">
        {/* Esquerda: contagem + dropdown de itens por página */}
        <div className="flex items-center gap-3 flex-wrap">
          <p className="text-sm text-slate-600">
            {totalCount === 0 ? 'Nenhum registro encontrado' : (
              <>Exibindo <span className="font-semibold">{((page - 1) * pageSize) + 1}</span> a{' '}<span className="font-semibold">{Math.min(page * pageSize, totalCount)}</span> de{' '}<span className="font-semibold">{totalCount}</span> leads</>
            )}
          </p>
          <div className="flex items-center gap-1.5">
            <span className="text-sm text-slate-500">Exibir</span>
            <select
              value={pageSize}
              onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
              className="h-8 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-slate-400 cursor-pointer"
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
            <span className="text-sm text-slate-500">por página</span>
          </div>
        </div>
        {/* Direita: botões de navegação */}
        {totalPages > 1 && (
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage(1)}
              disabled={page === 1}
              className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed h-9 px-2.5 transition-colors"
              title="Primeira página"
            >
              «
            </button>
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed h-9 px-3 transition-colors"
            >
              ← Anterior
            </button>
            <div className="flex items-center gap-1">
              {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => {
                let pageNum: number;
                if (totalPages <= 7) {
                  pageNum = i + 1;
                } else if (page <= 4) {
                  pageNum = i + 1;
                } else if (page >= totalPages - 3) {
                  pageNum = totalPages - 6 + i;
                } else {
                  pageNum = page - 3 + i;
                }
                return (
                  <button
                    key={pageNum}
                    onClick={() => setPage(pageNum)}
                    className={`inline-flex items-center justify-center rounded-md text-sm font-medium h-9 w-9 transition-colors ${
                      pageNum === page
                        ? 'bg-slate-900 text-white'
                        : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50'
                    }`}
                  >
                    {pageNum}
                  </button>
                );
              })}
            </div>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed h-9 px-3 transition-colors"
            >
              Próximo →
            </button>
            <button
              onClick={() => setPage(totalPages)}
              disabled={page === totalPages}
              className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed h-9 px-2.5 transition-colors"
              title="Última página"
            >
              »
            </button>
          </div>
        )}
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

      <EmailCampaignComposerModal
        open={isComposerOpen}
        onOpenChange={setIsComposerOpen}
        recipients={composerRecipients}
        title="Composer Profissional - Leads"
        onQueued={(queuedCount) => {
          alert(`Campanha enfileirada com sucesso para ${queuedCount} lead(s).`);
          setSelectedRows([]);
        }}
      />
    </div>
  );
}
