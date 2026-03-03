import { AgendaClient } from '@/components/agenda/AgendaClient';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Agenda | DIAX CRM',
  description: 'Gerencie seus compromissos e obrigações',
};

export default function AgendaPage() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <AgendaClient />
    </div>
  );
}
