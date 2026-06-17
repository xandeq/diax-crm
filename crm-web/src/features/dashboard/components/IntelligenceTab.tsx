'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { useBriefingDashboard } from '../hooks/useBriefingDashboard';
import { LoadingSkeleton } from './LoadingSkeleton';
import { ErrorState } from './ErrorState';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';
import { 
  Sparkles, Newspaper, Lightbulb, Copy, Check, MessageSquare, 
  Mail, Calendar, ArrowRight, Star, AlertCircle 
} from 'lucide-react';
import Link from 'next/link';

export function IntelligenceTab() {
  const { data, isLoading, isError, error, refetch } = useBriefingDashboard();
  const [copiedKey, setCopiedKey] = useState<string | null>(null);

  useEffect(() => {
    const handleSync = () => refetch();
    window.addEventListener('dashboard-sync', handleSync);
    return () => window.removeEventListener('dashboard-sync', handleSync);
  }, [refetch]);

  const copyToClipboard = (text: string, key: string) => {
    navigator.clipboard.writeText(text);
    setCopiedKey(key);
    toast.success('Copiado para a área de transferência!');
    setTimeout(() => setCopiedKey(null), 2000);
  };

  if (isLoading) {
    return <LoadingSkeleton rows={4} />;
  }

  if (isError) {
    return <ErrorState message={error instanceof Error ? error.message : undefined} onRetry={refetch} />;
  }

  if (!data) return null;

  const { cards, activeBriefing, extracted } = data;

  return (
    <div className="space-y-6">
      {/* Upper header */}
      <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <div className="flex items-center gap-2">
            <Sparkles className="h-5 w-5 text-teal-400" />
            <h3 className="text-base font-bold text-zinc-500 uppercase tracking-wider">Briefing Engine</h3>
          </div>
          <h2 className="text-xl font-extrabold text-zinc-100 mt-1">
            {activeBriefing?.title || 'Briefing Diário por Claude AI'}
          </h2>
          <div className="text-xs text-zinc-500 font-semibold uppercase tracking-wider mt-1 flex items-center gap-1.5">
            <Calendar className="h-3.5 w-3.5" />
            <span>Gerado em: {activeBriefing?.createdAt ? new Date(activeBriefing.createdAt).toLocaleDateString('pt-BR') : 'Hoje'}</span>
          </div>
        </div>

        <Button asChild size="sm" className="h-9 bg-teal-500 hover:bg-teal-600 text-zinc-950 font-bold gap-1.5 border-none shadow-[0_4px_15px_rgba(20,184,166,0.25)]">
          <Link href="/daily-briefings">Histórico de Briefings</Link>
        </Button>
      </div>

      {/* Main HTML render container */}
      {activeBriefing ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* TL;DR & Core sections */}
          <div className="md:col-span-2 space-y-6">
            {/* TL;DR */}
            <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
              <div className="flex items-center gap-2 mb-4">
                <Star className="h-4.5 w-4.5 text-teal-400" />
                <h3 className="text-sm font-bold text-zinc-100">Resumo Executivo (TL;DR)</h3>
              </div>
              <div 
                className="text-xs text-zinc-300 leading-relaxed space-y-2 prose prose-invert max-w-none prose-xs" 
                dangerouslySetInnerHTML={{ __html: extracted.tlDr }}
              />
            </div>

            {/* News and Updates */}
            <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
              <div className="flex items-center gap-2 mb-4">
                <Newspaper className="h-4.5 w-4.5 text-blue-400" />
                <h3 className="text-sm font-bold text-zinc-100">Novidades & Notícias Importantes</h3>
              </div>
              <div 
                className="text-xs text-zinc-300 leading-relaxed space-y-2 prose prose-invert max-w-none prose-xs" 
                dangerouslySetInnerHTML={{ __html: extracted.news }}
              />
            </div>

            {/* Monetization Ideas */}
            <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
              <div className="flex items-center gap-2 mb-4">
                <Lightbulb className="h-4.5 w-4.5 text-amber-400" />
                <h3 className="text-sm font-bold text-zinc-100">Ideias Monetizáveis & Dicas Práticas</h3>
              </div>
              <div 
                className="text-xs text-zinc-300 leading-relaxed space-y-2 prose prose-invert max-w-none prose-xs" 
                dangerouslySetInnerHTML={{ __html: extracted.ideas }}
              />
            </div>
          </div>

          {/* Copy Writing sections */}
          <div className="space-y-6">
            {/* WhatsApp Copy */}
            <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
              <div>
                <div className="flex justify-between items-center mb-4">
                  <div className="flex items-center gap-2">
                    <MessageSquare className="h-4.5 w-4.5 text-emerald-400" />
                    <h3 className="text-sm font-bold text-zinc-100">WhatsApp Copy</h3>
                  </div>
                  <Button 
                    size="icon" 
                    variant="ghost" 
                    className="h-8 w-8 text-zinc-500 hover:text-zinc-300"
                    onClick={() => copyToClipboard(extracted.whatsAppCopy, 'wa')}
                  >
                    {copiedKey === 'wa' ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
                  </Button>
                </div>
                <div className="bg-zinc-900/40 p-3 rounded-lg border border-zinc-850/60 font-mono text-[10px] text-zinc-300 leading-relaxed whitespace-pre-wrap max-h-56 overflow-y-auto scrollbar-none">
                  {extracted.whatsAppCopy}
                </div>
              </div>
            </div>

            {/* Email Copy */}
            <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
              <div>
                <div className="flex justify-between items-center mb-4">
                  <div className="flex items-center gap-2">
                    <Mail className="h-4.5 w-4.5 text-pink-400" />
                    <h3 className="text-sm font-bold text-zinc-100">Email Copy</h3>
                  </div>
                  <Button 
                    size="icon" 
                    variant="ghost" 
                    className="h-8 w-8 text-zinc-500 hover:text-zinc-300"
                    onClick={() => copyToClipboard(extracted.emailCopy, 'email')}
                  >
                    {copiedKey === 'email' ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
                  </Button>
                </div>
                <div className="bg-zinc-900/40 p-3 rounded-lg border border-zinc-850/60 font-mono text-[10px] text-zinc-300 leading-relaxed whitespace-pre-wrap max-h-56 overflow-y-auto scrollbar-none">
                  {extracted.emailCopy}
                </div>
              </div>
            </div>
          </div>
        </div>
      ) : (
        <div className="p-12 text-center bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <AlertCircle className="h-8 w-8 text-zinc-500 mx-auto mb-2" />
          <h3 className="text-sm font-bold text-zinc-400">Nenhum briefing diário encontrado</h3>
          <p className="text-xs text-zinc-500 max-w-sm mx-auto mt-1 leading-normal">
            Rode o script Python `claude-ai-briefing-generate.py` localmente para alimentar o CRM com as novidades da IA.
          </p>
        </div>
      )}
    </div>
  );
}
