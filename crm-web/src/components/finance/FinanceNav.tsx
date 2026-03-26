'use client';

import { cn } from '@/lib/utils';
import {
    ArrowDownCircle,
    ArrowRightLeft,
    ArrowUpCircle,
    Calendar,
    CreditCard,
    FileInput,
    Landmark,
    LayoutDashboard,
    List,
    Wallet,
    Tags
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
    name: 'Receitas',
    href: '/finance/incomes',
    icon: ArrowUpCircle
  },
  {
    name: 'Despesas',
    href: '/finance/expenses',
    icon: ArrowDownCircle
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
    name: 'Categorias',
    href: '/finance/categories',
    icon: Tags
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
    <div className="border-b border-gray-100 bg-white mb-6">
      <div className="flex flex-wrap items-center justify-center gap-2 p-2">
        {navItems.map((item) => {
          const isActive = item.exact
            ? pathname === item.href
            : pathname.startsWith(item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md transition-colors whitespace-nowrap",
                isActive
                  ? "bg-blue-50 text-blue-700"
                  : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
              )}
            >
              <item.icon className={cn("h-4 w-4", isActive ? "text-blue-600" : "text-gray-400")} />
              {item.name}
            </Link>
          );
        })}
      </div>
    </div>
  );
}
