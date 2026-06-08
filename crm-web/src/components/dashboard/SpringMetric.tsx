'use client';

import { useEffect, useRef } from 'react';
import { motion, useMotionValue, useSpring, useTransform } from 'framer-motion';

interface Props {
  value: number;
  prefix?: string;
  suffix?: string;
  className?: string;
  style?: React.CSSProperties;
  decimals?: number;
  stiffness?: number;
  damping?: number;
}

export function SpringMetric({
  value,
  prefix = '',
  suffix = '',
  className,
  style,
  decimals = 0,
  stiffness = 55,
  damping = 14,
}: Props) {
  const mv = useMotionValue(0);
  // Low damping = slight weight/oscillation before settling (expressive feel)
  const spring = useSpring(mv, { stiffness, damping, restDelta: 0.01 });
  const display = useTransform(spring, (v) => {
    const n = Math.max(0, parseFloat(v.toFixed(decimals)));
    const formatted = decimals > 0
      ? n.toLocaleString('pt-BR', { minimumFractionDigits: decimals, maximumFractionDigits: decimals })
      : Math.round(n).toLocaleString('pt-BR');
    return `${prefix}${formatted}${suffix}`;
  });

  const prevRef = useRef(0);

  useEffect(() => {
    // On first mount, animate from 0
    if (prevRef.current === 0 && value > 0) {
      mv.set(0);
      // Small delay so the element is visible before counting
      const t = setTimeout(() => mv.set(value), 180);
      prevRef.current = value;
      return () => clearTimeout(t);
    }
    // On update, spring from current value
    mv.set(value);
    prevRef.current = value;
  }, [value, mv]);

  return <motion.span className={className} style={style}>{display}</motion.span>;
}
