import * as React from "react";
import { useCallback, useEffect, useState, useRef } from "react";
import { motion, useMotionValue, useSpring, useTransform, AnimatePresence } from "framer-motion";
import { ArrowDownRight, ArrowUpRight, DollarSign, PiggyBank, ShieldAlert, Baby, Home, Plane, TrendingUp, Landmark, Target, Sparkles, Wallet, ArrowRight } from "lucide-react";
import Link from "next/link";
import dynamic from "next/dynamic";
import { HeroParticles } from "./HeroParticles";
import { SpringMetric } from "./SpringMetric";
import { StatusBadge } from "./StatusBadge";
import { cn } from "@/lib/utils";

const ApexChart = dynamic(() => import("react-apexcharts"), { ssr: false });

const C = {
  primary: "#00D4AA",
  success: "#22C55E",
  loss: "#EF4444",
  warn: "#F59E0B",
  info: "#818CF8",
  accent: "#F472B6",
  text: "#f4f4f5",
  text2: "#b0b0ba",
  text3: "#8e8e99",
  muted: "#6e6e7a",
  grid: "rgba(255,255,255,0.045)",
};

const R = (v: number) => v.toLocaleString("pt-BR", { style: "currency", currency: "BRL", minimumFractionDigits: 0, maximumFractionDigits: 0 });
const S = (v: number) => Math.abs(v) >= 1_000_000 ? `R$ ${(v / 1_000_000).toFixed(1)}M` : Math.abs(v) >= 1_000 ? `R$ ${(v / 1_000).toFixed(1)}k` : R(v);

const EASE_OUT = [0.16, 1, 0.3, 1] as [number, number, number, number];

// Goal categories visual metadata
const GOAL_META = {
  emergency: { icon: <ShieldAlert className="h-4.5 w-4.5" />, color: C.loss, label: "Emergência" },
  baby: { icon: <Baby className="h-4.5 w-4.5" />, color: C.accent, label: "Bebê" },
  house: { icon: <Home className="h-4.5 w-4.5" />, color: C.info, label: "Casa" },
  travel: { icon: <Plane className="h-4.5 w-4.5" />, color: C.primary, label: "Viagem" },
  investment: { icon: <TrendingUp className="h-4.5 w-4.5" />, color: C.success, label: "Investimento" },
  debt: { icon: <Landmark className="h-4.5 w-4.5" />, color: C.warn, label: "Dívida" },
  other: { icon: <Target className="h-4.5 w-4.5" />, color: C.text2, label: "Outro" },
};

const animCfg = { enabled: true, easing: "easeinout" as const, speed: 1100, animateGradually: { enabled: true, delay: 120 }, dynamicAnimation: { enabled: true, speed: 500 } };

function heroChartConfig(trend: { label: string; income: number; expense: number }[]) {
  return {
    type: "area" as const,
    height: 132,
    series: [{ name: "Receita", data: trend.map(t => Math.round(t.income)) }],
    options: {
      chart: { sparkline: { enabled: true }, toolbar: { show: false }, background: "transparent", animations: { ...animCfg, speed: 1400 } },
      stroke: { curve: "smooth" as const, width: 3 },
      fill: { type: "gradient", gradient: { opacityFrom: 0.34, opacityTo: 0 } },
      colors: [C.primary],
      tooltip: { enabled: true, theme: "dark" as const, y: { formatter: (v: number) => R(v) } },
      xaxis: { categories: trend.map(t => t.label) },
    },
  };
}

function AnimatedRing({ pct, color, size = 116, stroke = 9, label, value, center }: { pct: number; color: string; size?: number; stroke?: number; label?: string; value?: string; center?: React.ReactNode }) {
  const r = (size - stroke - 3) / 2;
  const circ = 2 * Math.PI * r;
  const dash = useSpring(circ, { stiffness: 60, damping: 18 });
  const ref = useRef<SVGSVGElement>(null);
  
  useEffect(() => {
    const el = ref.current; if (!el) return;
    const io = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return; io.disconnect();
      setTimeout(() => dash.set(circ * (1 - Math.min(Math.max(pct, 0), 100) / 100)), 120);
    }, { threshold: 0.3 });
    io.observe(el); return () => io.disconnect();
  }, [pct, circ, dash]);

  return (
    <div className="relative shrink-0" style={{ width: size, height: size }}>
      <svg ref={ref} width={size} height={size} style={{ transform: "rotate(-90deg)" }}>
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke="rgba(255,255,255,0.07)" strokeWidth={stroke} />
        <motion.circle
          cx={size / 2} cy={size / 2} r={r} fill="none" stroke={color} strokeWidth={stroke}
          strokeLinecap="round" strokeDasharray={circ} style={{ strokeDashoffset: dash, filter: `drop-shadow(0 0 6px ${color}80)` }}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center gap-0.5">
        {center ?? (
          <>
            <span className="font-mono text-xl font-bold tracking-tight" style={{ color }}>{value}</span>
            <span className="text-[9px] text-zinc-500 font-bold uppercase tracking-wider">{label}</span>
          </>
        )}
      </div>
    </div>
  );
}

export interface DashboardHeroProps {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  data: any;
  loading: boolean;
  mounted: boolean;
}

export function DashboardHero({ data, loading, mounted }: DashboardHeroProps) {
  const [fx, setFx] = useState(false);
  
  useEffect(() => {
    setFx(
      window.matchMedia("(hover: hover) and (pointer: fine)").matches &&
      !window.matchMedia("(prefers-reduced-motion: reduce)").matches
    );
  }, []);

  const mx = useMotionValue(0);
  const my = useMotionValue(0);
  const sx = useSpring(mx, { stiffness: 60, damping: 18 });
  const sy = useSpring(my, { stiffness: 60, damping: 18 });
  const rotX = useTransform(sy, [-0.5, 0.5], [1.6, -1.6]);
  const rotY = useTransform(sx, [-0.5, 0.5], [-1.6, 1.6]);
  const gridX = useTransform(sx, [-0.5, 0.5], [10, -10]);
  const gridY = useTransform(sy, [-0.5, 0.5], [7, -7]);
  
  const onMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    const r = e.currentTarget.getBoundingClientRect();
    mx.set((e.clientX - r.left) / r.width - 0.5);
    my.set((e.clientY - r.top) / r.height - 0.5);
  }, [mx, my]);
  
  const onLeave = useCallback(() => { mx.set(0); my.set(0); }, [mx, my]);

  const s = data?.curr?.summary;
  const income = s?.totalIncome ?? 0;
  const prev = data?.prev?.summary?.totalIncome ?? 0;
  const mom = prev > 0 ? ((income - prev) / prev) * 100 : 0;
  const cashFlow = s?.remainingBalance ?? 0;
  const totalExpenses = s?.totalExpenses ?? 0;
  const available = s?.availableToInvest ?? 0;
  const goalRef = prev;
  const goalPct = goalRef > 0 ? Math.min((income / goalRef) * 100, 100) : 0;
  const goalLabel = "vs mês ant.";
  const goalColor = goalPct >= 80 ? C.primary : goalPct >= 50 ? C.warn : C.loss;
  const heroCfg = mounted && data?.trend?.length ? heroChartConfig(data.trend) : null;

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5, ease: EASE_OUT }}
      className="hero-card relative overflow-hidden rounded-3xl border border-zinc-800 bg-zinc-950/45 p-6 md:p-8 hover:border-[#00D4AA]/25 transition-colors duration-300 group"
      onMouseMove={fx ? onMove : undefined} 
      onMouseLeave={fx ? onLeave : undefined}
    >
      {/* Mesh Glow backdrop (Aurora) */}
      <div className="absolute inset-0 z-0 pointer-events-none overflow-hidden rounded-3xl">
        <div className="absolute inset-[-10%] bg-radial-[circle_at_18%_22%] from-[#00D4AA]/10 via-transparent to-transparent opacity-80 blur-3xl animate-[pulse_10s_ease-in-out_infinite]" />
        <div className="absolute inset-[-10%] bg-radial-[circle_at_82%_16%] from-[#818CF8]/8 via-transparent to-transparent opacity-60 blur-3xl animate-[pulse_12s_ease-in-out_infinite_delay-1000]" />
      </div>

      <motion.div 
        className="absolute inset-0 z-0 pointer-events-none opacity-40 bg-[linear-gradient(rgba(0,212,170,0.03)_1px,transparent_1px),linear-gradient(90deg,rgba(0,212,170,0.03)_1px,transparent_1px)] bg-[size:44px_44px] [mask-image:radial-gradient(ellipse_80%_90%_at_72%_28%,black_0%,transparent_74%)]" 
        aria-hidden="true" 
        style={fx ? { x: gridX, y: gridY } : undefined} 
      />

      <div className="absolute w-[320px] h-[320px] rounded-full top-[-130px] right-[-70px] pointer-events-none bg-radial-[circle] from-[#00D4AA]/10 via-transparent to-transparent blur-3xl" aria-hidden="true" />
      
      <HeroParticles />
      
      <motion.div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-2 gap-8 items-center"
        style={fx ? { rotateX: rotX, rotateY: rotY, transformPerspective: 1100 } : {}}
      >
        {/* Left Side: Real Metrics */}
        <div className="space-y-6">
          <div>
            <div className="flex items-center gap-2 mb-2">
              <span className="text-[10px] font-bold text-zinc-500 tracking-wider uppercase">Receita do Mês</span>
              <span className="live-dot relative flex h-1.5 w-1.5">
                <span className="absolute inline-flex h-full w-full rounded-full bg-[#00D4AA] opacity-75 animate-ping" />
                <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-[#00D4AA]" />
              </span>
              <span className="text-[10px] text-[#00D4AA] font-bold tracking-widest">AO VIVO</span>
            </div>
            
            <AnimatePresence mode="wait">
              {loading ? (
                <div className="h-14 w-2/3 bg-zinc-800/60 rounded animate-pulse" />
              ) : (
                <motion.div
                  key="metric"
                  initial={{ opacity: 0, scale: 0.95, y: 8 }}
                  animate={{ opacity: 1, scale: 1, y: 0 }}
                  transition={{ type: "spring", stiffness: 280, damping: 20 }}
                >
                  <SpringMetric
                    value={income}
                    prefix="R$ "
                    className="metric-huge text-4xl md:text-5xl font-black tracking-tight text-[#00D4AA] drop-shadow-[0_0_30px_rgba(0,212,170,0.25)] font-mono"
                    stiffness={55}
                    damping={13}
                  />
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          <div className="flex items-center gap-3 flex-wrap">
            {!loading && (
              <StatusBadge
                variant={mom >= 0 ? "success" : "destructive"}
                trending={mom >= 0 ? "up" : "down"}
              >
                {Math.abs(mom).toFixed(1)}% vs mês ant.
              </StatusBadge>
            )}
            {!loading && (
              <StatusBadge variant={cashFlow >= 0 ? "primary" : "destructive"}>
                Fluxo: {S(cashFlow)}
              </StatusBadge>
            )}
          </div>

          <div className="flex items-center gap-6 pt-2">
            {!loading && goalRef > 0 && (
              <AnimatedRing pct={goalPct} color={goalColor} value={`${goalPct.toFixed(0)}%`} label={goalLabel} />
            )}
            
            <div className="flex-1 space-y-4">
              {loading ? (
                <div className="space-y-2">
                  <div className="h-10 bg-zinc-800/40 rounded animate-pulse" />
                  <div className="h-10 bg-zinc-800/40 rounded animate-pulse" />
                </div>
              ) : (
                [
                  { lbl: "Despesas do mês", val: totalExpenses, c: C.warn, icon: <Wallet className="h-4 w-4" /> },
                  { lbl: "Saldo líquido", val: cashFlow, c: cashFlow >= 0 ? C.primary : C.loss, icon: <PiggyBank className="h-4 w-4" /> },
                ].map((m, i) => (
                  <motion.div
                    key={m.lbl}
                    initial={{ opacity: 0, x: 12 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: 0.2 + i * 0.08, duration: 0.4, ease: EASE_OUT }}
                  >
                    <div className="text-[10px] font-bold text-zinc-500 tracking-wider uppercase flex items-center gap-1.5 mb-1">
                      <span style={{ color: m.c }} className="inline-flex">{m.icon}</span>
                      {m.lbl}
                    </div>
                    <SpringMetric
                      value={Math.abs(m.val)}
                      prefix="R$ "
                      suffix={m.val < 0 ? " ↓" : ""}
                      className="text-lg font-bold font-mono tracking-tight"
                      style={{ color: m.c } as React.CSSProperties}
                      stiffness={70}
                      damping={16}
                    />
                  </motion.div>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Right Side: Graph details */}
        <div className="space-y-4 lg:border-l lg:border-zinc-800/80 lg:pl-8">
          <div className="flex justify-between items-center">
            <span className="text-[10px] font-bold text-zinc-500 tracking-wider uppercase flex items-center gap-1.5">
              <TrendingUp className="h-4 w-4 text-[#00D4AA]" />
              Tendência — 6 meses
            </span>
            <Link href="/finance" className="text-xs font-semibold text-[#00D4AA] hover:text-[#00B894] hover:underline flex items-center gap-1 transition-colors">
              Detalhes <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </div>

          {loading || !heroCfg ? (
            <div className="h-[132px] w-full bg-zinc-855/40 rounded animate-pulse" />
          ) : (
            <ApexChart {...heroCfg} />
          )}

          <hr className="border-zinc-800/60 my-4" />
          
          <div className="grid grid-cols-2 gap-4">
            {loading ? (
              <>
                <div className="h-10 bg-zinc-800/40 rounded animate-pulse" />
                <div className="h-10 bg-zinc-800/40 rounded animate-pulse" />
              </>
            ) : (
              [
                { lbl: "Recebido", val: income, c: C.primary },
                { lbl: "A investir", val: available, c: C.info },
              ].map((m, i) => (
                <motion.div
                  key={m.lbl}
                  initial={{ opacity: 0, x: 10 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.3 + i * 0.06, duration: 0.35, ease: EASE_OUT }}
                >
                  <div className="text-[10px] font-bold text-zinc-500 tracking-wider uppercase mb-1">{m.lbl}</div>
                  <SpringMetric
                    value={Math.abs(m.val)}
                    prefix="R$ "
                    className="text-lg font-bold font-mono tracking-tight"
                    style={{ color: m.c } as React.CSSProperties}
                    stiffness={70}
                    damping={16}
                  />
                </motion.div>
              ))
            )}
          </div>
        </div>
      </motion.div>
    </motion.div>
  );
}
