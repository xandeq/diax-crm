'use client';

import { useAuth } from '@/contexts/AuthContext';
import { parseBRL } from '@/lib/packageDraft';
import {
  createServicePackage,
  listServicePackages,
  updateServicePackage,
  type ServicePackage,
  type UpsertServicePackage,
} from '@/services/servicePackages';
import { AlertCircle, ArrowLeft, Loader2, Package, Pencil, Plus } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

interface FormState {
  name: string;
  shortDescription: string;
  scope: string;
  price: string;
  deliveryDays: string;
  sortOrder: string;
  isActive: boolean;
}

const EMPTY: FormState = { name: '', shortDescription: '', scope: '', price: '', deliveryDays: '15', sortOrder: '0', isActive: true };

const toForm = (p: ServicePackage): FormState => ({
  name: p.name, shortDescription: p.shortDescription, scope: p.scope,
  price: String(p.price), deliveryDays: String(p.deliveryDays), sortOrder: String(p.sortOrder), isActive: p.isActive,
});

const inputCls = 'w-full rounded-lg bg-white/5 border border-white/10 px-3 py-2 text-sm text-white placeholder:text-white/30 focus:outline-none focus:border-violet-500/50';

export default function ServicePackagesPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [packages, setPackages] = useState<ServicePackage[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState | null>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(() => {
    setIsLoading(true);
    setError(null);
    listServicePackages(false)
      .then(setPackages)
      .catch(err => setError(err instanceof Error ? err.message : 'Erro ao carregar pacotes.'))
      .finally(() => setIsLoading(false));
  }, []);

  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }
    load();
  }, [authLoading, isAuthenticated, router, load]);

  const startNew = () => { setEditingId(null); setForm(EMPTY); setError(null); };
  const startEdit = (p: ServicePackage) => { setEditingId(p.id); setForm(toForm(p)); setError(null); };

  const save = async () => {
    if (!form) return;
    const price = parseBRL(form.price);
    const deliveryDays = Number(form.deliveryDays);
    const sortOrder = Number(form.sortOrder);
    if (!form.name.trim()) return setError('Informe o nome do pacote.');
    if (!(price > 0)) return setError('Preço inválido. Use 1800 ou 1.800,00.');
    if (!Number.isInteger(deliveryDays) || deliveryDays < 0) return setError('Prazo inválido.');
    if (!Number.isInteger(sortOrder)) return setError('Ordem inválida.');

    const data: UpsertServicePackage = {
      name: form.name.trim(), shortDescription: form.shortDescription.trim(), scope: form.scope.trim(),
      price, deliveryDays, sortOrder, isActive: form.isActive,
    };
    setSaving(true);
    setError(null);
    try {
      if (editingId) await updateServicePackage(editingId, data);
      else await createServicePackage(data);
      setForm(null);
      setEditingId(null);
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao salvar o pacote.');
    } finally {
      setSaving(false);
    }
  };

  if (authLoading || (isLoading && packages.length === 0 && !error)) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader2 className="h-8 w-8 text-violet-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="min-h-screen">
      <div className="border-b border-white/5 bg-white/[0.02]">
        <div className="max-w-screen-xl mx-auto px-6 py-5 flex items-center justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-xl font-bold text-white flex items-center gap-2">
              <Package className="h-5 w-5 text-violet-400" /> Pacotes de serviço
            </h1>
            <p className="text-[12px] text-white/40 mt-0.5">
              Preço fixo por pacote. No pipeline, &quot;Gerar proposta&quot; preenche título, escopo e valor a partir daqui.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Link href="/propostas"
              className="px-3 h-8 rounded-lg text-xs font-medium border bg-white/5 border-white/10 text-white/50 hover:text-white flex items-center gap-1.5">
              <ArrowLeft className="h-3.5 w-3.5" /> Propostas
            </Link>
            <button type="button" onClick={startNew}
              className="px-3 h-8 rounded-lg text-xs font-medium border bg-violet-600/25 border-violet-500/40 text-violet-200 hover:bg-violet-600/35 flex items-center gap-1.5">
              <Plus className="h-3.5 w-3.5" /> Novo pacote
            </button>
          </div>
        </div>
      </div>

      {error && (
        <div className="max-w-screen-xl mx-auto px-6 pt-4">
          <div className="flex items-center gap-2 rounded-xl bg-red-500/10 border border-red-500/25 px-4 py-3 text-sm text-red-300">
            <AlertCircle className="h-4 w-4 shrink-0" /> {error}
          </div>
        </div>
      )}

      <div className="max-w-screen-xl mx-auto px-6 py-6 grid gap-6 lg:grid-cols-[1fr_380px]">
        <div className="space-y-3">
          {packages.length === 0 && (
            <div className="rounded-2xl border border-white/10 bg-white/[0.02] p-8 text-center text-sm text-white/50">
              Nenhum pacote ainda. Crie de 3 a 4 pacotes com preço fixo para gerar propostas em um clique.
            </div>
          )}
          {packages.map(p => (
            <div key={p.id}
              className={`rounded-2xl border p-4 flex items-start justify-between gap-4
                ${editingId === p.id ? 'border-violet-500/40 bg-violet-500/5' : 'border-white/10 bg-white/[0.02]'}
                ${p.isActive ? '' : 'opacity-50'}`}>
              <div className="min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <h2 className="text-sm font-semibold text-white">{p.name}</h2>
                  {!p.isActive && <span className="text-[10px] px-1.5 py-0.5 rounded border border-white/15 text-white/50">inativo</span>}
                </div>
                {p.shortDescription && <p className="text-[12px] text-white/50 mt-1">{p.shortDescription}</p>}
                <p className="text-[12px] text-white/40 mt-1">
                  {p.scope.split('\n').filter(s => s.trim()).length} itens no escopo
                  {p.deliveryDays > 0 && <> · entrega em {p.deliveryDays} dias úteis</>}
                </p>
              </div>
              <div className="flex items-center gap-3 shrink-0">
                <span className="text-sm font-semibold text-emerald-300">{BRL.format(p.price)}</span>
                <button type="button" onClick={() => startEdit(p)} title={`Editar ${p.name}`}
                  className="h-8 w-8 rounded-lg bg-white/5 hover:bg-white/10 border border-white/10 flex items-center justify-center text-white/50 hover:text-white">
                  <Pencil className="h-3.5 w-3.5" />
                </button>
              </div>
            </div>
          ))}
        </div>

        {form && (
          <div className="rounded-2xl bg-[#141419] border border-white/10 p-5 space-y-3 h-fit">
            <h2 className="text-sm font-semibold text-white">{editingId ? 'Editar pacote' : 'Novo pacote'}</h2>
            <label className="block text-[11px] text-white/50">Nome
              <input className={inputCls} value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} placeholder="Site Essencial" />
            </label>
            <label className="block text-[11px] text-white/50">Resumo
              <input className={inputCls} value={form.shortDescription} onChange={e => setForm({ ...form, shortDescription: e.target.value })} placeholder="Site de 1 página para pequeno negócio" />
            </label>
            <label className="block text-[11px] text-white/50">Escopo (um item por linha)
              <textarea className={`${inputCls} min-h-[120px]`} value={form.scope} onChange={e => setForm({ ...form, scope: e.target.value })} placeholder={'Página única responsiva\nBotão de WhatsApp'} />
            </label>
            <div className="grid grid-cols-3 gap-2">
              <label className="block text-[11px] text-white/50">Preço (R$)
                <input className={inputCls} inputMode="decimal" value={form.price} onChange={e => setForm({ ...form, price: e.target.value })} placeholder="1.800,00" />
              </label>
              <label className="block text-[11px] text-white/50">Prazo (dias)
                <input className={inputCls} inputMode="numeric" value={form.deliveryDays} onChange={e => setForm({ ...form, deliveryDays: e.target.value })} />
              </label>
              <label className="block text-[11px] text-white/50">Ordem
                <input className={inputCls} inputMode="numeric" value={form.sortOrder} onChange={e => setForm({ ...form, sortOrder: e.target.value })} />
              </label>
            </div>
            <label className="flex items-center gap-2 text-[12px] text-white/60">
              <input type="checkbox" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })} />
              Ativo (aparece no &quot;Gerar proposta&quot;)
            </label>
            <div className="flex justify-end gap-2 pt-1">
              <button type="button" onClick={() => { setForm(null); setEditingId(null); }} disabled={saving}
                className="px-3 h-8 rounded-lg text-xs border bg-white/5 border-white/10 text-white/60 hover:text-white">Cancelar</button>
              <button type="button" onClick={save} disabled={saving}
                className="px-3 h-8 rounded-lg text-xs font-medium bg-violet-600 hover:bg-violet-500 text-white flex items-center gap-1.5 disabled:opacity-50">
                {saving && <Loader2 className="h-3.5 w-3.5 animate-spin" />} Salvar
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
