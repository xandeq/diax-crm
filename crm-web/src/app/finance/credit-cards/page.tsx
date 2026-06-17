'use client';

import { FinanceNav } from '@/components/finance/FinanceNav';
import { useCreditCards } from '@/hooks/finance';
import { CardBrand, CardKind } from '@/services/finance';
import Link from 'next/link';
import { motion } from 'framer-motion';
import { 
  CreditCard as CreditCardIcon, 
  Plus, 
  CalendarDays, 
  ChevronRight, 
  Loader2,
  XCircle,
  ShieldCheck,
  Zap
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { EmptyState } from '@/components/dashboard/EmptyState';

const brandLabels: Record<CardBrand, string> = {
  [CardBrand.Unknown]: 'Desconhecido',
  [CardBrand.Visa]: 'Visa',
  [CardBrand.Mastercard]: 'Mastercard',
  [CardBrand.Elo]: 'Elo',
  [CardBrand.Amex]: 'American Express',
  [CardBrand.Hipercard]: 'Hipercard',
  [CardBrand.Diners]: 'Diners Club',
  [CardBrand.Discover]: 'Discover',
  [CardBrand.JCB]: 'JCB',
};

const brandGradients: Record<CardBrand, string> = {
  [CardBrand.Unknown]: 'from-zinc-900 via-zinc-800 to-zinc-950',
  [CardBrand.Visa]: 'from-blue-900 via-slate-900 to-blue-950',
  [CardBrand.Mastercard]: 'from-orange-950 via-zinc-900 to-red-950',
  [CardBrand.Elo]: 'from-emerald-950 via-zinc-900 to-indigo-950',
  [CardBrand.Amex]: 'from-cyan-900 via-slate-900 to-blue-900',
  [CardBrand.Hipercard]: 'from-red-900 via-zinc-900 to-amber-950',
  [CardBrand.Diners]: 'from-zinc-800 via-slate-900 to-zinc-900',
  [CardBrand.Discover]: 'from-orange-900 via-stone-900 to-amber-900',
  [CardBrand.JCB]: 'from-red-950 via-slate-900 to-blue-950',
};

export default function CreditCardsPage() {
  const { data: creditCards = [], isLoading, isError } = useCreditCards();

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-6 max-w-7xl">
        <FinanceNav />
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-emerald-400" />
          <p className="text-zinc-400 text-sm animate-pulse">Carregando cartões de crédito...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-6 max-w-7xl">
      <FinanceNav />

      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
            <CreditCardIcon className="h-6 w-6 text-violet-400" />
            Cartões de Crédito
          </h1>
          <p className="text-zinc-400 text-sm mt-1">Gerencie seus limites, vencimentos e faturas</p>
        </div>
        <Link href="/finance/credit-cards/new" className="inline-flex items-center gap-2 px-4 py-2 rounded-xl font-semibold text-sm transition-all duration-300 bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894]">
          <Plus className="h-4 w-4" />
          Novo Cartão
        </Link>
      </div>

      {isError && (
        <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-4 rounded-xl mb-6 flex items-center gap-2 text-sm font-semibold">
          <XCircle className="h-4 w-4 shrink-0" />
          Erro ao carregar cartões de crédito. Por favor, tente novamente.
        </div>
      )}

      {creditCards.length === 0 ? (
        <EmptyState
          title="Nenhum cartão cadastrado"
          description="Cadastre seus cartões de crédito físicos ou virtuais para planejar faturas e lançar compras."
          icon={CreditCardIcon}
          actionLabel="Novo Cartão"
          actionHref="/finance/credit-cards/new"
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {creditCards.map((card, index) => {
            const cardGradient = brandGradients[card.brand] ?? brandGradients[CardBrand.Unknown];
            return (
              <motion.div
                key={card.id}
                initial={{ opacity: 0, scale: 0.95, y: 15 }}
                animate={{ opacity: 1, scale: 1, y: 0 }}
                transition={{ duration: 0.3, delay: index * 0.05 }}
                whileHover={{ y: -6, transition: { duration: 0.2 } }}
                className={cn(
                  "relative rounded-2xl p-6 shadow-xl transition-all duration-300 select-none",
                  "border border-zinc-800/80 bg-gradient-to-br",
                  cardGradient,
                  !card.isActive && "opacity-50"
                )}
              >
                {/* Visual card glow */}
                <div className="absolute inset-0 bg-white/5 opacity-0 hover:opacity-100 transition-opacity duration-300 rounded-2xl pointer-events-none" />

                {/* Top card bar: Chip and Type */}
                <div className="flex justify-between items-start mb-6">
                  {/* Real look chip */}
                  <div className="w-10 h-8 rounded-md bg-gradient-to-br from-yellow-300 via-amber-400 to-yellow-600 border border-yellow-500/30 relative flex items-center justify-center overflow-hidden shadow-inner">
                    <div className="absolute w-8 h-6 border-r border-b border-black/20" />
                    <div className="absolute w-6 h-4 border-l border-t border-black/20" />
                  </div>
                  
                  {/* Brand name */}
                  <span className="text-xs uppercase font-extrabold tracking-widest text-zinc-300/80 bg-white/10 px-2 py-0.5 rounded-full border border-white/5 backdrop-blur-sm">
                    {brandLabels[card.brand]}
                  </span>
                </div>

                {/* Card Number */}
                <div className="mb-4">
                  <span className="text-lg font-mono tracking-[0.2em] text-zinc-100 block drop-shadow-md">
                    ••••  ••••  ••••  {card.lastFourDigits}
                  </span>
                </div>

                {/* Card Holder Name */}
                <div className="mb-6">
                  <span className="text-xs text-zinc-400 uppercase tracking-wider block mb-0.5">Nome do Cartão</span>
                  <span className="font-semibold text-zinc-100 text-sm truncate block">{card.name}</span>
                </div>

                {/* Card Info Footer */}
                <div className="grid grid-cols-3 gap-2 pt-4 border-t border-white/10 text-xs">
                  <div>
                    <span className="text-[10px] text-zinc-400 uppercase tracking-wider block mb-0.5">Vence</span>
                    <span className="font-semibold text-zinc-200 block">Dia {card.dueDay}</span>
                  </div>
                  <div>
                    <span className="text-[10px] text-zinc-400 uppercase tracking-wider block mb-0.5">Fecha</span>
                    <span className="font-semibold text-zinc-200 block">Dia {card.closingDay}</span>
                  </div>
                  <div className="text-right">
                    <span className="text-[10px] text-zinc-400 uppercase tracking-wider block mb-0.5">Limite</span>
                    <span className="font-bold text-[#00D4AA] block">
                      {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(card.limit)}
                    </span>
                  </div>
                </div>

                {/* Details Button / Active Indicator */}
                <div className="flex items-center justify-between mt-5 pt-3 border-t border-white/5">
                  <div className="flex items-center gap-1.5 text-[10px] uppercase font-bold tracking-wider text-zinc-400">
                    <span className={cn(
                      "h-1.5 w-1.5 rounded-full",
                      card.isActive ? "bg-emerald-400 animate-pulse" : "bg-zinc-500"
                    )} />
                    {card.isActive ? 'Ativo' : 'Inativo'}
                    {card.cardKind === CardKind.Virtual && (
                      <span className="ml-1 px-1.5 py-0.5 rounded bg-blue-500/10 text-blue-400 text-[8px] font-extrabold border border-blue-500/20 flex items-center gap-0.5">
                        <Zap className="h-2 w-2" /> VIRTUAL
                      </span>
                    )}
                  </div>
                  
                  <Link 
                    href={`/finance/credit-cards/details?id=${card.id}`}
                    className="inline-flex items-center gap-1 text-xs font-semibold text-violet-400 hover:text-violet-300 transition-colors group"
                  >
                    Ver Faturas
                    <ChevronRight className="h-3.5 w-3.5 group-hover:translate-x-0.5 transition-transform" />
                  </Link>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
    </div>
  );
}
