'use client';

import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { 
  Plus, 
  Search, 
  Edit2, 
  Trash2, 
  ChevronLeft, 
  ChevronRight, 
  Loader2,
  Filter,
  MoreHorizontal
} from 'lucide-react';
import { 
  getCustomers, 
  createCustomer, 
  updateCustomer, 
  deleteCustomer, 
  Customer, 
  CustomerStatus,
  PersonType
} from '@/services/customers';

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
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<CustomerStatus | undefined>(undefined);

  // Estados do Modal/Form
  const [isModalOpen, setIsModalOpen] = useState(false);
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
      setError('Erro ao salvar cliente. Verifique os dados.');
    } finally {
      setSubmitting(false);
    }
  };

  // Status Badge Helper
  const getStatusBadge = (status: CustomerStatus) => {
    const styles: Record<number, string> = {
      [CustomerStatus.Lead]: 'bg-blue-100 text-blue-800',
      [CustomerStatus.Contacted]: 'bg-yellow-100 text-yellow-800',
      [CustomerStatus.Qualified]: 'bg-indigo-100 text-indigo-800',
      [CustomerStatus.ProposalSent]: 'bg-purple-100 text-purple-800',
      [CustomerStatus.Negotiation]: 'bg-orange-100 text-orange-800',
      [CustomerStatus.Customer]: 'bg-green-100 text-green-800',
      [CustomerStatus.Churned]: 'bg-gray-100 text-gray-800',
      [CustomerStatus.Lost]: 'bg-red-100 text-red-800',
    };
    const label = CustomerStatus[status] || 'Desconhecido';
    const style = styles[status] || 'bg-gray-100 text-gray-800';
    
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${style}`}>
        {label}
      </span>
    );
  };

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
      </div>

      {/* Table */}
      <div className="rounded-md border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="bg-slate-50 text-slate-700 font-medium border-b border-slate-200">
              <tr>
                <th className="px-4 py-3">Nome</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Telefone / WhatsApp</th>
                <th className="px-4 py-3">Empresa</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Criado em</th>
                <th className="px-4 py-3 text-right">Ações</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {loading ? (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-slate-500">
                    <Loader2 className="h-6 w-6 animate-spin mx-auto mb-2" />
                    Carregando clientes...
                  </td>
                </tr>
              ) : customers.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-slate-500">
                    Nenhum cliente encontrado.
                  </td>
                </tr>
              ) : (
                customers.map((customer) => (
                  <tr key={customer.id} className="hover:bg-slate-50 transition-colors">
                    <td className="px-4 py-3 font-medium text-slate-900">
                      {customer.name}
                      {customer.document && (
                        <span className="block text-xs text-slate-500">{customer.document}</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-slate-600">{customer.email}</td>
                    <td className="px-4 py-3 text-slate-600">
                      <div className="flex flex-col">
                        <span>{customer.phone || '-'}</span>
                        {customer.whatsApp && (
                          <span className="text-xs text-green-600 flex items-center gap-1">
                            WA: {customer.whatsApp}
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-600">{customer.companyName || '-'}</td>
                    <td className="px-4 py-3">{getStatusBadge(customer.status)}</td>
                    <td className="px-4 py-3 text-slate-600">
                      {new Date(customer.createdAt).toLocaleDateString('pt-BR')}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-2">
                        <button 
                          onClick={() => handleOpenEdit(customer)}
                          className="p-1 hover:bg-slate-200 rounded-md text-slate-600 transition-colors"
                          title="Editar"
                        >
                          <Edit2 className="h-4 w-4" />
                        </button>
                        <button 
                          onClick={() => handleDelete(customer.id)}
                          className="p-1 hover:bg-red-100 rounded-md text-red-600 transition-colors"
                          title="Excluir"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        
        {/* Pagination */}
        <div className="flex items-center justify-between px-4 py-3 border-t border-slate-200 bg-slate-50">
          <div className="text-sm text-slate-500">
            Página {page} de {totalPages}
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-1 rounded-md border border-slate-300 bg-white disabled:opacity-50 hover:bg-slate-50"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="p-1 rounded-md border border-slate-300 bg-white disabled:opacity-50 hover:bg-slate-50"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
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
    </div>
  );
}
