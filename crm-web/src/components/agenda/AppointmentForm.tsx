'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Appointment, AppointmentLabel, AppointmentType, CreateAppointmentDto, RecurringAppointmentDto, UpdateAppointmentDto } from '@/types/agenda';
import { format, parseISO } from 'date-fns';
import { useState } from 'react';

const DAYS_OF_WEEK = [
    { label: 'Seg', value: 1 },
    { label: 'Ter', value: 2 },
    { label: 'Qua', value: 3 },
    { label: 'Qui', value: 4 },
    { label: 'Sex', value: 5 },
    { label: 'Sáb', value: 6 },
    { label: 'Dom', value: 0 },
];

interface AppointmentFormProps {
    initialData?: Appointment;
    preselectedDate?: Date;
    labels: AppointmentLabel[];
    onSubmit: (data: CreateAppointmentDto | UpdateAppointmentDto | RecurringAppointmentDto, isRecurring?: boolean) => Promise<void>;
    onCancel: () => void;
    isLoading?: boolean;
}

export function AppointmentForm({ initialData, preselectedDate, labels, onSubmit, onCancel, isLoading }: AppointmentFormProps) {
    const [title, setTitle] = useState(initialData?.title || '');
    const [description, setDescription] = useState(initialData?.description || '');
    const [type, setType] = useState<AppointmentType>(initialData?.type || 'Other');
    const [labelId, setLabelId] = useState<string | undefined>(initialData?.labelId);
    const [durationMinutes, setDurationMinutes] = useState(initialData?.durationMinutes ?? 60);

    // Recurrence
    const [isRecurring, setIsRecurring] = useState(false);
    const [recurDays, setRecurDays] = useState<number[]>([1, 2, 3, 4, 5]); // Seg-Sex default
    const [recurTime, setRecurTime] = useState('09:00');
    const [recurStart, setRecurStart] = useState(format(preselectedDate || new Date(), 'yyyy-MM-dd'));
    const [recurEnd, setRecurEnd] = useState('');

    // Datetime for single appointment
    const defaultDateStr = initialData
        ? format(parseISO(initialData.date), "yyyy-MM-dd'T'HH:mm")
        : preselectedDate
            ? format(preselectedDate, "yyyy-MM-dd'T'09:00")
            : format(new Date(), "yyyy-MM-dd'T'09:00");

    const [date, setDate] = useState(defaultDateStr);

    const toggleDay = (day: number) => {
        setRecurDays(prev =>
            prev.includes(day) ? prev.filter(d => d !== day) : [...prev, day]
        );
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!title) return;

        if (isRecurring) {
            if (!recurTime || !recurStart || !recurEnd || recurDays.length === 0) return;
            const dto: RecurringAppointmentDto = {
                title,
                description: description || undefined,
                type,
                labelId: labelId || undefined,
                durationMinutes,
                timeHHmm: recurTime,
                daysOfWeek: recurDays,
                startDate: recurStart,
                endDate: recurEnd,
            };
            await onSubmit(dto, true);
            return;
        }

        if (!date) return;
        const dateObj = new Date(date);

        if (initialData) {
            const dto: UpdateAppointmentDto = {
                title,
                description: description || undefined,
                type,
                date: dateObj.toISOString(),
                durationMinutes,
                labelId: labelId || undefined,
            };
            await onSubmit(dto, false);
        } else {
            const dto: CreateAppointmentDto = {
                title,
                description: description || undefined,
                type,
                date: dateObj.toISOString(),
                durationMinutes,
                labelId: labelId || undefined,
            };
            await onSubmit(dto, false);
        }
    };

    const selectedLabel = labels.find(l => l.id === labelId);

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
                <Label htmlFor="title">Título *</Label>
                <Input
                    id="title"
                    value={title}
                    onChange={e => setTitle(e.target.value)}
                    required
                    placeholder="Ex: Daily KPIT, Almoço..."
                />
            </div>

            {/* Label selector */}
            {labels.length > 0 && (
                <div className="space-y-2">
                    <Label>Label</Label>
                    <div className="flex flex-wrap gap-2">
                        <button
                            type="button"
                            onClick={() => setLabelId(undefined)}
                            className={`px-3 py-1.5 rounded-full text-xs font-medium border transition-all ${!labelId ? 'border-slate-600 bg-slate-100' : 'border-slate-200 bg-white text-slate-500 hover:bg-slate-50'}`}
                        >
                            Nenhum
                        </button>
                        {labels.map(l => (
                            <button
                                key={l.id}
                                type="button"
                                onClick={() => setLabelId(l.id === labelId ? undefined : l.id)}
                                className={`px-3 py-1.5 rounded-full text-xs font-medium border-2 transition-all flex items-center gap-1.5 ${l.id === labelId ? 'border-current' : 'border-transparent'}`}
                                style={{
                                    backgroundColor: l.id === labelId ? l.color + '20' : l.color + '15',
                                    color: l.color,
                                    borderColor: l.id === labelId ? l.color : 'transparent',
                                }}
                            >
                                <span className="w-2 h-2 rounded-full" style={{ backgroundColor: l.color }} />
                                {l.name}
                            </button>
                        ))}
                    </div>
                </div>
            )}

            <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                    <Label>Tipo</Label>
                    <Select value={type} onValueChange={(val: AppointmentType) => setType(val)}>
                        <SelectTrigger>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="Medical">Médico / Saúde</SelectItem>
                            <SelectItem value="HomeService">Serviço em Casa</SelectItem>
                            <SelectItem value="Payment">Pagamento / Conta</SelectItem>
                            <SelectItem value="Other">Outro</SelectItem>
                        </SelectContent>
                    </Select>
                </div>
                <div className="space-y-2">
                    <Label htmlFor="duration">Duração (min)</Label>
                    <Input
                        id="duration"
                        type="number"
                        min={15}
                        step={15}
                        value={durationMinutes}
                        onChange={e => setDurationMinutes(Number(e.target.value))}
                    />
                </div>
            </div>

            {/* Recurrence toggle (only for new appointments) */}
            {!initialData && (
                <div className="space-y-3">
                    <label className="flex items-center gap-2 cursor-pointer">
                        <input
                            type="checkbox"
                            checked={isRecurring}
                            onChange={e => setIsRecurring(e.target.checked)}
                            className="w-4 h-4"
                        />
                        <span className="text-sm font-medium text-slate-700">Compromisso recorrente</span>
                    </label>

                    {isRecurring ? (
                        <div className="bg-blue-50 border border-blue-100 rounded-lg p-4 space-y-3">
                            <div className="space-y-2">
                                <Label>Dias da semana</Label>
                                <div className="flex gap-1.5 flex-wrap">
                                    {DAYS_OF_WEEK.map(d => (
                                        <button
                                            key={d.value}
                                            type="button"
                                            onClick={() => toggleDay(d.value)}
                                            className={`px-2.5 py-1 rounded-md text-xs font-medium transition-all border ${
                                                recurDays.includes(d.value)
                                                    ? 'bg-blue-600 text-white border-blue-600'
                                                    : 'bg-white text-slate-600 border-slate-300 hover:border-blue-300'
                                            }`}
                                        >
                                            {d.label}
                                        </button>
                                    ))}
                                </div>
                            </div>
                            <div className="grid grid-cols-3 gap-3">
                                <div className="space-y-1">
                                    <Label htmlFor="recurTime" className="text-xs">Horário</Label>
                                    <Input
                                        id="recurTime"
                                        type="time"
                                        value={recurTime}
                                        onChange={e => setRecurTime(e.target.value)}
                                    />
                                </div>
                                <div className="space-y-1">
                                    <Label htmlFor="recurStart" className="text-xs">De</Label>
                                    <Input
                                        id="recurStart"
                                        type="date"
                                        value={recurStart}
                                        onChange={e => setRecurStart(e.target.value)}
                                    />
                                </div>
                                <div className="space-y-1">
                                    <Label htmlFor="recurEnd" className="text-xs">Até</Label>
                                    <Input
                                        id="recurEnd"
                                        type="date"
                                        value={recurEnd}
                                        onChange={e => setRecurEnd(e.target.value)}
                                    />
                                </div>
                            </div>
                        </div>
                    ) : (
                        <div className="space-y-2">
                            <Label htmlFor="date">Data e Hora *</Label>
                            <Input
                                id="date"
                                type="datetime-local"
                                value={date}
                                onChange={e => setDate(e.target.value)}
                                required
                            />
                        </div>
                    )}
                </div>
            )}

            {/* Date for editing existing */}
            {initialData && (
                <div className="space-y-2">
                    <Label htmlFor="date">Data e Hora *</Label>
                    <Input
                        id="date"
                        type="datetime-local"
                        value={date}
                        onChange={e => setDate(e.target.value)}
                        required
                    />
                </div>
            )}

            <div className="space-y-2">
                <Label htmlFor="description">Notas</Label>
                <Input
                    id="description"
                    value={description}
                    onChange={e => setDescription(e.target.value)}
                    placeholder="Detalhes adicionais..."
                />
            </div>

            <div className="flex justify-end gap-2 pt-2">
                <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>
                    Cancelar
                </Button>
                <Button
                    type="submit"
                    disabled={isLoading || !title || (!isRecurring && !date) || (isRecurring && (!recurEnd || recurDays.length === 0))}
                >
                    {isLoading
                        ? 'Salvando...'
                        : isRecurring
                            ? 'Criar Série'
                            : 'Salvar'}
                </Button>
            </div>
        </form>
    );
}
