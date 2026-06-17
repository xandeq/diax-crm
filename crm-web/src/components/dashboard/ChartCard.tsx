import * as React from "react";
import { motion } from "framer-motion";
import { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { EmptyState } from "./EmptyState";
import dynamic from "next/dynamic";

const ApexChart = dynamic(() => import("react-apexcharts"), { ssr: false });

const cardHover = {
  y: -5,
  scale: 1.015,
  boxShadow: "0 22px 60px rgba(0,0,0,0.45)",
  transition: { type: "spring", stiffness: 360, damping: 26 },
} as const;

const cardTap = { scale: 0.985, transition: { type: "spring", stiffness: 500, damping: 30 } } as const;

export interface ChartCardProps {
  title: string;
  subtitle?: string;
  loading?: boolean;
  hasData?: boolean;
  emptyIcon?: LucideIcon;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyActionLabel?: string;
  emptyActionHref?: string;
  onEmptyAction?: () => void;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  chartConfig: any;
  headerActions?: React.ReactNode;
  footer?: React.ReactNode;
  height?: number;
  className?: string;
}

export function ChartCard({
  title,
  subtitle,
  loading = false,
  hasData = true,
  emptyIcon,
  emptyTitle = "Sem dados disponíveis",
  emptyDescription = "Não há dados suficientes cadastrados para exibir este gráfico.",
  emptyActionLabel,
  emptyActionHref,
  onEmptyAction,
  chartConfig,
  headerActions,
  footer,
  height = 240,
  className,
}: ChartCardProps) {
  const isReducedMotion = typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  return (
    <motion.div
      className={cn(
        "relative flex flex-col justify-between overflow-hidden rounded-xl border border-zinc-800 bg-zinc-900/35 p-5 transition-all duration-300",
        "before:absolute before:top-0 before:left-[14%] before:right-[14%] before:h-[1px] before:bg-gradient-to-r before:from-transparent before:via-white/10 before:to-transparent before:pointer-events-none",
        "hover:border-[#00D4AA]/20",
        className
      )}
      whileHover={isReducedMotion ? undefined : cardHover}
      whileTap={isReducedMotion ? undefined : cardTap}
    >
      <div>
        <div className="flex justify-between items-center mb-4">
          <div>
            <h3 className="text-sm font-semibold text-zinc-100 tracking-tight">{title}</h3>
            {subtitle && <p className="text-xs text-zinc-500 mt-0.5">{subtitle}</p>}
          </div>
          {headerActions && <div className="flex items-center gap-2">{headerActions}</div>}
        </div>

        <div style={{ minHeight: height }} className="flex flex-col justify-center">
          {loading ? (
            <div className="w-full bg-zinc-800/40 rounded animate-pulse" style={{ height }} />
          ) : !hasData && emptyIcon ? (
            <EmptyState
              title={emptyTitle}
              description={emptyDescription}
              icon={emptyIcon}
              actionLabel={emptyActionLabel}
              actionHref={emptyActionHref}
              onAction={onEmptyAction}
            />
          ) : chartConfig ? (
            <ApexChart {...chartConfig} height={height} />
          ) : (
            <div className="flex items-center justify-center text-xs text-zinc-500" style={{ height }}>
              Carregando gráfico...
            </div>
          )}
        </div>
      </div>

      {footer && (
        <div className="mt-4 pt-4 border-t border-zinc-800/60 flex items-center justify-between">
          {footer}
        </div>
      )}
    </motion.div>
  );
}
