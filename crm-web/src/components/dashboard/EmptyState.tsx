import { cn } from "@/lib/utils";
import { LucideIcon } from "lucide-react";
import { motion } from "framer-motion";
import * as React from "react";
import { Button } from "@/components/ui/button";

export interface EmptyStateProps extends React.HTMLAttributes<HTMLDivElement> {
  title: string;
  description: string;
  icon: LucideIcon;
  actionLabel?: string;
  onAction?: () => void;
  actionHref?: string;
  loading?: boolean;
}

export const EmptyState = React.forwardRef<HTMLDivElement, EmptyStateProps>(
  ({ className, title, description, icon: Icon, actionLabel, onAction, actionHref, loading, ...props }, ref) => {
    const content = (
      <div className="flex flex-col items-center justify-center text-center p-8 border border-dashed border-zinc-800 rounded-2xl bg-zinc-900/10 min-h-[220px]">
        <motion.div
          initial={{ scale: 0.8, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          transition={{ type: "spring", stiffness: 300, damping: 20 }}
          className="relative flex items-center justify-center w-12 h-12 rounded-full bg-emerald-500/10 text-emerald-400 mb-4 border border-emerald-500/20"
        >
          <Icon className="h-6 w-6" />
          <span className="absolute inset-0 rounded-full bg-emerald-500/5 blur-sm" />
        </motion.div>
        
        <h3 className="text-sm font-semibold text-zinc-100 tracking-tight mb-1">{title}</h3>
        <p className="text-xs text-zinc-400 max-w-[280px] leading-relaxed mb-4">{description}</p>
        
        {actionLabel && (onAction || actionHref) && (
          <Button
            size="sm"
            onClick={onAction}
            className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold text-xs transition-all duration-300"
            asChild={!!actionHref}
          >
            {actionHref ? <a href={actionHref}>{actionLabel}</a> : actionLabel}
          </Button>
        )}
      </div>
    );

    return (
      <div ref={ref} className={cn("w-full", className)} {...props}>
        {content}
      </div>
    );
  }
);

EmptyState.displayName = "EmptyState";
