'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { getDailyInsights } from "@/services/aiInsights";
import { Info, Sparkles } from "lucide-react";
import { useEffect, useState } from "react";

export function AiInsightsWidget() {
  const [insight, setInsight] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const res = await getDailyInsights();
        setInsight(res.text);
      } catch (err) {
        setError("Erro ao carregar insights.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="AI Command Center"
      icon={<Sparkles className="h-5 w-5 text-primary" />}
      isLoading={isLoading}
      error={error}
      className="col-span-1 md:col-span-2 lg:col-span-4 bg-primary/5 border-primary/20"
      infoTooltip="Nossa Inteligência Artificial analisa os dados diários do seu CRM para fornecer insights como um consultor estratégico de negócios."
    >
      <Alert className="mb-4 bg-background/50 border-primary/10">
        <Info className="h-4 w-4 text-primary" />
        <AlertDescription className="text-muted-foreground text-xs">
          O <strong>AI Command Center</strong> cruza dados dos seus leads e envios de e-mail para sugerir os próximos passos práticos de conversão.
        </AlertDescription>
      </Alert>
      {insight ? (
        <div className="text-sm text-foreground whitespace-pre-wrap leading-relaxed border-l-4 border-primary pl-4 py-1">
          {insight}
        </div>
      ) : (
        <div className="text-sm text-muted-foreground">Nenhum insight gerado.</div>
      )}
    </WidgetCard>
  );
}
