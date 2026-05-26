'use client';

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
import { formatCurrency } from '@/lib/utils';
import { useCreditCardDetail, useCreditCardInvoices, useInvoiceTransactions } from '@/hooks/finance';
import { Transaction } from '@/services/finance';
import { ColumnDef } from '@tanstack/react-table';
import { ArrowLeft, CreditCard as CreditCardIcon, FileText } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';

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
            cell: ({ row }) => <span className="font-medium text-gray-600">{formatDisplayDate(row.original.date)}</span>,
        },
        {
            accessorKey: 'description',
            header: 'Descrição',
            cell: ({ row }) => (
                <div className="flex flex-col">
                    <span className="font-semibold text-gray-900">{row.original.description}</span>
                    <span className="text-xs text-gray-500 uppercase tracking-wider font-medium">{row.original.categoryName}</span>
                </div>
            ),
        },
        {
            accessorKey: 'amount',
            header: 'Valor',
            cell: ({ row }) => <span className="font-bold text-gray-900">{formatCurrency(row.original.amount)}</span>,
        },
    ], []);

    if (loadingCard || loadingInvoices) return <div className="p-8 flex justify-center"><div className="animate-spin h-8 w-8 border-4 border-blue-600 rounded-full border-t-transparent"></div></div>;
    if (!id) return <div className="p-8 text-red-600">ID do cartão não fornecido</div>;
    if (!card) return <div className="p-8 text-red-600">Cartão não encontrado</div>;

    return (
        <div className="p-8 max-w-[1600px] mx-auto space-y-8">
            <div className="flex items-center gap-4 mb-6">
                <Link href="/finance/credit-cards">
                    <Button variant="ghost" size="icon" className="rounded-full hover:bg-gray-100">
                        <ArrowLeft className="h-6 w-6 text-gray-500" />
                    </Button>
                </Link>
                <div>
                    <h1 className="text-2xl font-bold flex items-center gap-2">
                        <CreditCardIcon className="h-6 w-6 text-blue-600" />
                        {card.name}
                    </h1>
                    <p className="text-gray-500 text-sm">Final **** {card.lastFourDigits} • Limite: {formatCurrency(card.limit)}</p>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="space-y-6">
                    <div className="p-6 rounded-xl" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
                        <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                            <FileText className="h-5 w-5 text-gray-500" />
                            Fatura
                        </h2>

                        <div className="space-y-4">
                            <div>
                                <label className="text-sm font-medium text-gray-700 mb-1 block">Selecione o Mês</label>
                                <Select value={selectedInvoiceId ?? ''} onValueChange={setSelectedInvoiceId}>
                                    <SelectTrigger>
                                        <SelectValue placeholder="Selecione uma fatura" />
                                    </SelectTrigger>
                                    <SelectContent>
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
                                <div className="pt-4 border-t border-gray-100 space-y-3">
                                    <div className="flex justify-between items-center">
                                        <span className="text-gray-600">Vencimento</span>
                                        <span className="font-medium">{new Date(selectedInvoice.dueDate).toLocaleDateString('pt-BR')}</span>
                                    </div>
                                    <div className="flex justify-between items-center">
                                        <span className="text-gray-600">Fechamento</span>
                                        <span className="font-medium">{new Date(selectedInvoice.closingDate).toLocaleDateString('pt-BR')}</span>
                                    </div>
                                    <div className="flex justify-between items-center">
                                        <span className="text-gray-600">Status</span>
                                        <Badge variant={selectedInvoice.isPaid ? 'secondary' : 'default'} className={selectedInvoice.isPaid ? "bg-green-100 text-green-700" : "bg-blue-100 text-blue-700"}>
                                            {selectedInvoice.isPaid ? 'Paga' : 'Aberta'}
                                        </Badge>
                                    </div>
                                    <div className="flex justify-between items-center pt-2 border-t border-gray-100">
                                        <span className="text-lg font-semibold text-gray-900">Total</span>
                                        <span className="text-xl font-bold text-blue-600">{formatCurrency(selectedInvoice.totalAmount)}</span>
                                    </div>
                                </div>
                            )}

                            {!selectedInvoice && (
                                <div className="text-center py-8 text-gray-500">
                                    Nenhuma fatura selecionada
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                <div className="lg:col-span-2 space-y-6">
                    <div className="rounded-xl overflow-hidden" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
                        <div className="p-6 flex justify-between items-center" style={{ borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
                            <h2 className="text-lg font-semibold">Lançamentos</h2>
                            <span className="text-sm text-gray-500">{transactions?.totalCount || 0} lançamentos</span>
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
                </div>
            </div>
        </div>
    );
}
