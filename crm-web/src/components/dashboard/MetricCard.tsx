import * as React from "react";
import { motion } from "framer-motion";
import { LucideIcon } from "lucide-react";
import { SpringMetric } from "./SpringMetric";
import { StatusBadge } from "./StatusBadge";
import { cn } from "@/lib/utils";
import dynamic from "next/dynamic";

const ApexChart = dynamic(() => import("react-apexcharts"), { ssr: false });

const animCfg = {
  enabled: true,
  easing: "easeinout" as const,
  speed: 1100,
  animateGradually: { enabled: true, delay: 120 },
  dynamicAnimation: { enabled: true, speed: 500 },
};

function sparkConfig(color: string, data: number[]) {
  return {
    type: "area" as const,
    height: 56,
    series: [{ data: data.length ? data : [0] }],
    options: {
      chart: { type: "area" as const, sparkline: { enabled: true }, toolbar: { show: false }, animations: animCfg },
      stroke: { curve: "smooth" as const, width: 2 },
      fill: { type: "gradient", gradient: { opacityFrom: 0.34, opacityTo: 0 } },
      colors: [color],
      tooltip: { enabled: false },
    },
  };
}

const cardHover = {
  y: -5,
  scale: 1.015,
  boxShadow: "0 22px 60px rgba(0,0,0,0.45)",
  transition: { type: "spring", stiffness: 360, damping: 26 },
} as const;

const cardTap = { scale: 0.985, transition: { type: "spring", stiffness: 500, damping: 30 } } as const;

export interface MetricCardProps {
  label: string;
  value: number;
  prefix?: string;
  suffix?: string;
  delta?: string;
  up?: boolean;
  spark?: number[];
  mini?: React.ReactNode;
  color: string;
  icon?: React.ReactNode;
  loading?: boolean;
  idx: number;
  className?: string;
}

export function MetricCard({
  label,
  value,
  prefix = "",
  suffix = "",
  delta,
  up,
  spark,
  mini,
  color,
  icon: Icon,
  loading = false,
  idx,
  className,
}: MetricCardProps) {
  const isReducedMotion = typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  return (
    <motion.div
      className={cn(
        "relative overflow-hidden rounded-xl border border-zinc-800/80 bg-zinc-900/30 p-5 transition-all duration-300",
        "before:absolute before:top-0 before:left-[14%] before:right-[14%] before:h-[1px] before:bg-gradient-to-r before:from-transparent before:via-white/10 before:to-transparent before:pointer-events-none",
        "hover:border-[#00D4AA]/20",
        className
      )}
      whileHover={isReducedMotion ? undefined : cardHover}
      whileTap={isReducedMotion ? undefined : cardTap}
      style={{ cursor: "default" }}
    >
      {/* Sheensweep sweep effect on hover */}
      {!isReducedMotion && (
        <span className="absolute inset-0 w-[60%] h-full bg-gradient-to-r from-transparent via-white/5 to-transparent -translate-x-[135%] -skew-x-12 pointer-events-none opacity-0 group-hover:opacity-100 group-hover:animate-sheen" />
      )}

      <div className="flex justify-between items-start mb-3">
        <div className="flex items-center gap-2">
          {Icon && (
            <motion.span
              style={{ color }}
              className="inline-flex"
              whileHover={isReducedMotion ? undefined : { rotate: 12, scale: 1.15 }}
              transition={{ type: "spring", stiffness: 400, damping: 12 }}
            >
              {Icon}
            </motion.span>
          )}
          <span className="text-[10px] font-bold text-zinc-400 tracking-wider uppercase">{label}</span>
        </div>
        
        {delta && !loading && (
          <StatusBadge
            variant={up ? "success" : "destructive"}
            trending={up ? "up" : "down"}
          >
            {delta}
          </StatusBadge>
        )}
      </div>

      {loading ? (
        <div className="h-9 w-2/3 bg-zinc-800/60 rounded animate-pulse mb-3" />
      ) : (
        <SpringMetric
          value={value}
          prefix={prefix}
          suffix={suffix}
          className="text-2xl font-bold tracking-tight text-zinc-100 font-mono"
          stiffness={65}
          damping={16}
        />
      )}

      <div className="mt-3 min-h-[56px] flex flex-col justify-end">
        {loading ? (
          <div className="h-14 w-full bg-zinc-800/40 rounded animate-pulse" />
        ) : mini ? (
          mini
        ) : spark && spark.length > 0 ? (
          <ApexChart {...sparkConfig(color, spark)} />
        ) : null}
      </div>
    </motion.div>
  );
}
