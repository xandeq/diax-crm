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
    <div className="rounded-2xl border border-zinc-800/60 bg-[#0a130f]/40 backdrop-blur-md p-1.5 mb-8 shadow-lg shadow-black/10 select-none">
      <div className="flex flex-wrap items-center justify-start gap-1">
        {navItems.map((item) => {
          const isActive = item.exact
            ? pathname === item.href
            : pathname.startsWith(item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-2 px-4 py-2.5 text-xs font-semibold rounded-xl transition-all duration-300 whitespace-nowrap border border-transparent",
                isActive
                  ? "text-[#00D4AA] bg-emerald-500/10 border-emerald-500/10 shadow-sm shadow-emerald-500/5 font-bold"
                  : "text-zinc-400 hover:text-zinc-200 hover:bg-zinc-900/30"
              )}
            >
              <item.icon className={cn("h-3.5 w-3.5", isActive ? "text-[#00D4AA]" : "text-zinc-500")} />
              {item.name}
            </Link>
          );
        })}
      </div>
    </div>
  );
}
