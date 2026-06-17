'use client';

import * as React from 'react';
import { motion } from 'framer-motion';
import { LucideIcon } from 'lucide-react';
import Link from 'next/link';

interface QuickActionCardProps {
  title: string;
  description: string;
  icon: LucideIcon;
  href: string;
  actionText?: string;
  color?: 'teal' | 'blue' | 'emerald' | 'amber' | 'pink';
}

export function QuickActionCard({
  title,
  description,
  icon: Icon,
  href,
  actionText = 'Acessar',
  color = 'teal',
}: QuickActionCardProps) {
  const colorMap = {
    teal: 'text-teal-400 bg-teal-500/10 border-teal-500/20 hover:border-teal-500/30',
    blue: 'text-blue-400 bg-blue-500/10 border-blue-500/20 hover:border-blue-500/30',
    emerald: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20 hover:border-emerald-500/30',
    amber: 'text-amber-400 bg-amber-500/10 border-amber-500/20 hover:border-amber-500/30',
    pink: 'text-pink-400 bg-pink-500/10 border-pink-500/20 hover:border-pink-500/30',
  };

  return (
    <div
      className="p-5 rounded-xl border border-zinc-800/80 bg-zinc-950/20 backdrop-blur-xl transition-all duration-300 hover:border-zinc-700/60"
    >
      <div className="flex gap-4">
        <div className={`p-2.5 rounded-lg border shrink-0 ${colorMap[color]}`}>
          <Icon className="h-5 w-5 stroke-[2px]" />
        </div>
        <div className="space-y-1 flex-1">
          <h4 className="text-sm font-extrabold text-zinc-200">{title}</h4>
          <p className="text-xs text-zinc-400 leading-normal font-medium">{description}</p>
        </div>
      </div>
      <div className="mt-4 flex justify-end">
        <Link
          href={href}
          className="text-xs font-semibold text-teal-400 hover:text-teal-300 transition-colors flex items-center gap-1"
        >
          <span>{actionText}</span>
          <span className="text-xs">→</span>
        </Link>
      </div>
    </div>
  );
}
