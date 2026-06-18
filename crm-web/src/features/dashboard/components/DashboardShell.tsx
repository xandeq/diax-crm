'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { LayoutDashboard, Users, Mail, Wallet, Sparkles, Cpu } from 'lucide-react';
import { DashboardHeader } from './DashboardHeader';
import { DashboardTabs } from './DashboardTabs';
import { OverviewTab } from './OverviewTab';
import { CrmTab } from './CrmTab';
import { MarketingTab } from './MarketingTab';
import { FinanceTab } from './FinanceTab';
import { IntelligenceTab } from './IntelligenceTab';
import { OpsTab } from './OpsTab';
import { AnimatePresence, motion } from 'framer-motion';

export type DashboardTabType = 'overview' | 'crm' | 'marketing' | 'finance' | 'intelligence' | 'ops';

export function DashboardShell() {
  const [activeTab, setActiveTab] = useState<DashboardTabType>('overview');
  const [lastSync, setLastSync] = useState<Date>(new Date());
  const [isSyncing, setIsSyncing] = useState<boolean>(false);

  const handleSync = () => {
    setIsSyncing(true);
    // Disparar evento global para fazer refetch das queries do React Query
    window.dispatchEvent(new Event('dashboard-sync'));
    setTimeout(() => {
      setLastSync(new Date());
      setIsSyncing(false);
    }, 1000);
  };

  const tabs = [
    { id: 'overview' as const, label: 'Visão Geral', icon: <LayoutDashboard className="h-4 w-4" /> },
    { id: 'crm' as const, label: 'CRM & Pipeline', icon: <Users className="h-4 w-4" /> },
    { id: 'marketing' as const, label: 'Outreach & Marketing', icon: <Mail className="h-4 w-4" /> },
    { id: 'finance' as const, label: 'Financeiro', icon: <Wallet className="h-4 w-4" /> },
    { id: 'intelligence' as const, label: 'Intelligence', icon: <Sparkles className="h-4 w-4" /> },
    { id: 'ops' as const, label: 'Monitoramento & Ops', icon: <Cpu className="h-4 w-4" /> },
  ];

  return (
    <div className="dsh flex flex-col gap-6 p-1 sm:p-4 md:p-6 text-zinc-100 max-w-[1600px] 2xl:max-w-[1750px] w-full mx-auto">
      {/* Background Mesh Gradient */}
      <div className="absolute inset-0 -z-10 overflow-hidden pointer-events-none" aria-hidden="true">
        <div className="absolute inset-[-10%] bg-radial-gradient-aurora opacity-20 filter blur-[100px]" />
      </div>

      <DashboardHeader 
        lastSync={lastSync} 
        isSyncing={isSyncing} 
        onSync={handleSync} 
      />

      <DashboardTabs 
        tabs={tabs} 
        activeTab={activeTab} 
        onChange={setActiveTab} 
      />

      <div className="mt-4">
        <AnimatePresence mode="wait">
          <motion.div
            key={activeTab}
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -12 }}
            transition={{ duration: 0.25, ease: [0.16, 1, 0.3, 1] }}
          >
            {activeTab === 'overview' && <OverviewTab />}
            {activeTab === 'crm' && <CrmTab />}
            {activeTab === 'marketing' && <MarketingTab />}
            {activeTab === 'finance' && <FinanceTab />}
            {activeTab === 'intelligence' && <IntelligenceTab />}
            {activeTab === 'ops' && <OpsTab />}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  );
}
