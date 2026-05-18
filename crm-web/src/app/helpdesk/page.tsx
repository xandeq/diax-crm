import { TicketsClient } from '@/components/helpdesk/TicketsClient';
import type { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Helpdesk | DIAX CRM',
    description: 'Gestão de tickets de suporte e atendimento ao cliente',
};

export default function HelpdeskPage() {
    return <TicketsClient />;
}
