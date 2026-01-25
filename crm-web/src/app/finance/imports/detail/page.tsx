"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { financeService, StatementImportDetail } from "@/services/finance";
import { ArrowLeft, Calendar, CreditCard, FileText, Landmark } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useState, Suspense } from "react";
import { ImportedTransactionsTable } from "../components/ImportedTransactionsTable";
import { ImportStatusBadge } from "../components/ImportStatusBadge";

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  }).format(new Date(dateString));
};

const formatTime = (dateString: string) => {
  return new Intl.DateTimeFormat('pt-BR', {
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString));
};

function ImportDetailContent() {
  const searchParams = useSearchParams();
  const id = searchParams.get("id");
  
  const [detail, setDetail] = useState<StatementImportDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!id) {
        setIsLoading(false);
        return;
    }

    const loadDetail = async () => {
      try {
        const data = await financeService.getStatementImportById(id);
        setDetail(data);
      } catch (error) {
        console.error("Erro ao carregar detalhes da importação:", error);
      } finally {
        setIsLoading(false);
      }
    };
    loadDetail();
  }, [id]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-20">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (!id || !detail) {
    return (
      <div className="container mx-auto py-10 text-center">
        <h2 className="text-2xl font-bold">Importação não encontrada</h2>
        <Button asChild variant="link" className="mt-4">
          <Link href="/finance/imports">Voltar para a lista</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-6 space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="outline" size="icon" asChild>
          <Link href="/finance/imports">
            <ArrowLeft className="h-4 w-4" />
          </Link>
        </Button>
        <div>
          <h1 className="text-2xl font-bold">Detalhes da Importação</h1>
          <p className="text-muted-foreground">{detail.summary.fileName}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2 text-muted-foreground">
              <Calendar className="h-4 w-4" />
              Data de Importação
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatDate(detail.summary.createdAt)}
            </div>
            <p className="text-xs text-muted-foreground">
              às {formatTime(detail.summary.createdAt)}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2 text-muted-foreground">
              {detail.summary.financialAccountName ? <Landmark className="h-4 w-4" /> : <CreditCard className="h-4 w-4" />}
              Destino
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold truncate">
              {detail.summary.financialAccountName || detail.summary.creditCardGroupName || "N/A"}
            </div>
            <p className="text-xs text-muted-foreground">
              {detail.summary.financialAccountName ? "Conta Corrente" : "Cartão de Crédito"}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2 text-muted-foreground">
              <FileText className="h-4 w-4" />
              Resumo
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between">
              <div className="text-2xl font-bold">
                {detail.summary.processedRecords} / {detail.summary.totalRecords}
              </div>
              <ImportStatusBadge status={detail.summary.status} />
            </div>
            <p className="text-xs text-muted-foreground">
              Transações processadas com sucesso
            </p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Transações Extraídas</CardTitle>
          <CardDescription>
            Lista de transações encontradas no arquivo CSV e seu status de processamento.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <ImportedTransactionsTable transactions={detail.transactions} />
        </CardContent>
      </Card>
    </div>
  );
}

export default function ImportDetailPage() {
    return (
        <Suspense fallback={<div>Carregando...</div>}>
            <ImportDetailContent />
        </Suspense>
    )
}
