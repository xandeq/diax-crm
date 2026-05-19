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
import { createLead, Lead, updateLead } from '@/services/leads';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

const leadSchema = z.object({
  name: z.string().min(3, 'Nome deve ter no mínimo 3 caracteres'),
  email: z.string().email('Email inválido').or(z.literal('')),
  personType: z.number(),
  companyName: z.string().optional(),
  phone: z.string().optional(),
  whatsApp: z.string().optional(),
  website: z.string().optional(),
  notes: z.string().optional(),
  tags: z.string().optional(),
});

type LeadFormValues = z.infer<typeof leadSchema>;

interface LeadFormDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  editingLead: Lead | null;
}

export function LeadFormDialog({ open, onClose, onSuccess, editingLead }: LeadFormDialogProps) {
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<LeadFormValues>({
    resolver: zodResolver(leadSchema),
    defaultValues: { name: '', email: '', personType: 0, companyName: '', phone: '', whatsApp: '', website: '', notes: '', tags: '' },
  });

  useEffect(() => {
    if (!open) return;
    if (editingLead) {
      reset({
        name: editingLead.name,
        email: editingLead.email || '',
        personType: editingLead.personType,
        companyName: editingLead.companyName || '',
        phone: editingLead.phone || '',
        whatsApp: editingLead.whatsApp || '',
        website: editingLead.website || '',
        notes: editingLead.notes || '',
        tags: editingLead.tags || '',
      });
    } else {
      reset({ name: '', email: '', personType: 0, companyName: '', phone: '', whatsApp: '', website: '', notes: '', tags: '' });
    }
    setFormError(null);
  }, [open, editingLead, reset]);

  const onSubmit = async (data: LeadFormValues) => {
    setSubmitting(true);
    setFormError(null);
    try {
      if (editingLead) {
        await updateLead(editingLead.id, {
          name: data.name,
          email: data.email,
          personType: data.personType,
          companyName: data.companyName || undefined,
          phone: data.phone || undefined,
          whatsApp: data.whatsApp || undefined,
          website: data.website || undefined,
          notes: data.notes || undefined,
          tags: data.tags || undefined,
          source: editingLead.source,
        });
      } else {
        await createLead({
          name: data.name,
          email: data.email,
          personType: data.personType,
          companyName: data.companyName || undefined,
          phone: data.phone || undefined,
        });
      }
      onClose();
      onSuccess();
    } catch {
      setFormError('Erro ao salvar lead. Verifique os dados.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="sm:max-w-[580px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{editingLead ? 'Editar Lead' : 'Novo Lead'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
          <div className="space-y-3">
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">Dados Básicos</p>
            <div className="space-y-2">
              <Label htmlFor="lead-name">Nome Completo *</Label>
              <Input id="lead-name" {...register('name')} placeholder="Ex: João Silva" />
              {errors.name && <p className="text-xs text-red-500">{errors.name.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="lead-email">Email</Label>
                <Input id="lead-email" type="email" {...register('email')} placeholder="joao@empresa.com" />
                {errors.email && <p className="text-xs text-red-500">{errors.email.message}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="lead-companyName">Empresa</Label>
                <Input id="lead-companyName" {...register('companyName')} placeholder="Empresa LTDA" />
              </div>
            </div>
            <div className="space-y-2">
              <Label>Tipo Pessoa</Label>
              <div className="flex gap-3">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="radio" value={0} {...register('personType', { valueAsNumber: true })} className="accent-slate-900" />
                  <span className="text-sm">Física</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="radio" value={1} {...register('personType', { valueAsNumber: true })} className="accent-slate-900" />
                  <span className="text-sm">Jurídica</span>
                </label>
              </div>
            </div>
          </div>

          <div className="space-y-3 pt-2 border-t border-slate-100">
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">Contato</p>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="lead-phone">Telefone</Label>
                <Input id="lead-phone" {...register('phone')} placeholder="(11) 99999-9999" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lead-whatsApp">WhatsApp</Label>
                <Input id="lead-whatsApp" {...register('whatsApp')} placeholder="(11) 99999-9999" />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="lead-website">Website</Label>
              <Input id="lead-website" {...register('website')} placeholder="https://empresa.com.br" />
            </div>
          </div>

          {editingLead && (
            <div className="space-y-3 pt-2 border-t border-slate-100">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">Notas & Tags</p>
              <div className="space-y-2">
                <Label htmlFor="lead-notes">Notas</Label>
                <textarea
                  id="lead-notes"
                  {...register('notes')}
                  placeholder="Observações sobre este lead..."
                  rows={3}
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lead-tags">Tags</Label>
                <Input id="lead-tags" {...register('tags')} placeholder="tag1, tag2, tag3" />
                <p className="text-xs text-slate-400">Separe as tags por vírgula</p>
              </div>
            </div>
          )}

          {formError && (
            <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">{formError}</div>
          )}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancelar
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {editingLead ? 'Salvar Alterações' : 'Criar Lead'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
