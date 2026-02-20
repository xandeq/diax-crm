'use client';

import {
    createCustomer,
    Customer,
    CustomerStatus,
    deleteCustomer,
    getCustomers,
    updateCustomer
} from '@/services/customers';
import { zodResolver } from '@hookform/resolvers/zod';
import { ColumnDef } from '@tanstack/react-table';
import {
    Building2,
    CheckCircle,
    Clock,
    Download,
    Edit2,
    Loader2,
    Mail,
    Plus,
    Search,
    Trash2,
    User,
    XCircle
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

import { DataTable } from '@/components/data-table/DataTable';
import { TableActions } from '@/components/data-table/TableActions';
import { EmailCampaignComposerModal } from '@/components/email/EmailCampaignComposerModal';
import { Badge } from '@/components/ui/badge';
import { exportToCSV } from '@/lib/export';

// Schema de validação
const customerSchema = z.object({
  name: z.string().min(3, 'Nome deve ter no mínimo 3 caracteres'),
  email: z.string().email('Email inválido'),
  phone: z.string().optional(),
  companyName: z.string().optional(),
  personType: z.any(),
  document: z.string().optional(),
  whatsApp: z.string().optional(),
  website: z.string().optional(),
  notes: z.string().optional(),
  tags: z.string().optional(),
});

type CustomerFormValues = z.infer<typeof customerSchema>;

export default function CustomersPage() {
  // Estados da Lista
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedRows, setSelectedRows] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<CustomerStatus | undefined>(undefined);

  // Estados do Modal/Form
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isComposerOpen, setIsComposerOpen] = useState(false);
  const [composerRecipients, setComposerRecipients] = useState<Array<{ id: string; name: string; email: string }>>([]);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: {
      name: '',
      email: '',
      phone: '',
      companyName: '',
      personType: 0,
      document: '',
      whatsApp: '',
      website: '',
      notes: '',
      tags: ''
    }
  });

  const personTypeWatch = watch('personType');

  // Carregar Clientes
  const fetchCustomers = async () => {
    setLoading(true);
    try {
      const data = await getCustomers(page, 10, search, statusFilter);
      setCustomers(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (err) {
      console.error(err);
      setError('Erro ao carregar clientes.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCustomers();
  }, [page, search, statusFilter]);

  // Reset para página 1 quando filtros mudam
  useEffect(() => {
    setPage(1);
  }, [search, statusFilter]);

  // Handlers
  const handleOpenCreate = () => {
    setEditingCustomer(null);
    reset({
      name: '',
      email: '',
      phone: '',
      companyName: '',
      personType: 0,
      document: '',
      whatsApp: '',
      website: '',
      notes: '',
      tags: ''
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (customer: Customer) => {
    setEditingCustomer(customer);
    setValue('name', customer.name);
    setValue('email', customer.email);
    setValue('phone', customer.phone || '');
    setValue('companyName', customer.companyName || '');
    setValue('personType', customer.personType);
    setValue('document', customer.document || '');
    setValue('whatsApp', customer.whatsApp || '');
    setValue('website', customer.website || '');
    setValue('notes', customer.notes || '');
    setValue('tags', customer.tags || '');
    setIsModalOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir este cliente?')) return;

    try {
      await deleteCustomer(id);
      fetchCustomers();
    } catch (err) {
      alert('Erro ao excluir cliente.');
    }
  };

  const handleBulkDelete = async () => {
    if (!confirm(`Deletar ${selectedRows.length} cliente(s)?`)) return;

    try {
      // TODO: Implementar endpoint bulk delete no backend
      for (const customer of selectedRows) {
        await deleteCustomer(customer.id);
      }
      fetchCustomers();
      setSelectedRows([]);
    } catch (err) {
      alert('Erro ao deletar clientes.');
    }
  };

  const handleExport = () => {
    const dataToExport = selectedRows.length > 0 ? selectedRows : customers;

    exportToCSV(
      dataToExport.map(c => ({
        Nome: c.name,
        Email: c.email,
        Telefone: c.phone || '',
        WhatsApp: c.whatsApp || '',
        Empresa: c.companyName || '',
        Documento: c.document || '',
        Status: CustomerStatus[c.status],
        'Tipo Pessoa': c.personType === 0 ? 'Física' : 'Jurídica',
        'Criado em': new Date(c.createdAt).toLocaleDateString('pt-BR'),
      })),
      `clientes-${new Date().toISOString().split('T')[0]}`
    );
  };

  const handleEmail = (customer: Customer) => {
    setComposerRecipients([{ id: customer.id, name: customer.name, email: customer.email }]);
    setIsComposerOpen(true);
  };

  const handleBulkEmail = () => {
    if (selectedRows.length === 0) {
      alert('Selecione ao menos um cliente.');
      return;
    }

    setComposerRecipients(
      selectedRows.map(customer => ({
        id: customer.id,
        name: customer.name,
        email: customer.email,
      }))
    );
    setIsComposerOpen(true);
  };

  const onSubmit = async (data: CustomerFormValues) => {
    setSubmitting(true);
    setError(null);
    try {
      const payload = { ...data, personType: Number(data.personType) };
      if (editingCustomer) {
        await updateCustomer(editingCustomer.id, payload);
      } else {
        await createCustomer(payload);
      }
      setIsModalOpen(false);
      fetchCustomers();
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Erro ao salvar cliente. Verifique os dados.');
      }
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
        style: 'bg-indigo-100 text-indigo-800',
        icon: <CheckCircle className="h-3 w-3" />,
        label: 'Qualificado'
      },
      [CustomerStatus.ProposalSent]: {
        style: 'bg-purple-100 text-purple-800',
        icon: <Clock className="h-3 w-3" />,
        label: 'Proposta Enviada'
      },
      [CustomerStatus.Negotiation]: {
        style: 'bg-orange-100 text-orange-800',
        icon: <Clock className="h-3 w-3" />,
        label: 'Negociação'
      },
      [CustomerStatus.Customer]: {
        style: 'bg-green-100 text-green-800',
        icon: <CheckCircle className="h-3 w-3" />,
        label: 'Cliente'
      },
      [CustomerStatus.Churned]: {
        style: 'bg-gray-100 text-gray-800',
        icon: <XCircle className="h-3 w-3" />,
        label: 'Churn'
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
  const columns: ColumnDef<Customer>[] = [
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
          {row.original.document && (
            <span className="block text-xs text-slate-500">{row.original.document}</span>
          )}
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
        <div className="flex justify-end gap-2">
          <button
            onClick={() => handleOpenEdit(row.original)}
            className="p-1 hover:bg-slate-200 rounded-md text-slate-600 transition-colors"
            title="Editar"
          >
            <Edit2 className="h-4 w-4" />
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
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Gerenciamento de Clientes</h1>
          <p className="text-slate-500">Visualize e gerencie sua base de clientes.</p>
        </div>
        <button
          onClick={handleOpenCreate}
          className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-slate-900 text-white hover:bg-slate-800 h-10 px-4 py-2"
        >
          <Plus className="mr-2 h-4 w-4" /> Novo Cliente
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 bg-white p-4 rounded-lg border border-slate-200 shadow-sm">
        <div className="relative flex-1">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-slate-500" />
          <input
            type="text"
            placeholder="Buscar por nome, email ou documento..."
            className="pl-9 flex h-10 w-full rounded-md border border-slate-300 bg-transparent px-3 py-2 text-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <button
          onClick={handleExport}
          className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-slate-300 bg-white hover:bg-slate-50 h-10 px-4 py-2"
        >
          <Download className="mr-2 h-4 w-4" /> Exportar
        </button>
      </div>

      {/* Bulk Actions */}
      <TableActions
        selectedCount={selectedRows.length}
        selectedRows={selectedRows}
        onDelete={handleBulkDelete}
        onExport={handleExport}
        onClearSelection={() => setSelectedRows([])}
        customActions={
          <button
            onClick={handleBulkEmail}
            className="inline-flex items-center justify-center rounded-md text-sm font-medium h-8 px-3 bg-blue-600 text-white hover:bg-blue-700"
          >
            <Mail className="mr-1.5 h-3.5 w-3.5" />
            Enviar E-mail
          </button>
        }
      />

      {/* DataTable */}
      <DataTable
        columns={columns}
        data={customers}
        loading={loading}
        selectable={true}
        onSelectionChange={setSelectedRows}
        pageSize={10}
      />

      {/* Paginação */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3 bg-white px-4 py-3 rounded-lg border border-slate-200 shadow-sm">
        <p className="text-sm text-slate-600">
          {totalCount === 0 ? 'Nenhum registro encontrado' : (
            <>Exibindo <span className="font-semibold">{((page - 1) * 10) + 1}</span> a{' '}<span className="font-semibold">{Math.min(page * 10, totalCount)}</span> de{' '}<span className="font-semibold">{totalCount}</span> clientes</>
          )}
        </p>
        {totalPages > 1 && (
          <div className="flex items-center gap-2">
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
          </div>
        )}
      </div>

      {/* Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4 overflow-y-auto">
          <div className="bg-white rounded-xl shadow-lg w-full max-w-2xl overflow-hidden animate-in fade-in zoom-in duration-200 my-8">
            <div className="px-6 py-4 border-b border-slate-100 flex justify-between items-center sticky top-0 bg-white z-10">
              <h2 className="text-lg font-semibold text-slate-900">
                {editingCustomer ? 'Editar Cliente' : 'Novo Cliente'}
              </h2>
              <button onClick={() => setIsModalOpen(false)} className="text-slate-400 hover:text-slate-600">
                ✕
              </button>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="p-6 space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Tipo de Pessoa */}
                <div className="sm:col-span-2">
                  <label className="text-sm font-medium text-slate-700 block mb-2">Tipo de Pessoa</label>
                  <div className="flex gap-4">
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="radio"
                        value="0"
                        {...register('personType')}
                        className="text-slate-900 focus:ring-slate-900"
                      />
                      <span className="text-sm text-slate-700">Pessoa Física</span>
                    </label>
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="radio"
                        value="1"
                        {...register('personType')}
                        className="text-slate-900 focus:ring-slate-900"
                      />
                      <span className="text-sm text-slate-700">Pessoa Jurídica</span>
                    </label>
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Nome Completo *</label>
                  <input
                    {...register('name')}
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="Nome do cliente"
                  />
                  {errors.name && <span className="text-xs text-red-500">{errors.name.message}</span>}
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Email *</label>
                  <input
                    {...register('email')}
                    type="email"
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="email@exemplo.com"
                  />
                  {errors.email && <span className="text-xs text-red-500">{errors.email.message}</span>}
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">
                    {Number(personTypeWatch) === 1 ? 'CNPJ' : 'CPF'}
                  </label>
                  <input
                    {...register('document')}
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder={Number(personTypeWatch) === 1 ? '00.000.000/0000-00' : '000.000.000-00'}
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Empresa</label>
                  <input
                    {...register('companyName')}
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="Nome da empresa"
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Telefone</label>
                  <input
                    {...register('phone')}
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="(00) 0000-0000"
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">WhatsApp</label>
                  <input
                    {...register('whatsApp')}
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="(00) 00000-0000"
                  />
                </div>

                <div className="space-y-2 sm:col-span-2">
                  <label className="text-sm font-medium text-slate-700">Website</label>
                  <input
                    {...register('website')}
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="https://www.exemplo.com"
                  />
                </div>

                <div className="space-y-2 sm:col-span-2">
                  <label className="text-sm font-medium text-slate-700">Tags (separadas por vírgula)</label>
                  <input
                    {...register('tags')}
                    className="flex h-10 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="vip, recorrente, indicação"
                  />
                </div>

                <div className="space-y-2 sm:col-span-2">
                  <label className="text-sm font-medium text-slate-700">Observações</label>
                  <textarea
                    {...register('notes')}
                    className="flex min-h-[80px] w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                    placeholder="Observações gerais sobre o cliente..."
                  />
                </div>
              </div>

              {error && (
                <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">
                  {error}
                </div>
              )}

              <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-md hover:bg-slate-50"
                  disabled={submitting}
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 text-sm font-medium text-white bg-slate-900 rounded-md hover:bg-slate-800 disabled:opacity-50 flex items-center gap-2"
                  disabled={submitting}
                >
                  {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
                  {editingCustomer ? 'Salvar Alterações' : 'Criar Cliente'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <EmailCampaignComposerModal
        open={isComposerOpen}
        onOpenChange={setIsComposerOpen}
        recipients={composerRecipients}
        title="Composer Profissional - Clientes"
        onQueued={(queuedCount) => {
          alert(`Campanha enfileirada com sucesso para ${queuedCount} cliente(s).`);
          setSelectedRows([]);
        }}
      />
    </div>
  );
}
