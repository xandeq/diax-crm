'use client';

import { Logo } from '@/components/Logo';
import { useAuth } from '@/contexts/AuthContext';
import { login } from '@/services/auth';
import { zodResolver } from '@hookform/resolvers/zod';
import { motion, type Variants } from 'framer-motion';
import {
  AlertCircle, ArrowRight, BarChart3, Loader2, Lock,
  Mail, ShieldCheck, Sparkles, TrendingUp, Zap,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

const loginSchema = z.object({
  email: z.string().email('Digite um email válido'),
  password: z.string().min(6, 'A senha deve ter no mínimo 6 caracteres'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

const EASE = [0.16, 1, 0.3, 1] as [number, number, number, number];
const stagger: Variants = { hidden: {}, show: { transition: { staggerChildren: 0.09, delayChildren: 0.1 } } };
const rise: Variants = {
  hidden: { opacity: 0, y: 26 },
  show: { opacity: 1, y: 0, transition: { duration: 0.6, ease: EASE } },
};

const CSS = `
  .lg-scene {
    position: fixed; inset: 0; display: flex;
    background: #0B1410; overflow: hidden;
    font-family: var(--font-jakarta, 'Plus Jakarta Sans', sans-serif);
  }
  .lg-aurora { position: absolute; inset: 0; pointer-events: none; }
  .lg-aurora::before {
    content: ''; position: absolute; inset: -20%;
    background:
      radial-gradient(circle at 14% 28%, rgba(16,185,129,0.16), transparent 36%),
      radial-gradient(circle at 78% 14%, rgba(99,102,241,0.13), transparent 40%),
      radial-gradient(circle at 70% 86%, rgba(0,212,170,0.10), transparent 42%),
      radial-gradient(circle at 22% 80%, rgba(244,114,182,0.06), transparent 38%);
    filter: blur(58px);
    animation: lg-flow 24s ease-in-out infinite;
    will-change: transform;
  }
  @keyframes lg-flow {
    0%,100% { transform: translate3d(0,0,0) rotate(0deg) scale(1); }
    33%     { transform: translate3d(2.5%,2%,0) rotate(3deg) scale(1.07); }
    66%     { transform: translate3d(-2.5%,-2%,0) rotate(-3deg) scale(1.03); }
  }
  .lg-grid {
    position: absolute; inset: 0; pointer-events: none; opacity: 0.5;
    background-image:
      linear-gradient(rgba(255,255,255,0.025) 1px, transparent 1px),
      linear-gradient(90deg, rgba(255,255,255,0.025) 1px, transparent 1px);
    background-size: 56px 56px;
    mask-image: radial-gradient(ellipse 75% 65% at 50% 42%, black 25%, transparent 78%);
  }

  .lg-brand {
    flex: 1; display: none; flex-direction: column; justify-content: center;
    padding: 64px 72px; position: relative; z-index: 1; max-width: 620px;
  }
  @media (min-width: 980px) { .lg-brand { display: flex; } }
  .lg-head { font-size: 42px; font-weight: 800; color: #f4f4f5; letter-spacing: -0.035em; line-height: 1.12; margin: 26px 0 14px; }
  .lg-head .hl { color: #10B981; }
  .lg-tag { font-size: 15px; color: #9ba0ab; line-height: 1.6; max-width: 46ch; }
  .lg-feats { display: flex; flex-direction: column; gap: 14px; margin-top: 38px; }
  .lg-feat { display: flex; align-items: center; gap: 12px; font-size: 14px; color: #c6c9d0; font-weight: 500; }
  .lg-feat-ic {
    width: 34px; height: 34px; border-radius: 10px; flex-shrink: 0;
    display: inline-flex; align-items: center; justify-content: center;
    background: rgba(16,185,129,0.12); border: 1px solid rgba(16,185,129,0.22); color: #10B981;
  }

  .lg-side {
    flex: 1; display: flex; align-items: center; justify-content: center;
    padding: 28px 18px; position: relative; z-index: 1; overflow-y: auto;
  }
  .lg-card {
    position: relative; width: 100%; max-width: 420px;
    border-radius: 24px; padding: 38px 34px 30px;
    --lg-angle: 0deg;
    animation: lg-rotate 8s linear infinite;
  }
  @property --lg-angle { syntax: '<angle>'; inherits: false; initial-value: 0deg; }
  @keyframes lg-rotate { to { --lg-angle: 360deg; } }
  .lg-card::before {
    content: ''; position: absolute; inset: -1.5px; border-radius: 25.5px; z-index: 0;
    background: conic-gradient(from var(--lg-angle), transparent 60%, #10B981 74%, #00D4AA 82%, #6366F1 90%, transparent 100%);
    opacity: 0.8;
  }
  .lg-card::after {
    content: ''; position: absolute; inset: 1px; border-radius: 23px; z-index: 0;
    background:
      radial-gradient(ellipse at 80% 0%, rgba(16,185,129,0.07), transparent 55%),
      #0C1712;
  }
  .lg-card > * { position: relative; z-index: 1; }

  .lg-title { font-size: 23px; font-weight: 800; color: #f4f4f5; letter-spacing: -0.02em; text-align: center; }
  .lg-sub { font-size: 13px; color: #9ba0ab; text-align: center; margin-top: 6px; }

  .lg-field { margin-top: 18px; }
  .lg-label { display: flex; justify-content: space-between; align-items: center; font-size: 12px; font-weight: 600; color: #c6c9d0; margin-bottom: 7px; }
  .lg-forgot { font-size: 12px; font-weight: 600; color: #10B981; text-decoration: none; }
  .lg-forgot:hover { text-decoration: underline; }
  .lg-inwrap { position: relative; }
  .lg-inwrap .ic {
    position: absolute; left: 13px; top: 50%; transform: translateY(-50%);
    color: #6e7480; pointer-events: none; transition: color .18s;
  }
  .lg-input {
    width: 100%; height: 48px; padding: 0 14px 0 42px;
    border-radius: 13px; font-size: 14px; color: #f4f4f5;
    background: rgba(255,255,255,0.045);
    border: 1px solid rgba(255,255,255,0.10);
    outline: none;
    transition: border-color .18s, box-shadow .18s, background .18s;
  }
  .lg-input::placeholder { color: #5d636e; }
  .lg-input:hover { border-color: rgba(255,255,255,0.18); }
  .lg-input:focus {
    border-color: rgba(16,185,129,0.65);
    box-shadow: 0 0 0 3px rgba(16,185,129,0.16), 0 0 24px rgba(16,185,129,0.10);
    background: rgba(16,185,129,0.04);
  }
  .lg-inwrap:focus-within .ic { color: #10B981; }
  .lg-input.err { border-color: rgba(239,68,68,0.6); }
  .lg-input.err:focus { box-shadow: 0 0 0 3px rgba(239,68,68,0.14); }
  .lg-error { display: flex; align-items: center; gap: 5px; font-size: 12px; color: #f87171; margin-top: 6px; }

  .lg-server {
    display: flex; align-items: center; gap: 8px; margin-top: 16px;
    padding: 11px 13px; border-radius: 12px; font-size: 13px; color: #fca5a5;
    background: rgba(239,68,68,0.09); border: 1px solid rgba(239,68,68,0.22);
  }

  .lg-btn {
    position: relative; overflow: hidden; isolation: isolate;
    width: 100%; height: 50px; margin-top: 22px;
    border: none; border-radius: 13px; cursor: pointer;
    display: inline-flex; align-items: center; justify-content: center; gap: 8px;
    font-size: 15px; font-weight: 700; color: #04110B;
    font-family: inherit;
    background: linear-gradient(118deg, #10B981, #00D4AA 55%, #34d399);
    background-size: 160% 160%;
    box-shadow: 0 8px 26px rgba(16,185,129,0.30), inset 0 1px 0 rgba(255,255,255,0.25);
    transition: transform .18s cubic-bezier(.22,1,.36,1), box-shadow .18s, background-position .4s;
  }
  .lg-btn::after {
    content: ''; position: absolute; top: 0; left: 0; width: 55%; height: 100%;
    background: linear-gradient(100deg, transparent, rgba(255,255,255,0.35), transparent);
    transform: translateX(-150%) skewX(-18deg); opacity: 0; pointer-events: none;
  }
  @keyframes lg-sheen { to { transform: translateX(280%) skewX(-18deg); } }
  @media (hover: hover) {
    .lg-btn:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 14px 38px rgba(16,185,129,0.42), inset 0 1px 0 rgba(255,255,255,0.3);
      background-position: 100% 50%;
    }
    .lg-btn:hover:not(:disabled)::after { opacity: 1; animation: lg-sheen .85s cubic-bezier(.22,1,.36,1); }
    .lg-btn:hover:not(:disabled) .lg-arrow { transform: translateX(3px); }
  }
  .lg-btn:active:not(:disabled) { transform: translateY(0) scale(0.985); }
  .lg-btn:disabled { opacity: 0.65; cursor: not-allowed; }
  .lg-btn:focus-visible { outline: 2px solid #10B981; outline-offset: 3px; }
  .lg-arrow { transition: transform .2s cubic-bezier(.22,1,.36,1); }

  .lg-foot { margin-top: 22px; text-align: center; font-size: 13px; color: #9ba0ab; }
  .lg-foot a { color: #10B981; font-weight: 600; text-decoration: none; }
  .lg-foot a:hover { text-decoration: underline; }
  .lg-secure {
    display: flex; align-items: center; justify-content: center; gap: 6px;
    margin-top: 18px; font-size: 11px; color: #6e7480;
  }

  @media (max-width: 979px) { .lg-card { padding: 32px 24px 26px; } }
  @media (prefers-reduced-motion: reduce) {
    .lg-aurora::before, .lg-card, .lg-btn::after { animation: none !important; }
    .lg-btn, .lg-input, .lg-arrow { transition: none !important; }
  }
`;

const FEATURES = [
  { icon: <TrendingUp size={16} />, text: 'Pipeline de leads e conversão em tempo real' },
  { icon: <BarChart3 size={16} />, text: 'Finanças, investimentos e ads em um só painel' },
  { icon: <Zap size={16} />, text: 'Automação de email e ferramentas de IA' },
];

export default function LoginPage() {
  const router = useRouter();
  const { login: authLogin } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  async function onSubmit(data: LoginFormValues) {
    setIsLoading(true);
    setServerError(null);
    try {
      const response = await login(data.email, data.password);
      if (!response?.accessToken) {
        throw new Error('Token não retornado pelo login.');
      }
      authLogin(response.accessToken);
      router.push('/dashboard/');
    } catch (err) {
      setServerError(err instanceof Error ? err.message : 'Ocorreu um erro inesperado. Tente novamente.');
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <>
      <style>{CSS}</style>
      <div className="lg-scene">
        <div className="lg-aurora" aria-hidden="true" />
        <div className="lg-grid" aria-hidden="true" />

        {/* Painel de marca (desktop) */}
        <motion.div className="lg-brand" variants={stagger} initial="hidden" animate="show">
          <motion.div variants={rise}><Logo variant="full" width={170} height={46} /></motion.div>
          <motion.h1 className="lg-head" variants={rise}>
            Seu negócio inteiro,<br />em <span className="hl">uma tela</span>.
          </motion.h1>
          <motion.p className="lg-tag" variants={rise}>
            Leads, vendas, finanças, investimentos e marketing reunidos num
            painel de decisão que mostra o que fazer agora.
          </motion.p>
          <motion.div className="lg-feats" variants={stagger}>
            {FEATURES.map(f => (
              <motion.div key={f.text} className="lg-feat" variants={rise}>
                <span className="lg-feat-ic">{f.icon}</span>{f.text}
              </motion.div>
            ))}
          </motion.div>
        </motion.div>

        {/* Painel do formulário */}
        <div className="lg-side">
          <motion.div
            className="lg-card"
            initial={{ opacity: 0, y: 30, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            transition={{ duration: 0.65, ease: EASE, delay: 0.15 }}
          >
            <motion.div variants={stagger} initial="hidden" animate="show">
              <motion.div variants={rise} style={{ display: 'flex', justifyContent: 'center', marginBottom: 18 }}>
                <Logo variant="icon" width={56} height={56} />
              </motion.div>
              <motion.div variants={rise}>
                <div className="lg-title">Bem-vindo de volta</div>
                <div className="lg-sub">Entre com suas credenciais para acessar o painel</div>
              </motion.div>

              <form onSubmit={handleSubmit(onSubmit)} noValidate>
                <motion.div className="lg-field" variants={rise}>
                  <label className="lg-label" htmlFor="email">Email</label>
                  <div className="lg-inwrap">
                    <Mail size={16} className="ic" />
                    <input
                      id="email"
                      type="email"
                      placeholder="seu@email.com"
                      autoComplete="email"
                      disabled={isLoading}
                      className={`lg-input ${errors.email ? 'err' : ''}`}
                      {...register('email')}
                    />
                  </div>
                  {errors.email && (
                    <p className="lg-error"><AlertCircle size={12} /> {errors.email.message}</p>
                  )}
                </motion.div>

                <motion.div className="lg-field" variants={rise}>
                  <div className="lg-label">
                    <label htmlFor="password">Senha</label>
                    <a href="#" className="lg-forgot">Esqueceu a senha?</a>
                  </div>
                  <div className="lg-inwrap">
                    <Lock size={16} className="ic" />
                    <input
                      id="password"
                      type="password"
                      placeholder="••••••••"
                      autoComplete="current-password"
                      disabled={isLoading}
                      className={`lg-input ${errors.password ? 'err' : ''}`}
                      {...register('password')}
                    />
                  </div>
                  {errors.password && (
                    <p className="lg-error"><AlertCircle size={12} /> {errors.password.message}</p>
                  )}
                </motion.div>

                {serverError && (
                  <motion.div className="lg-server bg-destructive/10" initial={{ opacity: 0, y: -6 }} animate={{ opacity: 1, y: 0 }}>
                    <AlertCircle size={15} style={{ flexShrink: 0 }} />
                    {serverError}
                  </motion.div>
                )}

                <motion.div variants={rise}>
                  <button type="submit" className="lg-btn" disabled={isLoading}>
                    {isLoading ? (
                      <><Loader2 size={17} className="animate-spin" /> Entrando...</>
                    ) : (
                      <>Entrar <ArrowRight size={17} className="lg-arrow" /></>
                    )}
                  </button>
                </motion.div>
              </form>

              <motion.div className="lg-foot" variants={rise}>
                Não tem uma conta? <a href="#">Criar conta</a>
              </motion.div>
              <motion.div className="lg-secure" variants={rise}>
                <ShieldCheck size={12} /> Conexão segura · acesso restrito
                <Sparkles size={11} style={{ color: '#10B981' }} />
              </motion.div>
            </motion.div>
          </motion.div>
        </div>
      </div>
    </>
  );
}
