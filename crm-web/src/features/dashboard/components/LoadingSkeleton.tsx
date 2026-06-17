'use client';

import * as React from 'react';
import { motion } from 'framer-motion';

interface LoadingSkeletonProps {
  rows?: number;
  className?: string;
}

export function LoadingSkeleton({ rows = 3, className }: LoadingSkeletonProps) {
  return (
    <div className={`space-y-4 ${className}`}>
      {Array.from({ length: rows }).map((_, idx) => (
        <div
          key={idx}
          className="w-full p-5 bg-zinc-900/10 border border-zinc-800/40 rounded-xl space-y-3 relative overflow-hidden"
        >
          {/* Animated glow sweep overlay */}
          <motion.div
            className="absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/5 to-transparent pointer-events-none"
            animate={{ x: ['100%', '-100%'] }}
            transition={{ duration: 1.5, repeat: Infinity, ease: 'linear' }}
          />

          <div className="flex justify-between items-center">
            <div className="h-4 bg-zinc-800/60 rounded w-1/4" />
            <div className="h-6 bg-zinc-800/60 rounded-full w-16" />
          </div>
          <div className="h-8 bg-zinc-800/60 rounded w-1/2" />
          <div className="h-3 bg-zinc-800/40 rounded w-full mt-2" />
        </div>
      ))}
    </div>
  );
}

export function LoadingGrid({ cards = 4, className }: { cards?: number; className?: string }) {
  return (
    <div className={`grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 ${className}`}>
      {Array.from({ length: cards }).map((_, idx) => (
        <div
          key={idx}
          className="p-5 bg-zinc-900/10 border border-zinc-800/40 rounded-xl space-y-3 relative overflow-hidden"
        >
          <motion.div
            className="absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/5 to-transparent pointer-events-none"
            animate={{ x: ['100%', '-100%'] }}
            transition={{ duration: 1.5, repeat: Infinity, ease: 'linear' }}
          />
          <div className="h-3 bg-zinc-800/50 rounded w-1/3" />
          <div className="h-7 bg-zinc-800/60 rounded w-2/3" />
          <div className="h-14 bg-zinc-800/30 rounded w-full mt-2" />
        </div>
      ))}
    </div>
  );
}
