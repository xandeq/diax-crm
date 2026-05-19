'use client';

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
import { createCustomer, Customer, updateCustomer } from '@/services/customers';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

const customerSchema = z.object({
  name: z.string().min(3, 'Nome deve ter no mínimo 3 caracteres'),
  email: z.string().email('Email inválido'),
  phone: z.string().optional(),
  companyName: z.string().optional(),
  personType: z.string(),
  document: z.string().optional(),
  whatsApp: z.string().optional(),
  website: z.string().optional(),
  notes: z.string().optional(),
  tags: z.string().optional(),
});

type CustomerFormValues = z.infer<typeof customerSchema>;

interface CustomerFormDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  editingCustomer: Customer | null;
}

export function CustomerFormDialog({ open, onClose, onSuccess, editingCustomer }: CustomerFormDialogProps) {
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: { name: '', email: '', phone: '', companyName: '', personType: '0', document: '', whatsApp: '', website: '', notes: '', tags: '' },
  });

  const personTypeWatch = watch('personType');

  useEffect(() => {
    if (!open) return;
    if (editingCustomer) {
      reset({
        name: editingCustomer.name,
        email: editingCustomer.email || '',
        phone: editingCustomer.phone || '',
        companyName: editingCustomer.companyName || '',
        personType: String(editingCustomer.personType ?? 0),
        document: editingCustomer.document || '',
        whatsApp: editingCustomer.whatsApp || '',
        website: editingCustomer.website || '',
        notes: editingCustomer.notes || '',
        tags: editingCustomer.tags || '',
      });
    } else {
      reset({ name: '', email: '', phone: '', companyName: '', personType: '0', document: '', whatsApp: '', website: '', notes: '', tags: '' });
    }
    setFormError(null);
  }, [open, editingCustomer, reset]);

  const onSubmit = async (data: CustomerFormValues) => {
    setSubmitting(true);
    setFormError(null);
    try {
      const payload = { ...data, personType: Number(data.personType) };
      if (editingCustomer) {
        await updateCustomer(editingCustomer.id, payload);
      } else {
        await createCustomer(payload);
      }
      onClose();
      onSuccess();
    } catch (err) {
      if (err instanceof Error) {
        setFormError(err.message);
      } else {
        setFormError('Erro ao salvar cliente. Verifique os dados.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {editingCustomer ? 'Editar Cliente' : 'Novo Cliente'}
          </DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <Label className="mb-2 block">Tipo de Pessoa</Label>
              <div className="flex gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="radio" value="0" {...register('personType')} className="text-slate-900 focus:ring-slate-900" />
                  <span className="text-sm text-slate-700">Pessoa Física</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="radio" value="1" {...register('personType')} className="text-slate-900 focus:ring-slate-900" />
                  <span className="text-sm text-slate-700">Pessoa Jurídica</span>
                </label>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="cust-name">Nome Completo *</Label>
              <Input id="cust-name" {...register('name')} placeholder="Nome do cliente" />
              {errors.name && <span className="text-xs text-red-500">{errors.name.message}</span>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="cust-email">Email *</Label>
              <Input id="cust-email" type="email" {...register('email')} placeholder="email@exemplo.com" />
              {errors.email && <span className="text-xs text-red-500">{errors.email.message}</span>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="cust-document">
                {Number(personTypeWatch) === 1 ? 'CNPJ' : 'CPF'}
              </Label>
              <Input
                id="cust-document"
                {...register('document')}
                placeholder={Number(personTypeWatch) === 1 ? '00.000.000/0000-00' : '000.000.000-00'}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="cust-companyName">Empresa</Label>
              <Input id="cust-companyName" {...register('companyName')} placeholder="Nome da empresa" />
            </div>

            <div className="space-y-2">
              <Label htmlFor="cust-phone">Telefone</Label>
              <Input id="cust-phone" {...register('phone')} placeholder="(00) 0000-0000" />
            </div>

            <div className="space-y-2">
              <Label htmlFor="cust-whatsApp">WhatsApp</Label>
              <Input id="cust-whatsApp" {...register('whatsApp')} placeholder="(00) 00000-0000" />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="cust-website">Website</Label>
              <Input id="cust-website" {...register('website')} placeholder="https://www.exemplo.com" />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="cust-tags">Tags (separadas por vírgula)</Label>
              <Input id="cust-tags" {...register('tags')} placeholder="vip, recorrente, indicação" />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="cust-notes">Observações</Label>
              <textarea
                id="cust-notes"
                {...register('notes')}
                className="flex min-h-[80px] w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                placeholder="Observações gerais sobre o cliente..."
              />
            </div>
          </div>

          {formError && (
            <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">{formError}</div>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={submitting}>
              Cancelar
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              {editingCustomer ? 'Salvar' : 'Criar Cliente'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
