'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Appointment, AppointmentType, CreateAppointmentDto, UpdateAppointmentDto } from '@/types/agenda';
import { format, parseISO } from 'date-fns';
import { useState } from 'react';

interface AppointmentFormProps {
  initialData?: Appointment;
  preselectedDate?: Date;
  onSubmit: (data: any) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
}

export function AppointmentForm({ initialData, preselectedDate, onSubmit, onCancel, isLoading }: AppointmentFormProps) {
  const [title, setTitle] = useState(initialData?.title || '');
  const [description, setDescription] = useState(initialData?.description || '');
  const [type, setType] = useState<AppointmentType>(initialData?.type || 'Medical');

  // Format date/time for the HTML inputs
  const defaultDateStr = initialData
    ? format(parseISO(initialData.date), "yyyy-MM-dd'T'HH:mm")
    : preselectedDate
      ? format(preselectedDate, "yyyy-MM-dd'T'12:00") // default to noon if only a date is given
      : format(new Date(), "yyyy-MM-dd'T'12:00");

  const [date, setDate] = useState(defaultDateStr);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title || !date) return;

    // Convert local datetime-local string to ISO 8601 string for the API
    const dateObj = new Date(date);

    if (initialData) {
      const dto: UpdateAppointmentDto = {
        title,
        description,
        type,
        date: dateObj.toISOString(),
      };
      await onSubmit(dto);
    } else {
      const dto: CreateAppointmentDto = {
        title,
        description,
        type,
        date: dateObj.toISOString(),
      };
      await onSubmit(dto);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="title">Título do Compromisso *</Label>
        <Input
          id="title"
          value={title}
          onChange={e => setTitle(e.target.value)}
          required
          placeholder="Ex: Consulta Dr. Silva"
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
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

        <div className="space-y-2">
          <Label htmlFor="type">Tipo *</Label>
          <Select value={type} onValueChange={(val: AppointmentType) => setType(val)}>
            <SelectTrigger id="type">
              <SelectValue placeholder="Selecione o tipo" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Medical">Médico / Saúde</SelectItem>
              <SelectItem value="HomeService">Serviço em Casa</SelectItem>
              <SelectItem value="Payment">Pagamento / Conta</SelectItem>
              <SelectItem value="Other">Outro</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Descrição / Notas</Label>
        <Input
          id="description"
          value={description}
          onChange={e => setDescription(e.target.value)}
          placeholder="Detalhes adicionais..."
        />
      </div>

      <div className="flex justify-end gap-2 pt-4">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isLoading || !title || !date}>
          {isLoading ? 'Salvando...' : 'Salvar Compromisso'}
        </Button>
      </div>
    </form>
  );
}
