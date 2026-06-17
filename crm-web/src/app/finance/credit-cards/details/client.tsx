'use client';

import { FinanceNav } from '@/components/finance/FinanceNav';
import { FinancialGrid } from '@/components/finance/FinancialGrid';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { formatDisplayDate } from '@/lib/date-utils';
import { formatCurrency, cn } from '@/lib/utils';
import { useCreditCardDetail, useCreditCardInvoices, useInvoiceTransactions } from '@/hooks/finance';
import { Transaction } from '@/services/finance';
import { ColumnDef } from '@tanstack/react-table';
import { ArrowLeft, CreditCard as CreditCardIcon, FileText, Calendar, Wallet, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { Label } from '@/components/ui/label';
import { useSearchParams } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';

export function CreditCardDetailsClient() {
    const searchParams = useSearchParams();
    const id = searchParams.get('id');

    const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null);

    const { data: card, isLoading: loadingCard } = useCreditCardDetail(id);
    const { data: invoices = [], isLoading: loadingInvoices } = useCreditCardInvoices(id);
    const { data: transactions, isLoading: loadingExpenses } = useInvoiceTransactions(selectedInvoiceId);

    useEffect(() => {
        if (invoices.length > 0 && !selectedInvoiceId) {
            setSelectedInvoiceId(invoices[0].id);
        }
    }, [invoices, selectedInvoiceId]);

    const selectedInvoice = useMemo(() =>
        invoices.find(i => i.id === selectedInvoiceId),
    [invoices, selectedInvoiceId]);

    const columns = useMemo<ColumnDef<Transaction>[]>(() => [
        {
            accessorKey: 'date',
            header: 'Data',
            cell: ({ row }) => <span className="font-medium text-zinc-400 text-xs tabular-nums">{formatDisplayDate(row.original.date)}</span>,
        },
        {
            accessorKey: 'description',
            header: 'Descrição',
            cell: ({ row }) => (
                <div className="flex flex-col py-0.5">
                    <span className="font-semibold text-zinc-100 text-sm leading-snug">{row.original.description}</span>
                    <span className="text-[10px] text-zinc-500 uppercase tracking-wider font-bold mt-0.5">{row.original.categoryName}</span>
                </div>
            ),
        },
        {
            accessorKey: 'amount',
            header: 'Valor',
            cell: ({ row }) => <span className="font-bold text-rose-400 text-sm tabular-nums">{formatCurrency(row.original.amount)}</span>,
        },
    ], []);

    if (loadingCard || loadingInvoices) {
        return (
            <div className="container mx-auto px-4 py-6 max-w-7xl">
                <FinanceNav />
                <div className="flex items-center justify-center min-h-[300px] gap-3">
                    <Loader2 className="h-6 w-6 animate-spin text-emerald-400" />
                    <span className="text-zinc-400 text-sm animate-pulse">Carregando detalhes do cartão...</span>
                </div>
            </div>
        );
    }
    
    if (!id) return <div className="container mx-auto px-4 py-6 max-w-7xl text-rose-400 font-semibold">ID do cartão não fornecido</div>;
    if (!card) return <div className="container mx-auto px-4 py-6 max-w-7xl text-rose-400 font-semibold">Cartão não encontrado</div>;

    return (
        <div className="container mx-auto px-4 py-6 max-w-7xl space-y-6">
            <FinanceNav />

            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
                <div className="flex items-center gap-3">
                    <Link href="/finance/credit-cards">
                        <Button variant="ghost" size="icon" className="rounded-full text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50">
                            <ArrowLeft className="h-5 w-5" />
                        </Button>
                    </Link>
                    <div>
                        <h1 className="text-2xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
                            <CreditCardIcon className="h-6 w-6 text-violet-400" />
                            {card.name}
                        </h1>
                        <p className="text-zinc-400 text-sm mt-0.5">
                            Final **** {card.lastFourDigits} • Limite Total: {formatCurrency(card.limit)}
                        </p>
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <motion.div 
                    initial={{ opacity: 0, x: -15 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ duration: 0.3 }}
                    className="space-y-6"
                >
                    <div className="p-6 rounded-2xl border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden">
                        <div className="absolute inset-0 bg-gradient-to-br from-violet-500/5 to-transparent pointer-events-none" />

                        <h2 className="text-sm font-bold tracking-wider uppercase text-zinc-400 mb-5 flex items-center gap-2 relative z-10">
                            <FileText className="h-4 w-4 text-violet-400" />
                            Fatura Mensal
                        </h2>

                        <div className="space-y-5 relative z-10">
                          <div className="space-y-1.5">
                            <Label className="text-xs font-medium text-zinc-300">Selecione o Mês</Label>
                            <Select value={selectedInvoiceId ?? ''} onValueChange={setSelectedInvoiceId}>
                              <SelectTrigger className="bg-zinc-950/80 border-zinc-800 text-zinc-100 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm">
                                <SelectValue placeholder="Selecione uma fatura" />
                              </SelectTrigger>
                              <SelectContent className="bg-zinc-900 border-zinc-800 text-zinc-100 rounded-xl">
                                {invoices.map(invoice => (
                                  <SelectItem key={invoice.id} value={invoice.id}>
                                    {new Date(invoice.referenceYear, invoice.referenceMonth - 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })}
                                    {' '}- {invoice.isPaid ? 'Paga' : 'Aberta'}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          </div>

                          {selectedInvoice && (
                            <div className="pt-4 border-t border-zinc-800/80 space-y-3.5">
                              <div className="flex justify-between items-center text-xs">
                                <span className="text-zinc-400 flex items-center gap-1.5">
                                  <Calendar className="h-3.5 w-3.5 text-zinc-500" />
                                  Fechamento
                                </span>
                                <span className="font-semibold text-zinc-200 tabular-nums">
                                  {new Date(selectedInvoice.closingDate).toLocaleDateString('pt-BR')}
                                </span>
                              </div>
                              <div className="flex justify-between items-center text-xs">
                                <span className="text-zinc-400 flex items-center gap-1.5">
                                  <Calendar className="h-3.5 w-3.5 text-zinc-500" />
                                  Vencimento
                                </span>
                                <span className="font-semibold text-zinc-200 tabular-nums">
                                  {new Date(selectedInvoice.dueDate).toLocaleDateString('pt-BR')}
                                </span>
                              </div>
                              <div className="flex justify-between items-center text-xs">
                                <span className="text-zinc-400">Status da Fatura</span>
                                <Badge 
                                  className={cn(
                                    "text-[10px] font-bold px-2 py-0.5 rounded-full border",
                                    selectedInvoice.isPaid 
                                      ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20" 
                                      : "bg-amber-500/10 text-amber-400 border-amber-500/20"
                                  )}
                                >
                                  {selectedInvoice.isPaid ? 'Paga ✓' : 'Aberta'}
                                </Badge>
                              </div>
                              <div className="flex justify-between items-center pt-3.5 border-t border-zinc-800/80">
                                <span className="text-sm font-bold text-zinc-100 flex items-center gap-1.5">
                                  <Wallet className="h-4 w-4 text-violet-400" />
                                  Total Fatura
                                </span>
                                <span className="text-xl font-bold text-violet-400 tabular-nums">
                                  {formatCurrency(selectedInvoice.totalAmount)}
                                </span>
                              </div>
                            </div>
                          )}

                          {!selectedInvoice && (
                            <div className="text-center py-8 text-zinc-500 text-xs">
                              Nenhuma fatura selecionada
                            </div>
                          )}
                        </div>
                    </div>
                </motion.div>

                <motion.div 
                    initial={{ opacity: 0, x: 15 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ duration: 0.3 }}
                    className="lg:col-span-2 space-y-6"
                >
                    <div className="rounded-2xl overflow-hidden border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md">
                        <div className="p-5 flex justify-between items-center" style={{ borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
                            <h2 className="text-sm font-bold tracking-wider uppercase text-zinc-300">Lançamentos da Fatura</h2>
                            <span className="text-xs text-zinc-500 font-bold bg-zinc-950/60 px-2 py-0.5 rounded-full border border-zinc-800/40">
                              {transactions?.totalCount || 0} compras
                            </span>
                        </div>

                        <FinancialGrid
                            columns={columns}
                            data={transactions?.items || []}
                            pageCount={transactions?.totalPages || 0}
                            page={1}
                            pageSize={100}
                            onPageChange={() => {}}
                            onPageSizeChange={() => {}}
                            loading={loadingExpenses}
                        />
                    </div>
                </motion.div>
            </div>
        </div>
    );
}
