import { FinanceNav } from '@/components/finance/FinanceNav';

export default function FinanceLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-gray-50/50">
      <FinanceNav />
      <main>
        {children}
      </main>
    </div>
  );
}
