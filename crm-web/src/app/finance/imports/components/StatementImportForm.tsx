"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { CreditCard, financeService, FinancialAccount, StatementImportType } from "@/services/finance";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Upload } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import * as z from "zod";

const formSchema = z.object({
  importType: z.string().min(1, "Selecione o tipo de importação"),
  financialAccountId: z.string().optional(),
  creditCardId: z.string().optional(),
}).refine(data => {
  if (data.importType === StatementImportType.Account.toString() && !data.financialAccountId) return false;
  if (data.importType === StatementImportType.CreditCard.toString() && !data.creditCardId) return false;
  return true;
}, {
  message: "Selecione a conta ou cartão correspondente",
  path: ["financialAccountId"] // This path is a bit weird for a general error, but keeping it simple
});

type FormValues = z.infer<typeof formSchema>;

interface StatementImportFormProps {
  onSuccess: () => void;
}

export function StatementImportForm({ onSuccess }: StatementImportFormProps) {
  const [file, setFile] = useState<File | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      importType: StatementImportType.Account.toString(),
    },
  });

  const importType = form.watch("importType");

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [accs, cards] = await Promise.all([
          financeService.getActiveFinancialAccounts(),
          financeService.getCreditCards(),
        ]);
        setAccounts(accs);
        setCreditCards(cards.filter(c => c.isActive));
      } catch (err) {
        console.error("Erro ao carregar dados para importação", err);
      }
    };
    fetchData();
  }, []);

  const onFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setFile(e.target.files[0]);
    }
  };

  const onSubmit = async (values: FormValues) => {
    if (!file) {
      setError("Selecione um arquivo CSV para continuar.");
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await financeService.uploadStatement({
        importType: parseInt(values.importType),
        financialAccountId: values.financialAccountId,
        creditCardId: values.creditCardId,
      }, file);

      form.reset();
      setFile(null);
      // Reset input file manually
      const fileInput = document.getElementById('statement-file') as HTMLInputElement;
      if (fileInput) fileInput.value = '';

      onSuccess();
    } catch (err: any) {
      setError(err.message || "Erro ao realizar upload do extrato.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Nova Importação</CardTitle>
        <CardDescription>Faça upload de um extrato bancário em formato CSV.</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="importType">Tipo de Importação</Label>
              <Select
                onValueChange={(value) => form.setValue("importType", value)}
                value={form.watch("importType")}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione o tipo" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={StatementImportType.Account.toString()}>Conta Corrente / Pix</SelectItem>
                  <SelectItem value={StatementImportType.CreditCard.toString()}>Fatura de Cartão</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {importType === StatementImportType.Account.toString() ? (
              <div className="space-y-2">
                <Label htmlFor="financialAccountId">Conta Financeira</Label>
                <Select
                  onValueChange={(value) => form.setValue("financialAccountId", value)}
                  value={form.watch("financialAccountId")}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Selecione a conta" />
                  </SelectTrigger>
                  <SelectContent>
                    {accounts.map(acc => (
                      <SelectItem key={acc.id} value={acc.id}>{acc.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            ) : (
              <div className="space-y-2">
                <Label htmlFor="creditCardId">Cartão de Crédito</Label>
                <Select
                  onValueChange={(value) => form.setValue("creditCardId", value)}
                  value={form.watch("creditCardId")}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Selecione o cartão" />
                  </SelectTrigger>
                  <SelectContent>
                    {creditCards.map(card => (
                      <SelectItem key={card.id} value={card.id}>{card.name} - {card.lastFourDigits}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="statement-file">Arquivo (CSV ou PDF)</Label>
            <Input
              id="statement-file"
              type="file"
              accept=".csv,.pdf"
              onChange={onFileChange}
              className="cursor-pointer"
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}
          {form.formState.errors.financialAccountId && (
            <p className="text-sm text-destructive">{form.formState.errors.financialAccountId.message}</p>
          )}

          <Button type="submit" disabled={isLoading} className="w-full">
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Processando...
              </>
            ) : (
              <>
                <Upload className="mr-2 h-4 w-4" />
                Importar Extrato
              </>
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
