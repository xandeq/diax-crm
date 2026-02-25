'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Button } from "@/components/ui/button";
import { getCustomers } from "@/services/customers";
import { getLeads } from "@/services/leads";
import { ArrowRight, UserPlus, Users } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

export function CrmSummaryWidget() {
  const [counts, setCounts] = useState<{ leads: number; customers: number } | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const [leadsRes, customersRes] = await Promise.all([
            getLeads({ page: 1, pageSize: 1 }),
            getCustomers({ page: 1, pageSize: 1 })
        ]);

        setCounts({
            leads: leadsRes.totalCount,
            customers: customersRes.totalCount
        });
      } catch (err) {
        setError("Erro ao carregar dados do CRM.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="CRM & Clientes"
      icon={<Users className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
      action={
        <Button asChild variant="default" size="sm" className="gap-2">
          <Link href="/leads">
            Ver leads
            <ArrowRight className="h-4 w-4" />
          </Link>
        </Button>
      }
    >
      {counts && (
        <div className="grid grid-cols-2 gap-4">
             <div className="space-y-1">
                <div className="flex items-center gap-1 text-xs text-muted-foreground">
                  <UserPlus className="h-3 w-3" />
                  Leads
                </div>
                <p className="text-2xl font-bold">{counts.leads}</p>
             </div>
             <div className="space-y-1">
                <div className="flex items-center gap-1 text-xs text-muted-foreground">
                  <Users className="h-3 w-3" />
                  Clientes
                </div>
                <p className="text-2xl font-bold">{counts.customers}</p>
             </div>
        </div>
      )}
    </WidgetCard>
  );
}
