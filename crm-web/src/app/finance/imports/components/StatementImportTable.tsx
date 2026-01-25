"use client";

import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { StatementImport, StatementImportType } from "@/services/finance";
import { ExternalLink, FileText } from "lucide-react";
import Link from "next/link";
import { ImportStatusBadge } from "./ImportStatusBadge";

interface StatementImportTableProps {
  imports: StatementImport[];
  isLoading: boolean;
}

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString));
};

export function StatementImportTable({ imports, isLoading }: StatementImportTableProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (imports.length === 0) {
    return (
      <div className="text-center p-8 border rounded-lg bg-muted/20">
        <p className="text-muted-foreground">Nenhuma importação encontrada.</p>
      </div>
    );
  }

  return (
    <div className="border rounded-md">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Data</TableHead>
            <TableHead>Arquivo</TableHead>
            <TableHead>Tipo</TableHead>
            <TableHead>Conta/Cartão</TableHead>
            <TableHead className="text-right">Transações</TableHead>
            <TableHead className="text-center">Status</TableHead>
            <TableHead className="text-right">Ações</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {imports.map((item) => (
            <TableRow key={item.id}>
              <TableCell className="font-medium">
                {formatDate(item.createdAt)}
              </TableCell>
              <TableCell className="max-w-[200px] truncate" title={item.fileName}>
                <div className="flex items-center gap-2">
                  <FileText className="h-4 w-4 text-muted-foreground" />
                  {item.fileName}
                </div>
              </TableCell>
              <TableCell>
                {item.importType === StatementImportType.Account ? "Conta" : "Cartão"}
              </TableCell>
              <TableCell>
                {item.financialAccountName || item.creditCardGroupName || "—"}
              </TableCell>
              <TableCell className="text-right font-mono">
                {item.processedRecords} / {item.totalRecords}
              </TableCell>
              <TableCell className="text-center">
                <ImportStatusBadge status={item.status} />
              </TableCell>
              <TableCell className="text-right">
                <Button variant="ghost" size="sm" asChild>
                  <Link href={`/finance/imports/detail?id=${item.id}`}>
                    <ExternalLink className="h-4 w-4" />
                  </Link>
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
