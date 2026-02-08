'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Button } from "@/components/ui/button";
import { getPromptHistory, UserPromptHistory } from "@/services/promptGenerator";
import { ArrowRight, Sparkles } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

export function RecentPromptsWidget() {
  const [items, setItems] = useState<UserPromptHistory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const history = await getPromptHistory(5);
        setItems(history);
      } catch (err) {
        setError("Erro ao carregar prompts.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="Últimos Prompts"
      icon={<Sparkles className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
      action={
        <Button asChild variant="default" size="sm" className="gap-2">
          <Link href="/utilities/prompt-generator">
            Ver prompts
            <ArrowRight className="h-4 w-4" />
          </Link>
        </Button>
      }
    >
      {items.length === 0 ? (
        <div className="text-sm text-muted-foreground">Nenhum prompt encontrado.</div>
      ) : (
        <ul className="space-y-2">
          {items.map(item => (
            <li key={item.id} className="min-w-0">
              <span className="block truncate text-sm">{item.inputPreview}</span>
            </li>
          ))}
        </ul>
      )}
    </WidgetCard>
  );
}
