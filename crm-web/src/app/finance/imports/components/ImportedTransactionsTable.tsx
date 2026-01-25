"use client";

import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ImportedTransaction, ImportedTransactionStatus } from "@/services/finance";
import { CheckCircle2, AlertCircle, HelpCircle, Ban, ArrowRightLeft, PlusCircle } from "lucide-react";

interface ImportedTransactionsTableProps {
  transactions: ImportedTransaction[];
}

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  }).format(new Date(dateString));
};

export function ImportedTransactionsTable({ transactions }: ImportedTransactionsTableProps) {
  const getStatusIcon = (status: ImportedTransactionStatus) => {
    switch (status) {
      case ImportedTransactionStatus.Matched:
        return <ArrowRightLeft className="h-4 w-4 text-indigo-500" />;
      case ImportedTransactionStatus.Created:
        return <PlusCircle className="h-4 w-4 text-emerald-500" />;
      case ImportedTransactionStatus.Failed:
        return <Ban className="h-4 w-4 text-rose-500" />;
      case ImportedTransactionStatus.Ignored:
        return <AlertCircle className="h-4 w-4 text-amber-500" />;
      default:
        return <HelpCircle className="h-4 w-4 text-muted-foreground" />;
    }
  };

  const getStatusLabel = (status: ImportedTransactionStatus) => {
    switch (status) {
      case ImportedTransactionStatus.Matched: return "Conciliado";
      case ImportedTransactionStatus.Created: return "Criado";
      case ImportedTransactionStatus.Failed: return "Erro";
      case ImportedTransactionStatus.Ignored: return "Ignorado";
      default: return "Pendente";
    }
  };

  return (
    <div className="border rounded-md">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Data</TableHead>
            <TableHead>Descrição</TableHead>
            <TableHead className="text-right">Valor</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Erro</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {transactions.map((t, idx) => (
            <TableRow key={t.id || idx}>
              <TableCell className="whitespace-nowrap">
                {formatDate(t.transactionDate)}
              </TableCell>
              <TableCell className="max-w-[300px] truncate" title={t.rawDescription}>
                {t.rawDescription}
              </TableCell>
              <TableCell className={`text-right font-mono ${t.amount < 0 ? "text-rose-600" : "text-emerald-600"}`}>
                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(t.amount)}
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-2">
                  {getStatusIcon(t.status)}
                  <span className="text-xs font-medium">{getStatusLabel(t.status)}</span>
                </div>
              </TableCell>
              <TableCell className="text-xs text-rose-500 max-w-[200px] truncate">
                {t.errorMessage || "—"}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
