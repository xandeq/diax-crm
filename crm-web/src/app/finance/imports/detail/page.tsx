"use client";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { financeService, ImportStatus, StatementImportDetail, StatementImportPostPreview } from "@/services/finance";
import { ArrowLeft, Calendar, CheckCircle2, CreditCard, FileText, Info, Landmark, Loader2, Play } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
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
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [isPostingLoading, setIsPostingLoading] = useState(false);
  const [preview, setPreview] = useState<StatementImportPostPreview | null>(null);
  const [postResult, setPostResult] = useState<{ createdExpenses: number, createdIncomes: number, skipped: number, failed: number } | null>(null);

  const loadDetail = async () => {
    if (!id) return;
    try {
      const data = await financeService.getStatementImportById(id);
      setDetail(data);
    } catch (error) {
      console.error("Erro ao carregar detalhes da importação:", error);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (!id) {
        setIsLoading(false);
        return;
    }
    loadDetail();
  }, [id]);

  const handlePreview = async () => {
    if (!id) return;
    setIsPreviewLoading(true);
    try {
      const data = await financeService.previewStatementImportPost(id);
      setPreview(data);
    } catch (error) {
      console.error("Erro ao gerar preview:", error);
    } finally {
      setIsPreviewLoading(false);
    }
  };

  const handlePost = async () => {
    if (!id) return;
    if (!confirm("Deseja realmente postar todos os lançamentos desse extrato?")) return;

    setIsPostingLoading(true);
    try {
      const result = await financeService.postStatementImport(id, { force: false });
      setPostResult(result);
      setPreview(null);
      await loadDetail();
    } catch (error) {
      console.error("Erro ao excluir transação:", error);
    } finally {
      setIsPostingLoading(false);
    }
  };

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

      {detail.summary.status === ImportStatus.Completed && detail.summary.financialAccountId && (
        <Card className="border-primary/20 bg-primary/5">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <Play className="h-5 w-5 text-primary" />
              Postar Lançamentos
            </CardTitle>
            <CardDescription>
              Converta as transações deste extrato em despesas e receitas reais no sistema.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {!preview && !postResult && (
              <div className="flex flex-col items-center justify-center py-4 space-y-4 text-center">
                <p className="text-sm text-muted-foreground max-w-md">
                  Clique no botão abaixo para ver um resumo do que será criado no sistema antes de confirmar.
                </p>
                <Button onClick={handlePreview} disabled={isPreviewLoading}>
                  {isPreviewLoading ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Analisando...
                    </>
                  ) : (
                    "Gerar Preview da Postagem"
                  )}
                </Button>
              </div>
            )}

            {preview && (
              <div className="space-y-6">
                <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
                  <div className="bg-background p-3 rounded-lg border text-center">
                    <div className="text-xl font-bold">{preview.expensesToCreate}</div>
                    <div className="text-xs text-muted-foreground">Despesas</div>
                  </div>
                  <div className="bg-background p-3 rounded-lg border text-center">
                    <div className="text-xl font-bold">{preview.incomesToCreate}</div>
                    <div className="text-xs text-muted-foreground">Receitas</div>
                  </div>
                  <div className="bg-background p-3 rounded-lg border text-center">
                    <div className="text-xl font-bold">{preview.alreadyCreated}</div>
                    <div className="text-xs text-muted-foreground">Já Criadas</div>
                  </div>
                  <div className="bg-background p-3 rounded-lg border text-center">
                    <div className="text-xl font-bold">{preview.toIgnore}</div>
                    <div className="text-xs text-muted-foreground">Ignoradas</div>
                  </div>
                  <div className="bg-background p-3 rounded-lg border text-center">
                    <div className="text-xl font-bold text-destructive">{preview.failed}</div>
                    <div className="text-xs text-muted-foreground text-destructive">Falhas</div>
                  </div>
                </div>

                <Alert variant="default" className="bg-blue-50 border-blue-200">
                  <Info className="h-4 w-4 text-blue-600" />
                  <AlertTitle className="text-blue-800">Pronto para postar</AlertTitle>
                  <AlertDescription className="text-blue-700">
                    Todas as despesas serão marcadas como <b>Pagas</b> e as receitas como <b>Recebidas</b>, vinculadas à conta <b>{detail.summary.financialAccountName}</b>.
                  </AlertDescription>
                </Alert>

                <div className="flex justify-end gap-3">
                  <Button variant="outline" onClick={() => setPreview(null)} disabled={isPostingLoading}>
                    Cancelar
                  </Button>
                  <Button onClick={handlePost} disabled={isPostingLoading || (preview.expensesToCreate === 0 && preview.incomesToCreate === 0)}>
                    {isPostingLoading ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Postando...
                      </>
                    ) : (
                      <>
                        <CheckCircle2 className="mr-2 h-4 w-4" />
                        Confirmar Postagem
                      </>
                    )}
                  </Button>
                </div>
              </div>
            )}

            {postResult && (
              <Alert variant="default" className="bg-green-50 border-green-200">
                <CheckCircle2 className="h-4 w-4 text-green-600" />
                <AlertTitle className="text-green-800">Postagem Concluída!</AlertTitle>
                <AlertDescription className="text-green-700 space-y-2">
                  <p>Lançamentos processados com sucesso:</p>
                  <ul className="list-disc list-inside text-sm">
                    <li>{postResult.createdExpenses} despesas criadas</li>
                    <li>{postResult.createdIncomes} receitas criadas</li>
                    {postResult.skipped > 0 && <li>{postResult.skipped} já existentes/pulados</li>}
                    {postResult.failed > 0 && <li className="text-destructive font-bold">{postResult.failed} falhas</li>}
                  </ul>
                  <Button variant="outline" size="sm" onClick={() => setPostResult(null)} className="mt-2 border-green-300 text-green-800 hover:bg-green-100">
                    Fechar Aviso
                  </Button>
                </AlertDescription>
              </Alert>
            )}
          </CardContent>
        </Card>
      )}

      {detail.summary.importType === StatementImportType.CreditCard && (
        <Alert>
          <Info className="h-4 w-4" />
          <AlertTitle>Atenção</AlertTitle>
          <AlertDescription>
            A postagem automática de faturas de cartão de crédito será implementada em breve.
          </AlertDescription>
        </Alert>
      )}

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
