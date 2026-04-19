'use client';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { appointmentLabelsService } from '@/services/appointmentLabels';
import { AppointmentLabel, CreateAppointmentLabelDto } from '@/types/agenda';
import { Loader2, Pencil, Plus, Trash2, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

const PRESET_COLORS = [
    '#3B82F6', '#10B981', '#8B5CF6', '#EC4899', '#F59E0B',
    '#EF4444', '#06B6D4', '#84CC16', '#F97316', '#64748b',
    '#D946EF', '#14B8A6'
];

interface LabelManagerModalProps {
    isOpen: boolean;
    onOpenChange: (open: boolean) => void;
    onLabelsChanged: () => void;
}

export function LabelManagerModal({ isOpen, onOpenChange, onLabelsChanged }: LabelManagerModalProps) {
    const [labels, setLabels] = useState<AppointmentLabel[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState<CreateAppointmentLabelDto>({ name: '', color: '#3B82F6', order: 0 });
    const [isSaving, setIsSaving] = useState(false);

    const fetchLabels = async () => {
        setIsLoading(true);
        try {
            const data = await appointmentLabelsService.getAll();
            setLabels(data);
        } catch {
            toast.error('Erro ao carregar labels.');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        if (isOpen) fetchLabels();
    }, [isOpen]);

    const resetForm = () => {
        setForm({ name: '', color: '#3B82F6', order: labels.length });
        setEditingId(null);
    };

    const handleSave = async () => {
        if (!form.name.trim()) return;
        setIsSaving(true);
        try {
            if (editingId) {
                await appointmentLabelsService.update(editingId, form);
                toast.success('Label atualizado!');
            } else {
                await appointmentLabelsService.create({ ...form, order: labels.length });
                toast.success('Label criado!');
            }
            await fetchLabels();
            onLabelsChanged();
            resetForm();
        } catch {
            toast.error('Erro ao salvar label.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleEdit = (label: AppointmentLabel) => {
        setEditingId(label.id);
        setForm({ name: label.name, color: label.color, order: label.order });
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Excluir este label? Os compromissos perderão a associação.')) return;
        try {
            await appointmentLabelsService.delete(id);
            toast.success('Label excluído.');
            await fetchLabels();
            onLabelsChanged();
        } catch {
            toast.error('Erro ao excluir label.');
        }
    };

    return (
        <Dialog open={isOpen} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[480px]">
                <DialogHeader>
                    <DialogTitle>Gerenciar Labels</DialogTitle>
                </DialogHeader>

                <div className="space-y-4 pt-2">
                    {/* Form */}
                    <div className="bg-slate-50 border border-slate-200 rounded-lg p-4 space-y-3">
                        <p className="text-sm font-medium text-slate-700">
                            {editingId ? 'Editar Label' : 'Novo Label'}
                        </p>
                        <div className="flex gap-2">
                            <Input
                                value={form.name}
                                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                                placeholder="Nome do label (ex: KPIT, Pessoal...)"
                                className="flex-1"
                            />
                            <div
                                className="w-10 h-10 rounded-md border-2 border-slate-200 cursor-pointer flex-shrink-0"
                                style={{ backgroundColor: form.color }}
                                title="Cor atual"
                            />
                        </div>
                        {/* Color picker */}
                        <div className="flex flex-wrap gap-2">
                            {PRESET_COLORS.map(c => (
                                <button
                                    key={c}
                                    type="button"
                                    onClick={() => setForm(f => ({ ...f, color: c }))}
                                    className={`w-7 h-7 rounded-full border-2 transition-transform hover:scale-110 ${form.color === c ? 'border-slate-800 scale-110' : 'border-transparent'}`}
                                    style={{ backgroundColor: c }}
                                />
                            ))}
                            <div className="flex items-center gap-1">
                                <Label htmlFor="customColor" className="text-xs text-slate-500">Hex:</Label>
                                <Input
                                    id="customColor"
                                    value={form.color}
                                    onChange={e => setForm(f => ({ ...f, color: e.target.value }))}
                                    className="w-24 h-7 text-xs"
                                    placeholder="#hex"
                                />
                            </div>
                        </div>
                        <div className="flex justify-end gap-2">
                            {editingId && (
                                <Button variant="ghost" size="sm" onClick={resetForm}>
                                    <X className="w-4 h-4 mr-1" /> Cancelar
                                </Button>
                            )}
                            <Button
                                size="sm"
                                onClick={handleSave}
                                disabled={isSaving || !form.name.trim()}
                                className="bg-blue-600 hover:bg-blue-700"
                            >
                                {isSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4 mr-1" />}
                                {editingId ? 'Salvar' : 'Criar Label'}
                            </Button>
                        </div>
                    </div>

                    {/* List */}
                    <div className="space-y-2 max-h-[280px] overflow-y-auto">
                        {isLoading ? (
                            <div className="flex justify-center py-4">
                                <Loader2 className="w-5 h-5 animate-spin text-slate-400" />
                            </div>
                        ) : labels.length === 0 ? (
                            <p className="text-sm text-slate-500 text-center py-4">Nenhum label criado ainda.</p>
                        ) : (
                            labels.map(label => (
                                <div key={label.id} className="flex items-center justify-between p-3 bg-white border border-slate-200 rounded-lg">
                                    <div className="flex items-center gap-3">
                                        <div className="w-4 h-4 rounded-full flex-shrink-0" style={{ backgroundColor: label.color }} />
                                        <span className="text-sm font-medium text-slate-800">{label.name}</span>
                                    </div>
                                    <div className="flex gap-1">
                                        <Button variant="ghost" size="sm" className="h-7 w-7 p-0" onClick={() => handleEdit(label)}>
                                            <Pencil className="w-3.5 h-3.5 text-slate-500" />
                                        </Button>
                                        <Button variant="ghost" size="sm" className="h-7 w-7 p-0 text-red-500 hover:text-red-700" onClick={() => handleDelete(label.id)}>
                                            <Trash2 className="w-3.5 h-3.5" />
                                        </Button>
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </div>
            </DialogContent>
        </Dialog>
    );
}
