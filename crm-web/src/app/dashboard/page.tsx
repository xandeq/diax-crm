'use client';

import { RecentPromptsWidget } from "@/components/dashboard/ai/RecentPromptsWidget";
import { CrmSummaryWidget } from "@/components/dashboard/crm/CrmSummaryWidget";
import { FinanceSummaryWidget } from "@/components/dashboard/finance/FinanceSummaryWidget";
import { ShoppingListWidget } from "@/components/dashboard/household/ShoppingListWidget";
import { RecentSnippetsWidget } from "@/components/dashboard/snippets/RecentSnippetsWidget";
import { SystemHealthWidget } from "@/components/dashboard/system/SystemHealthWidget";
import { Badge } from "@/components/ui/badge";
import { useAuth } from '@/contexts/AuthContext';
import { me } from '@/services/auth';
import { MeResponse } from '@/types/auth';
import { Loader2 } from "lucide-react";
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

export default function DashboardPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [userData, setUserData] = useState<MeResponse | null>(null);

  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }

    me().then(setUserData).catch(console.error);
  }, [isAuthenticated, authLoading, router]);

  if (authLoading || !userData) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="flex-1 space-y-4 p-8 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">Dashboard</h2>
        <div className="hidden md:flex items-center space-x-2">
           <span className="text-sm text-muted-foreground mr-2">Bem-vindo, {userData.email}</span>
           {userData.roles?.map(role => (
            <Badge key={role} variant="secondary">
              {role}
            </Badge>
          ))}
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Finance spans 2 columns */}
        <FinanceSummaryWidget />
        <CrmSummaryWidget />
        <SystemHealthWidget />
        <ShoppingListWidget />
        <RecentPromptsWidget />
        <RecentSnippetsWidget />
      </div>
    </div>
  );
}
