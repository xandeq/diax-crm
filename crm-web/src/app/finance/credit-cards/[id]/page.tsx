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
import { CreditCard, CreditCardInvoice, Expense, financeService, PagedResponse } from '@/services/finance';
import { ColumnDef } from '@tanstack/react-table';
import { ArrowLeft, CreditCard as CreditCardIcon, FileText } from 'lucide-react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';

export default function CreditCardDetailsPage() {
    const params = useParams();
    const id = params.id as string;

    const [card, setCard] = useState<CreditCard | null>(null);
    const [invoices, setInvoices] = useState<CreditCardInvoice[]>([]);
    const [selectedInvoiceId, setSelectedInvoiceId] = useState<string>('current');
    const [expenses, setExpenses] = useState<PagedResponse<Expense> | null>(null);
    const [loading, setLoading] = useState(true);
    const [loadingExpenses, setLoadingExpenses] = useState(false);

    useEffect(() => {
        if (id) {
            loadData();
        }
    }, [id]);

    useEffect(() => {
        if (selectedInvoiceId && selectedInvoiceId !== 'current') {
            loadExpenses(selectedInvoiceId);
        } else if (invoices.length > 0) {
            // Se 'current' ou nada selecionado, tenta pegar a fatura em aberto ou a mais recente
            const openInvoice = invoices.find(i => !i.isPaid) || invoices[0];
            if (openInvoice && openInvoice.id !== selectedInvoiceId) {
                setSelectedInvoiceId(openInvoice.id);
            }
        }
    }, [selectedInvoiceId, invoices]);

    const loadData = async () => {
        setLoading(true);
        try {
            const [cardData, invoicesData] = await Promise.all([
                financeService.getCreditCardById(id),
                financeService.getInvoicesByCreditCard(id)
            ]);
            setCard(cardData);
            // Ordena faturas da mais recente para a mais antiga
            const sortedInvoices = invoicesData.sort((a, b) => {
                if (a.referenceYear !== b.referenceYear) return b.referenceYear - a.referenceYear;
                return b.referenceMonth - a.referenceMonth;
            });
            setInvoices(sortedInvoices);

            // Seleciona a primeira fatura (mais recente) por padrão se houver
            if (sortedInvoices.length > 0) {
                setSelectedInvoiceId(sortedInvoices[0].id);
            }
        } catch (error) {
            console.error('Erro ao carregar dados do cartão:', error);
        } finally {
            setLoading(false);
        }
    };

    const loadExpenses = async (invoiceId: string) => {
        setLoadingExpenses(true);
        try {
            // TODO: Paginação real poderia ser implementada aqui
            const response = await financeService.getExpenses({
                creditCardInvoiceId: invoiceId,
                page: 1,
                pageSize: 100 // Trazendo mais itens por enquanto para ver a fatura completa
            });
            setExpenses(response);
        } catch (error) {
            console.error('Erro ao carregar despesas da fatura:', error);
        } finally {
            setLoadingExpenses(false);
        }
    };

    const selectedInvoice = useMemo(() =>
        invoices.find(i => i.id === selectedInvoiceId),
    [invoices, selectedInvoiceId]);

    const columns = useMemo<ColumnDef<Expense>[]>(() => [
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
                    <span className="text-xs text-gray-500 uppercase tracking-wider font-medium">{row.original.expenseCategoryName}</span>
                </div>
            ),
        },
        {
            accessorKey: 'amount',
            header: 'Valor',
            cell: ({ row }) => <span className="font-bold text-gray-900">{formatCurrency(row.original.amount)}</span>,
        },
    ], []);

    if (loading) return <div className="p-8 flex justify-center"><div className="animate-spin h-8 w-8 border-4 border-blue-600 rounded-full border-t-transparent"></div></div>;
    if (!card) return <div className="p-8 text-red-600">Cartão não encontrado</div>;

    return (
        <div className="p-8 max-w-[1600px] mx-auto space-y-8">
            {/* Header */}
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
                {/* Sidebar / Invoice Selector */}
                <div className="space-y-6">
                    <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                        <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                            <FileText className="h-5 w-5 text-gray-500" />
                            Fatura
                        </h2>

                        <div className="space-y-4">
                            <div>
                                <label className="text-sm font-medium text-gray-700 mb-1 block">Selecione o Mês</label>
                                <Select value={selectedInvoiceId} onValueChange={setSelectedInvoiceId}>
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

                {/* Main Content / Expenses List */}
                <div className="lg:col-span-2 space-y-6">
                    <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
                        <div className="p-6 border-b border-gray-100 flex justify-between items-center">
                            <h2 className="text-lg font-semibold">Lançamentos</h2>
                            <span className="text-sm text-gray-500">{expenses?.totalCount || 0} despesas</span>
                        </div>

                        <FinancialGrid
                            columns={columns}
                            data={expenses?.items || []}
                            pageCount={expenses?.totalPages || 0}
                            page={1} // TODO: Implementar paginação na state se necessário
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
