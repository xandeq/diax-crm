'use client';

import * as React from 'react';
import { motion, LayoutGroup } from 'framer-motion';
import { DashboardTabType } from './DashboardShell';
import { cn } from '@/lib/utils';

interface TabItem {
  id: DashboardTabType;
  label: string;
  icon: React.ReactNode;
}

interface DashboardTabsProps {
  tabs: TabItem[];
  activeTab: DashboardTabType;
  onChange: (tab: DashboardTabType) => void;
}

export function DashboardTabs({ tabs, activeTab, onChange }: DashboardTabsProps) {
  return (
    <div className="w-full overflow-x-auto scrollbar-none pb-1 -mx-4 px-4 sm:mx-0 sm:px-0">
      <div className="inline-flex p-1 bg-zinc-950/40 border border-zinc-800/60 rounded-xl gap-1 backdrop-blur-xl min-w-max sm:min-w-0">
        <LayoutGroup id="dashboard-tab-slider">
          {tabs.map((tab) => {
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                role="tab"
                aria-selected={isActive}
                onClick={() => onChange(tab.id)}
                className={cn(
                  "relative flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg transition-colors cursor-pointer select-none",
                  isActive ? "text-zinc-950 font-bold" : "text-zinc-400 hover:text-zinc-200"
                )}
              >
                {isActive && (
                  <motion.span
                    layoutId="activeTabPill"
                    className="absolute inset-0 bg-teal-400 rounded-lg shadow-[0_4px_15px_rgba(45,212,191,0.3)]"
                    transition={{ type: 'spring', stiffness: 350, damping: 28 }}
                  />
                )}
                <span className="relative z-10 flex items-center gap-2">
                  {tab.icon}
                  {tab.label}
                </span>
              </button>
            );
          })}
        </LayoutGroup>
      </div>
    </div>
  );
}
