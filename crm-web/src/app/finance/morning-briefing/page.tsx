import MorningBriefingClient from '@/components/finance/MorningBriefingClient';
import type { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Morning Briefing | DIAX CRM',
    description: 'Resumo financeiro diário',
};

export default function MorningBriefingPage() {
    return (
        <div className="container max-w-2xl mx-auto py-8 px-4">
            <MorningBriefingClient />
        </div>
    );
}
