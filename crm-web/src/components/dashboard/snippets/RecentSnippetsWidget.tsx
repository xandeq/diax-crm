'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { SnippetResponse, snippetService } from "@/services/snippetService";
import { Code2 } from "lucide-react";
import { useEffect, useState } from "react";

export function RecentSnippetsWidget() {
  const [items, setItems] = useState<SnippetResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const snippets = await snippetService.getSnippets();
        const sorted = [...snippets].sort(
          (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
        setItems(sorted.slice(0, 5));
      } catch (err) {
        setError("Erro ao carregar snippets.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="Últimos Snippets"
      icon={<Code2 className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
    >
      {items.length === 0 ? (
        <div className="text-sm text-muted-foreground">Nenhum snippet encontrado.</div>
      ) : (
        <ul className="space-y-2">
          {items.map(item => (
            <li key={item.id} className="min-w-0">
              <span className="block truncate text-sm">{item.content}</span>
            </li>
          ))}
        </ul>
      )}
    </WidgetCard>
  );
}
