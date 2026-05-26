'use client';

import { cn } from '@/lib/utils';
import {
    ArrowRightLeft,
    Calendar,
    CreditCard,
    FileInput,
    Landmark,
    LayoutDashboard,
    List,
    Wallet,
} from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

const navItems = [
  {
    name: 'Dashboard',
    href: '/finance',
    icon: LayoutDashboard,
    exact: true
  },
  {
    name: 'Planejador',
    href: '/finance/planner',
    icon: Calendar
  },
  {
    name: 'Planilha Financeira',
    href: '/finance/personal-control',
    icon: Wallet
  },
  {
    name: 'Transações',
    href: '/finance/transactions',
    icon: List
  },
  {
    name: 'Cartões',
    href: '/finance/credit-cards',
    icon: CreditCard
  },
  {
    name: 'Contas',
    href: '/finance/accounts',
    icon: Landmark
  },
  {
    name: 'Importações',
    href: '/finance/imports',
    icon: FileInput
  },
  {
    name: 'Transferências',
    href: '/finance/transfers',
    icon: ArrowRightLeft
  }
];

export function FinanceNav() {
  const pathname = usePathname();

  return (
    <div className="border-b mb-6" style={{ borderColor: 'rgba(255,255,255,0.07)', background: 'rgba(11,21,16,0.8)' }}>
      <div className="flex flex-wrap items-center justify-center gap-1.5 p-2">
        {navItems.map((item) => {
          const isActive = item.exact
            ? pathname === item.href
            : pathname.startsWith(item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-2 px-3.5 py-2 text-sm font-medium rounded-lg transition-colors whitespace-nowrap",
                isActive
                  ? "text-emerald-400"
                  : "text-zinc-400 hover:text-zinc-200"
              )}
              style={isActive ? { background: 'rgba(16,185,129,0.12)' } : undefined}
            >
              <item.icon className={cn("h-4 w-4", isActive ? "text-emerald-400" : "text-zinc-600")} />
              {item.name}
            </Link>
          );
        })}
      </div>
    </div>
  );
}
