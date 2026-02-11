'use client';

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { cn, formatCurrency } from '@/lib/utils';
import { financeService, FinancialSummary } from '@/services/finance';
import { motion } from 'framer-motion';
import {
    ArrowRight,
    Banknote,
    Building2,
    Calendar,
    CreditCard,
    FolderOpen,
    Search,
    Settings,
    TrendingDown,
    TrendingUp,
    Wallet
} from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export function DashboardClient() {
    const [summary, setSummary] = useState<FinancialSummary | null>(null);
    const [loading, setLoading] = useState(true);
    const [period, setPeriod] = useState('current');

    useEffect(() => {
        loadSummary();
    }, [period]);

    const loadSummary = async () => {
        setLoading(true);
        try {
            // Calculate dates based on period
            const now = new Date();
            let startDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
            let endDate = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString();

            if (period === 'last_month') {
                startDate = new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString();
                endDate = new Date(now.getFullYear(), now.getMonth(), 0).toISOString();
            } else if (period === 'year') {
                startDate = new Date(now.getFullYear(), 0, 1).toISOString();
                endDate = new Date(now.getFullYear(), 11, 31).toISOString();
            }

            const data = await financeService.getFinancialSummary({ startDate, endDate });
            setSummary(data);
        } catch (error) {
            console.error('Failed to load financial summary:', error);
            // Fallback mock data for visual development if API fails/is inconsistent during dev
            // remove in production if stable
        } finally {
            setLoading(false);
        }
    };

    const container = {
        hidden: { opacity: 0 },
        show: {
            opacity: 1,
            transition: {
                staggerChildren: 0.1
            }
        }
    };

    const item = {
        hidden: { y: 20, opacity: 0 },
        show: { y: 0, opacity: 1 }
    };

    return (
        <div className="p-8 max-w-[1600px] mx-auto space-y-8">
            <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-2">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900 tracking-tight">Visão Geral</h1>
                    <p className="text-gray-500 mt-1">Acompanhe o desempenho financeiro da sua empresa.</p>
                </div>

                <div className="flex items-center gap-2 bg-white p-1 rounded-lg border border-gray-200 shadow-sm">
                    <Select value={period} onValueChange={setPeriod}>
                        <SelectTrigger className="w-[180px] border-0 focus:ring-0">
                            <Calendar className="mr-2 h-4 w-4 text-gray-500" />
                            <SelectValue placeholder="Selecione o período" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="current">Este Mês</SelectItem>
                            <SelectItem value="last_month">Mês Passado</SelectItem>
                            <SelectItem value="year">Este Ano</SelectItem>
                        </SelectContent>
                    </Select>
                </div>
            </div>

            <motion.div
                variants={container}
                initial="hidden"
                animate="show"
                className="space-y-8"
            >
                {/* Main Action Cards - Single Row */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                    <motion.div variants={item}>
                        <Link href="/finance/incomes" className="group block h-full">
                            <div className="h-full bg-gradient-to-br from-white to-green-50/50 p-6 rounded-2xl shadow-sm border border-green-100 hover:shadow-md transition-all hover:-translate-y-1">
                                <div className="flex justify-between items-start mb-4">
                                    <div className="p-3 bg-green-100 rounded-xl text-green-600 group-hover:bg-green-600 group-hover:text-white transition-colors">
                                        <TrendingUp className="h-6 w-6" />
                                    </div>
                                    <span className="text-xs font-semibold text-green-700 bg-green-100 px-2 py-1 rounded-full">+ Entradas</span>
                                </div>
                                <h3 className="text-lg font-semibold text-gray-900 mb-1">Receitas</h3>
                                <p className="text-sm text-gray-500 mb-4">Gerenciar e lançar novas receitas.</p>
                                <div className="flex items-center text-sm font-medium text-green-600 group-hover:text-green-700">
                                    Acessar <ArrowRight className="ml-1 h-4 w-4 group-hover:translate-x-1 transition-transform" />
                                </div>
                            </div>
                        </Link>
                    </motion.div>

                    <motion.div variants={item}>
                        <Link href="/finance/expenses" className="group block h-full">
                            <div className="h-full bg-gradient-to-br from-white to-red-50/50 p-6 rounded-2xl shadow-sm border border-red-100 hover:shadow-md transition-all hover:-translate-y-1">
                                <div className="flex justify-between items-start mb-4">
                                    <div className="p-3 bg-red-100 rounded-xl text-red-600 group-hover:bg-red-600 group-hover:text-white transition-colors">
                                        <TrendingDown className="h-6 w-6" />
                                    </div>
                                    <span className="text-xs font-semibold text-red-700 bg-red-100 px-2 py-1 rounded-full">- Saídas</span>
                                </div>
                                <h3 className="text-lg font-semibold text-gray-900 mb-1">Despesas</h3>
                                <p className="text-sm text-gray-500 mb-4">Gerenciar pagamentos e custos.</p>
                                <div className="flex items-center text-sm font-medium text-red-600 group-hover:text-red-700">
                                    Acessar <ArrowRight className="ml-1 h-4 w-4 group-hover:translate-x-1 transition-transform" />
                                </div>
                            </div>
                        </Link>
                    </motion.div>

                    <motion.div variants={item}>
                        <Link href="/finance/credit-cards" className="group block h-full">
                            <div className="h-full bg-gradient-to-br from-white to-blue-50/50 p-6 rounded-2xl shadow-sm border border-blue-100 hover:shadow-md transition-all hover:-translate-y-1">
                                <div className="flex justify-between items-start mb-4">
                                    <div className="p-3 bg-blue-100 rounded-xl text-blue-600 group-hover:bg-blue-600 group-hover:text-white transition-colors">
                                        <CreditCard className="h-6 w-6" />
                                    </div>
                                    <span className="text-xs font-semibold text-blue-700 bg-blue-100 px-2 py-1 rounded-full">Crédito</span>
                                </div>
                                <h3 className="text-lg font-semibold text-gray-900 mb-1">Cartões</h3>
                                <p className="text-sm text-gray-500 mb-4">Faturas, limites e conciliação.</p>
                                <div className="flex items-center text-sm font-medium text-blue-600 group-hover:text-blue-700">
                                    Acessar <ArrowRight className="ml-1 h-4 w-4 group-hover:translate-x-1 transition-transform" />
                                </div>
                            </div>
                        </Link>
                    </motion.div>

                    <motion.div variants={item}>
                        <Link href="/finance/imports" className="group block h-full">
                            <div className="h-full bg-gradient-to-br from-white to-amber-50/50 p-6 rounded-2xl shadow-sm border border-amber-100 hover:shadow-md transition-all hover:-translate-y-1">
                                <div className="flex justify-between items-start mb-4">
                                    <div className="p-3 bg-amber-100 rounded-xl text-amber-600 group-hover:bg-amber-600 group-hover:text-white transition-colors">
                                        <FolderOpen className="h-6 w-6" />
                                    </div>
                                    <span className="text-xs font-semibold text-amber-700 bg-amber-100 px-2 py-1 rounded-full">Arquivos</span>
                                </div>
                                <h3 className="text-lg font-semibold text-gray-900 mb-1">Importação</h3>
                                <p className="text-sm text-gray-500 mb-4">Processar extratos via IA.</p>
                                <div className="flex items-center text-sm font-medium text-amber-600 group-hover:text-amber-700">
                                    Acessar <ArrowRight className="ml-1 h-4 w-4 group-hover:translate-x-1 transition-transform" />
                                </div>
                            </div>
                        </Link>
                    </motion.div>
                </div>

                {/* Financial Summary Stats */}
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Cash Flow Card */}
                    <motion.div variants={item} className="lg:col-span-2">
                        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6 h-full">
                            <div className="flex items-center justify-between mb-6">
                                <h2 className="text-lg font-bold text-gray-900 flex items-center gap-2">
                                    <Wallet className="h-5 w-5 text-gray-500" />
                                    Fluxo de Caixa
                                </h2>
                                {!loading && summary && (
                                    <span className={cn(
                                        "text-sm font-medium px-2 py-1 rounded-full",
                                        summary.netCashFlow >= 0 ? "bg-green-100 text-green-700" : "bg-red-100 text-red-700"
                                    )}>
                                        {summary.netCashFlow >= 0 ? 'Positivo' : 'Negativo'}
                                    </span>
                                )}
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                                <div className="space-y-2">
                                    <p className="text-sm text-gray-500 font-medium uppercase tracking-wider">Entradas</p>
                                    <p className="text-3xl font-bold text-gray-900">
                                        {loading ? <span className="animate-pulse bg-gray-200 h-8 w-24 rounded block"></span> : formatCurrency(summary?.totalIncome || 0)}
                                    </p>
                                    <div className="h-1.5 w-full bg-gray-100 rounded-full overflow-hidden">
                                        <motion.div
                                            initial={{ width: 0 }}
                                            animate={{ width: "100%" }}
                                            transition={{ duration: 1, ease: "easeOut" }}
                                            className="h-full bg-green-500 rounded-full"
                                        />
                                    </div>
                                </div>

                                <div className="space-y-2">
                                    <p className="text-sm text-gray-500 font-medium uppercase tracking-wider">Saídas Totais</p>
                                    <p className="text-3xl font-bold text-gray-900">
                                        {loading ? <span className="animate-pulse bg-gray-200 h-8 w-24 rounded block"></span> : formatCurrency(summary?.totalExpenses || 0)}
                                    </p>
                                    <div className="h-1.5 w-full bg-gray-100 rounded-full overflow-hidden">
                                        <motion.div
                                            initial={{ width: 0 }}
                                            animate={{ width: summary ? `${Math.min((summary.totalExpenses / (summary.totalIncome || 1)) * 100, 100)}%` : 0 }}
                                            transition={{ duration: 1, ease: "easeOut" }}
                                            className="h-full bg-red-500 rounded-full"
                                        />
                                    </div>
                                    <p className="text-xs text-gray-400">
                                        Pagos: {formatCurrency(summary?.totalPaidExpenses || 0)}
                                    </p>
                                </div>

                                <div className="space-y-2">
                                    <p className="text-sm text-gray-500 font-medium uppercase tracking-wider">Saldo Líquido</p>
                                    <p className={cn(
                                        "text-3xl font-bold",
                                        (summary?.netCashFlow || 0) >= 0 ? "text-green-600" : "text-red-600"
                                    )}>
                                        {loading ? <span className="animate-pulse bg-gray-200 h-8 w-24 rounded block"></span> : formatCurrency(summary?.netCashFlow || 0)}
                                    </p>
                                    <div className="bg-gray-50 px-2 py-1 rounded text-xs text-center text-gray-500">
                                        Resultante do período
                                    </div>
                                </div>
                            </div>
                        </div>
                    </motion.div>

                    {/* Projections Card */}
                    <motion.div variants={item}>
                        <div className="bg-gradient-to-br from-indigo-600 to-indigo-800 rounded-2xl shadow-lg p-6 text-white h-full relative overflow-hidden">
                            <div className="absolute top-0 right-0 p-3 opacity-10">
                                <TrendingUp className="h-32 w-32" />
                            </div>

                            <h2 className="text-lg font-bold mb-6 flex items-center gap-2 relative z-10">
                                <Search className="h-5 w-5 text-indigo-200" />
                                Projeção
                            </h2>

                            <div className="space-y-6 relative z-10">
                                <div>
                                    <p className="text-indigo-200 text-sm mb-1">A Receber (Pendente)</p>
                                    <p className="text-2xl font-bold">{loading ? '...' : formatCurrency(summary?.pendingCash || 0)}</p>
                                </div>

                                <div>
                                    <p className="text-indigo-200 text-sm mb-1">A Pagar (Pendente)</p>
                                    <p className="text-2xl font-bold">{loading ? '...' : formatCurrency(summary?.totalPendingExpenses || 0)}</p>
                                </div>

                                <div className="pt-4 border-t border-indigo-500/30">
                                    <p className="text-indigo-200 text-sm mb-1">Saldo Projetado</p>
                                    <p className="text-3xl font-bold">{loading ? '...' : formatCurrency(summary?.projectedCashFlow || 0)}</p>
                                </div>
                            </div>
                        </div>
                    </motion.div>
                </div>

                {/* Quick Management Links */}
                <motion.div variants={item}>
                    <h2 className="text-lg font-semibold mb-4 text-gray-700 flex items-center gap-2">
                        <Settings className="h-5 w-5" />
                        Gerenciamento Rápido
                    </h2>
                    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                        <Link href="/finance/accounts" className="flex items-center gap-3 p-4 bg-white rounded-xl shadow-sm border border-gray-100 hover:border-indigo-200 hover:shadow-md transition-all">
                            <div className="p-2 bg-indigo-50 rounded-lg text-indigo-600">
                                <Building2 className="h-5 w-5" />
                            </div>
                            <div>
                                <h3 className="font-medium text-gray-900">Contas Bancárias</h3>
                            </div>
                        </Link>

                        <Link href="/finance/categories/income" className="flex items-center gap-3 p-4 bg-white rounded-xl shadow-sm border border-gray-100 hover:border-green-200 hover:shadow-md transition-all">
                            <div className="p-2 bg-green-50 rounded-lg text-green-600">
                                <FolderOpen className="h-5 w-5" />
                            </div>
                            <div>
                                <h3 className="font-medium text-gray-900">Categorias (Entrada)</h3>
                            </div>
                        </Link>

                        <Link href="/finance/categories/expense" className="flex items-center gap-3 p-4 bg-white rounded-xl shadow-sm border border-gray-100 hover:border-red-200 hover:shadow-md transition-all">
                            <div className="p-2 bg-red-50 rounded-lg text-red-600">
                                <Banknote className="h-5 w-5" />
                            </div>
                            <div>
                                <h3 className="font-medium text-gray-900">Categorias (Saída)</h3>
                            </div>
                        </Link>
                    </div>
                </motion.div>
            </motion.div>
        </div>
    );
}
