import { cn } from "@/lib/utils";
import { ArrowDownRight, ArrowUpRight, LucideIcon } from "lucide-react";
import * as React from "react";

export type StatusBadgeVariant = "success" | "destructive" | "warning" | "info" | "primary" | "neutral";

export interface StatusBadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: StatusBadgeVariant;
  trending?: "up" | "down";
  icon?: LucideIcon;
  pulse?: boolean;
}

export const StatusBadge = React.forwardRef<HTMLSpanElement, StatusBadgeProps>(
  ({ className, variant = "neutral", trending, icon: Icon, pulse, children, ...props }, ref) => {
    // Style configurations matching the dark-teal palette
    const variants = {
      success: "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20",
      destructive: "bg-red-500/10 text-red-400 border border-red-500/20",
      warning: "bg-amber-500/10 text-amber-400 border border-amber-500/20",
      info: "bg-indigo-500/10 text-indigo-400 border border-indigo-500/20",
      primary: "bg-[#00D4AA]/10 text-[#00D4AA] border border-[#00D4AA]/20",
      neutral: "bg-zinc-800/50 text-zinc-300 border border-zinc-700/50",
    };

    return (
      <span
        ref={ref}
        className={cn(
          "inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-[11px] font-semibold tracking-wide transition-all duration-300",
          variants[variant],
          className
        )}
        {...props}
      >
        {pulse && (
          <span className="relative flex h-1.5 w-1.5">
            <span className={cn(
              "absolute inline-flex h-full w-full rounded-full opacity-75 animate-ping",
              variant === "success" && "bg-emerald-400",
              variant === "destructive" && "bg-red-400",
              variant === "warning" && "bg-amber-400",
              variant === "info" && "bg-indigo-400",
              variant === "primary" && "bg-[#00D4AA]",
              variant === "neutral" && "bg-zinc-400"
            )} />
            <span className={cn(
              "relative inline-flex rounded-full h-1.5 w-1.5",
              variant === "success" && "bg-emerald-500",
              variant === "destructive" && "bg-red-500",
              variant === "warning" && "bg-amber-500",
              variant === "info" && "bg-indigo-500",
              variant === "primary" && "bg-[#00D4AA]",
              variant === "neutral" && "bg-zinc-500"
            )} />
          </span>
        )}
        {trending === "up" && <ArrowUpRight className="h-3 w-3 shrink-0" />}
        {trending === "down" && <ArrowDownRight className="h-3 w-3 shrink-0" />}
        {Icon && !trending && <Icon className="h-3 w-3 shrink-0" />}
        {children}
      </span>
    );
  }
);

StatusBadge.displayName = "StatusBadge";
