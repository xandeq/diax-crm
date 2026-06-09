import DailyBriefingsClient from '@/components/daily-briefings/DailyBriefingsClient';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Daily Briefings | DIAX CRM',
  description: 'Briefings de IA e desenvolvimento do dia corrente',
};

export default function DailyBriefingsPage() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
      <DailyBriefingsClient />
    </div>
  );
}
