import * as React from "react";
import { motion, Variants, HTMLMotionProps } from "framer-motion";
import { cn } from "@/lib/utils";

const pageVariants: Variants = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.06, delayChildren: 0.04 } },
};

export interface AnimatedContainerProps extends HTMLMotionProps<"div"> {
  children: React.ReactNode;
}

export function AnimatedContainer({ children, className, ...props }: AnimatedContainerProps) {
  const isReducedMotion = typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  return (
    <motion.div
      variants={isReducedMotion ? undefined : pageVariants}
      initial={isReducedMotion ? undefined : "hidden"}
      animate={isReducedMotion ? undefined : "show"}
      className={cn("w-full", className)}
      {...props}
    >
      {children}
    </motion.div>
  );
}
