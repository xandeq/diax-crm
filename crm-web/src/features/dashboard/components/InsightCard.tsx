'use client';

import * as React from 'react';
import { motion } from 'framer-motion';
import { Sparkles, ArrowRight, TrendingUp } from 'lucide-react';
import { cn } from '@/lib/utils';

interface InsightCardProps {
  title: string;
  description: string;
  badgeText?: string;
  color?: 'teal' | 'blue' | 'amber' | 'purple';
  onClick?: () => void;
  className?: string;
}

export function InsightCard({
  title,
  description,
  badgeText = 'Insight IA',
  color = 'teal',
  onClick,
  className,
}: InsightCardProps) {
  const colorMap = {
    teal: {
      border: 'hover:border-teal-500/30',
      bg: 'bg-teal-500/5',
      text: 'text-teal-400',
      badge: 'bg-teal-500/10 text-teal-400 border-teal-500/20',
    },
    blue: {
      border: 'hover:border-blue-500/30',
      bg: 'bg-blue-500/5',
      text: 'text-blue-400',
      badge: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
    },
    amber: {
      border: 'hover:border-amber-500/30',
      bg: 'bg-amber-500/5',
      text: 'text-amber-400',
      badge: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
    },
    purple: {
      border: 'hover:border-purple-500/30',
      bg: 'bg-purple-500/5',
      text: 'text-purple-400',
      badge: 'bg-purple-500/10 text-purple-400 border-purple-500/20',
    },
  };

  const selected = colorMap[color];

  return (
    <motion.div
      whileHover={{ y: -3, scale: 1.01, transition: { duration: 0.2 } }}
      onClick={onClick}
      className={cn(
        "relative p-5 rounded-xl border border-zinc-800/80 bg-zinc-950/20 backdrop-blur-xl transition-colors duration-300",
        onClick && "cursor-pointer",
        selected.border,
        className
      )}
    >
      {/* Decorative gradient corner */}
      <div className={cn("absolute top-0 right-0 w-24 h-24 bg-radial-gradient opacity-10 rounded-tr-xl pointer-events-none")} />

      <div className="flex items-center justify-between gap-4 mb-3">
        <div className="flex items-center gap-2">
          <Sparkles className={cn("h-4.5 w-4.5", selected.text)} />
          <span className={cn("text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full border", selected.badge)}>
            {badgeText}
          </span>
        </div>
        
        {onClick && (
          <ArrowRight className="h-4 w-4 text-zinc-500 hover:text-zinc-300 transition-colors" />
        )}
      </div>

      <h4 className="text-sm font-bold text-zinc-100 mb-1.5">{title}</h4>
      <p className="text-xs text-zinc-400 leading-relaxed font-medium">{description}</p>
    </motion.div>
  );
}
