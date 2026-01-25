"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { financeService, StatementImport } from "@/services/finance";
import { RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { StatementImportForm } from "./components/StatementImportForm";
import { StatementImportTable } from "./components/StatementImportTable";

export default function FinanceImportsPage() {
  const [imports, setImports] = useState<StatementImport[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadImports = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await financeService.getStatementImports();
      setImports(data);
    } catch (error) {
      console.error("Erro ao carregar importações:", error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadImports();
  }, [loadImports]);

  return (
    <div className="container mx-auto py-6 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Importação de Extratos</h1>
          <p className="text-muted-foreground">
            Gerencie o upload e processamento de extratos bancários (CSV).
          </p>
        </div>
        <Button variant="outline" size="icon" onClick={loadImports} disabled={isLoading}>
          <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1">
          <StatementImportForm onSuccess={loadImports} />
        </div>

        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle>Histórico de Importações</CardTitle>
              <CardDescription>Ultimas importações realizadas no sistema.</CardDescription>
            </CardHeader>
            <CardContent>
              <StatementImportTable imports={imports} isLoading={isLoading} />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
