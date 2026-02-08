'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/lib/utils";
import { checklistService } from "@/services/checklistService";
import { ChecklistItem, ChecklistItemStatus } from "@/types/household";
import { ArrowRight, ShoppingCart } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

export function ShoppingListWidget() {
  const [items, setItems] = useState<ChecklistItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const response = await checklistService.getItems({
          page: 1,
          pageSize: 5,
          sortBy: "estimatedPrice",
          sortDir: "desc",
          status: ChecklistItemStatus.ToBuy
        });
        setItems(response.items);
      } catch (err) {
        setError("Erro ao carregar lista de compras.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="Lista de Compras (Top 5 mais caros)"
      icon={<ShoppingCart className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
      action={
        <Button asChild variant="default" size="sm" className="gap-2">
          <Link href="/household/checklists">
            Ver lista
            <ArrowRight className="h-4 w-4" />
          </Link>
        </Button>
      }
    >
      {items.length === 0 ? (
        <div className="text-sm text-muted-foreground">Nenhum item encontrado.</div>
      ) : (
        <ul className="space-y-2">
          {items.map(item => {
            const price = item.actualPrice ?? item.estimatedPrice;
            return (
              <li key={item.id} className="flex items-center justify-between gap-2">
                <span className="min-w-0 flex-1 truncate text-sm">{item.title}</span>
                <span className="text-sm font-semibold">
                  {price !== undefined && price !== null ? formatCurrency(price) : "-"}
                </span>
              </li>
            );
          })}
        </ul>
      )}
    </WidgetCard>
  );
}
