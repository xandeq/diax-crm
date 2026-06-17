import * as React from "react";
import { motion, Variants, HTMLMotionProps } from "framer-motion";
import { cn } from "@/lib/utils";

const EASE_OUT = [0.16, 1, 0.3, 1] as [number, number, number, number];

const sectionVariants: Variants = {
  hidden: { opacity: 0, y: 24 },
  show: { opacity: 1, y: 0, transition: { duration: 0.5, ease: EASE_OUT } },
};

export interface DashboardSectionProps extends HTMLMotionProps<"div"> {
  children: React.ReactNode;
  delay?: number;
}

export function DashboardSection({ children, className, delay = 0, ...props }: DashboardSectionProps) {
  const isReducedMotion = typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  return (
    <motion.div
      variants={isReducedMotion ? undefined : sectionVariants}
      initial={isReducedMotion ? undefined : "hidden"}
      whileInView={isReducedMotion ? undefined : "show"}
      viewport={{ once: true, margin: "-100px" }}
      className={cn("w-full", className)}
      style={delay ? { transitionDelay: `${delay}ms` } : undefined}
      {...props}
    >
      {children}
    </motion.div>
  );
}
