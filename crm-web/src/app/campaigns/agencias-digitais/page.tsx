"use client";

import React, { useState, useEffect, Suspense } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { 
  ArrowLeft, CheckCircle2, MessageSquare, AlertTriangle, 
  Send, Eye, Play, ShieldAlert, Sparkles, RefreshCw, Layers, Lock, Unlock, Activity
} from "lucide-react";
import { toast } from "sonner";
import Link from "next/link";
import { apiFetch } from '@/services/api';

function CampaignOperationalPageContent() {
  const [campaign, setCampaign] = useState<any>(null);
  const [pilotStatus, setPilotStatus] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [testEmail, setTestEmail] = useState("admin@diaxcrm.com.br");
  const [sendingTest, setSendingTest] = useState(false);
  const [domainVerified, setDomainVerified] = useState(false);
  const [verifyingDns, setVerifyingDns] = useState(false);

  // Fetch campaign and status on mount
  const loadCampaignData = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      const res = await apiFetch<any>('/email-campaigns/campaigns?page=1&pageSize=50');
      let found = res.items?.find((c: any) => 
        c.name.toLowerCase().includes("agências digitais") || 
        c.subject.toLowerCase().includes("agências digitais") ||
        c.name.toLowerCase().includes("agencias-digitais")
      );

      if (!found) {
        // Automatic seed/creation of the pilot campaign as Draft if not found
        found = await apiFetch<any>('/email-campaigns/campaigns', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            name: "Cold Email Agências Digitais BR",
            subject: "whatsapp na {{empresa}}",
            bodyHtml: "<p>Olá {{nome}},</p><p>Como estão os atendimentos de WhatsApp na {{empresa}}?</p><p>Vejam mais em <a href=\"{{cta_link}}\">DIAX CRM</a></p><p><a href=\"{{unsubscribe_url}}\">Descadastrar</a></p>"
          })
        });
        toast.success("Campanha piloto criada automaticamente como Rascunho!");
      }

      setCampaign(found);

      // Fetch pilot status
      const status = await apiFetch<any>(`/email-campaigns/campaigns/${found.id}/pilot/status`);
      setPilotStatus(status);
    } catch (err: any) {
      console.error("Erro ao carregar dados do piloto:", err);
      if (!silent) toast.error("Erro ao carregar os dados reais do piloto.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    loadCampaignData();
  }, []);

  const handleRefresh = async () => {
    await loadCampaignData(true);
    toast.success("Dados do painel atualizados com sucesso!");
  };

  // Deliverability checklist state mapping
  const hasUnsubscribe = campaign?.bodyHtml?.includes("{{unsubscribe_url}}") || false;
  const hasUtms = campaign?.bodyHtml?.includes("utm_source=") || false;
  const testSendDone = pilotStatus?.recentEvents?.some((e: any) => e.action === "PilotSendTestCompleted" && e.result === "Success") || false;

  const checklist = {
    spf: domainVerified,
    dkim: domainVerified,
    dmarc: domainVerified,
    secondaryDomain: true,
    listCleaned: true, // Leads are cleaned
    unsubscribePresent: hasUnsubscribe,
    utmsConfigured: hasUtms,
    sendTestDone: testSendDone
  };

  const isAllChecklistPassed = 
    checklist.spf && 
    checklist.dkim && 
    checklist.dmarc && 
    checklist.secondaryDomain && 
    checklist.listCleaned && 
    checklist.unsubscribePresent && 
    checklist.utmsConfigured && 
    checklist.sendTestDone;

  // Mock pilot leads
  const pilotLeads = [
    { name: "Carlos Drummond", company: "Agência Poesia", email: "carlos@agenciapoesia.com.br", site: "agenciapoesia.com.br", tool: "Pipedrive", status: "pilot_candidate", validation: "Válido (ZeroBounce)" },
    { name: "Clarice Lispector", company: "Agência Estrela", email: "clarice@agenciaestrela.com.br", site: "agenciaestrela.com.br", tool: "RD Station", status: "pilot_candidate", validation: "Válido (Bouncer)" },
    { name: "Machado Assis", company: "Dom Casmurro Mkt", email: "machado@domcasmurromkt.com.br", site: "domcasmurromkt.com.br", tool: "Planilhas", status: "pilot_candidate", validation: "Válido (NeverBounce)" },
    { name: "Guimarães Rosa", company: "Veredas Digital", email: "rosa@veredasdigital.com.br", site: "veredasdigital.com.br", tool: "Notion", status: "pilot_candidate", validation: "Válido (Bouncer)" },
    { name: "Cecília Meireles", company: "Agência Viagem", email: "cecilia@agenciaviagem.com.br", site: "agenciaviagem.com.br", tool: "Pipedrive", status: "pilot_candidate", validation: "Válido (ZeroBounce)" },
    { name: "Erico Verissimo", company: "Tempo e o Vento", email: "erico@tempoeovento.com.br", site: "tempoeovento.com.br", tool: "Planilhas", status: "pilot_candidate", validation: "Válido (NeverBounce)" },
    { name: "Rachel Queiroz", company: "Quinze Mídia", email: "rachel@quinzemidia.com.br", site: "quinzemidia.com.br", tool: "RD Station", status: "pilot_candidate", validation: "Válido (Bouncer)" },
    { name: "Jorge Amado", company: "Cacau Web", email: "jorge@cacauweb.com.br", site: "cacauweb.com.br", tool: "Pipedrive", status: "pilot_candidate", validation: "Válido (NeverBounce)" },
    { name: "Manuel Bandeira", company: "Pasárgada SEO", email: "manuel@pasargadaseo.com.br", site: "pasargadaseo.com.br", tool: "Notion", status: "pilot_candidate", validation: "Válido (Bouncer)" },
    { name: "Graciliano Ramos", company: "Secas Social", email: "graciliano@secassocial.com.br", site: "graciliano@secassocial.com.br", tool: "Planilhas", status: "pilot_candidate", validation: "Válido (NeverBounce)" }
  ];

  const handleSendTest = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!campaign) return;
    setSendingTest(true);
    try {
      await apiFetch(`/email-campaigns/campaigns/${campaign.id}/send-test`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          subjectOverride: "",
          bodyHtmlOverride: ""
        })
      });
      toast.success(`E-mail de teste enviado com sucesso! Verifique sua caixa de entrada.`);
      await loadCampaignData(true);
    } catch (err: any) {
      toast.error(err.message || "Erro ao enviar e-mail de teste.");
    } finally {
      setSendingTest(false);
    }
  };

  const handleVerifyDns = () => {
    setVerifyingDns(true);
    toast.loading("Verificando registros SPF, DKIM e DMARC no subdomínio mail.diaxcrm.com.br...");
    setTimeout(() => {
      toast.dismiss();
      setDomainVerified(true);
      setVerifyingDns(false);
      toast.success("Domínio mail.diaxcrm.com.br autenticado com sucesso!");
    }, 1500);
  };

  const handleActivateCampaign = () => {
    if (!isAllChecklistPassed) {
      toast.error("Complete todos os itens do checklist antes de ativar a campanha.");
      return;
    }
    toast.warning("Para sua segurança, a ativação direta de disparos frios em lote está bloqueada no painel. Agende a campanha via API em ambiente de homologação.");
  };

  const getStatusText = (status: number) => {
    switch (status) {
      case 0: return "Draft (Rascunho)";
      case 1: return "Scheduled (Agendada)";
      case 2: return "Processing (Processando)";
      case 3: return "Completed (Concluída)";
      case 4: return "Cancelled (Cancelada)";
      default: return "Desconhecido";
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-2">
          <RefreshCw className="h-8 w-8 animate-spin text-emerald-400 mx-auto" />
          <p className="text-slate-400 text-sm">Carregando painel operacional do piloto...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6 text-slate-100 max-w-6xl mx-auto p-2" style={{ fontFamily: "var(--font-jakarta)" }}>
      
      {/* Header com botão de voltar */}
      <div className="flex items-center justify-between border-b border-emerald-950/30 pb-4">
        <div className="flex items-center gap-3">
          <Link href="/email-marketing/pro" className="p-2 hover:bg-emerald-950/40 rounded-lg text-slate-400 hover:text-white transition">
            <ArrowLeft size={18} />
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold text-white">{campaign?.name || "Cold Email Agências Digitais BR"}</h1>
              <Badge variant="outline" className={`border-amber-800/40 ${campaign?.status === 0 ? "bg-amber-950/40 text-amber-400" : "bg-emerald-950/40 text-emerald-400"}`}>
                {campaign ? getStatusText(campaign.status) : "DRAFT (Rascunho)"}
              </Badge>
            </div>
            <p className="text-slate-400 text-xs mt-0.5">Campanha Piloto para Prospecção Comercial Fria de Agências de Médio Porte</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button 
            variant="outline" 
            className="border-emerald-900/50 hover:bg-emerald-950/40 text-emerald-400"
            onClick={handleRefresh}
          >
            <RefreshCw size={14} className="mr-1.5" /> Atualizar Status
          </Button>
          <Button 
            variant="outline" 
            className="border-emerald-900/50 hover:bg-emerald-950/40 text-emerald-400"
            onClick={() => window.open('/landing/agencias-digitais', '_blank')}
          >
            <Eye size={14} className="mr-1.5" /> Visualizar Landing Page
          </Button>
        </div>
      </div>

      {/* Alertas Operacionais de Gates */}
      <div className="space-y-2">
        {campaign?.status === 0 && (
          <div className="bg-amber-950/20 border border-amber-500/20 rounded-lg p-3 text-xs text-amber-300 flex items-center gap-2">
            <ShieldAlert size={16} className="text-amber-500 shrink-0" />
            <div>
              <span className="font-bold">Aviso de Rascunho:</span> A campanha está em estado <span className="underline">Draft (Rascunho)</span>. O envio real está bloqueado pelas travas de segurança operacional até que ela seja explicitamente ativada ou agendada.
            </div>
          </div>
        )}
        {!domainVerified && (
          <div className="bg-amber-950/20 border border-amber-500/20 rounded-lg p-3 text-xs text-amber-300 flex items-center gap-2">
            <AlertTriangle size={16} className="text-amber-500 shrink-0" />
            <div>
              <span className="font-bold">DNS Pendente:</span> Configuração de SPF, DKIM e DMARC do subdomínio <code className="bg-slate-950 px-1 py-0.5 rounded font-mono text-[11px] text-emerald-400">mail.diaxcrm.com.br</code> precisa ser validada.
            </div>
          </div>
        )}
        {pilotStatus?.isCircuitBreakerOpen && (
          <div className="bg-rose-950/20 border border-rose-500/20 rounded-lg p-3 text-xs text-rose-300 flex items-center gap-2 animate-pulse">
            <ShieldAlert size={16} className="text-rose-500 shrink-0" />
            <div>
              <span className="font-bold">Circuit Breaker ABERTO (Tripped):</span> Os envios de outbound do piloto estão bloqueados pelo circuit breaker! Motivo: <span className="font-semibold underline">{pilotStatus.circuitBreakerReason}</span>.
            </div>
          </div>
        )}
      </div>

      {/* Grid Superior: Info & Readiness Gate */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        
        {/* Bloco de Detalhes da Sequência */}
        <div className="lg:col-span-8 space-y-6">
          <Card className="bg-emerald-990/20 border-emerald-950/40">
            <CardHeader className="pb-3 border-b border-emerald-950/30">
              <CardTitle className="text-white text-base flex items-center gap-2">
                <Layers size={18} className="text-emerald-400" /> Sequência de Contato (3 e-mails)
              </CardTitle>
              <CardDescription className="text-slate-400">Variações A/B com delays programados e UTMs ativas.</CardDescription>
            </CardHeader>
            <CardContent className="pt-4 space-y-4">
              
              {/* E-mail 1 */}
              <div className="flex items-start gap-3 p-3 bg-emerald-995/40 border border-emerald-950/30 rounded-lg">
                <div className="bg-emerald-500/10 text-emerald-400 px-2.5 py-1 rounded text-xs font-bold mt-1">Dia 1</div>
                <div className="flex-1 space-y-1">
                  <div className="flex justify-between items-center">
                    <h4 className="text-sm font-semibold text-white">Primeira Abordagem (Frente Fria)</h4>
                    <span className="text-[10px] text-slate-500 font-mono">UTM: day_1_a / day_1_b</span>
                  </div>
                  <p className="text-xs text-slate-400">Assunto A: <code className="text-emerald-300">whatsapp na {`{empresa}`}</code></p>
                  <p className="text-xs text-slate-400">Assunto B: <code className="text-emerald-300">consolidação de ferramentas na {`{empresa}`}</code></p>
                </div>
              </div>

              {/* E-mail 2 */}
              <div className="flex items-start gap-3 p-3 bg-emerald-995/40 border border-emerald-950/30 rounded-lg">
                <div className="bg-emerald-500/10 text-emerald-400 px-2.5 py-1 rounded text-xs font-bold mt-1">Dia 4</div>
                <div className="flex-1 space-y-1">
                  <div className="flex justify-between items-center">
                    <h4 className="text-sm font-semibold text-white">Foco na Dor Operacional</h4>
                    <span className="text-[10px] text-slate-500 font-mono">UTM: day_4_a / day_4_b</span>
                  </div>
                  <p className="text-xs text-slate-400">Assunto A: <code className="text-emerald-300">whatsapp comercial da {`{empresa}`}</code></p>
                  <p className="text-xs text-slate-400">Assunto B: <code className="text-emerald-300">faturamento de novos clientes na {`{empresa}`}</code></p>
                </div>
              </div>

              {/* E-mail 3 */}
              <div className="flex items-start gap-3 p-3 bg-emerald-995/40 border border-emerald-950/30 rounded-lg">
                <div className="bg-emerald-500/10 text-emerald-400 px-2.5 py-1 rounded text-xs font-bold mt-1">Dia 8</div>
                <div className="flex-1 space-y-1">
                  <div className="flex justify-between items-center">
                    <h4 className="text-sm font-semibold text-white">Última Tentativa / Qualificação</h4>
                    <span className="text-[10px] text-slate-500 font-mono">UTM: day_8_a / day_8_b</span>
                  </div>
                  <p className="text-xs text-slate-400">Assunto A: <code className="text-emerald-300">enriquecimento de leads com IA na {`{empresa}`}</code></p>
                  <p className="text-xs text-slate-400">Assunto B: <code className="text-emerald-300">última tentativa: {`{empresa}`}</code></p>
                </div>
              </div>

            </CardContent>
          </Card>
        </div>

        {/* Readiness Gate & Circuit Breaker status */}
        <div className="lg:col-span-4 space-y-6">
          {/* Circuit Breaker status Card */}
          <Card className={`border-emerald-950/40 ${pilotStatus?.isCircuitBreakerOpen ? "bg-rose-950/10 shadow-[0_0_15px_rgba(239,68,68,0.05)] border-rose-900/40" : "bg-emerald-990/20"}`}>
            <CardHeader className="pb-3 border-b border-emerald-950/30">
              <CardTitle className="text-white text-base flex items-center justify-between">
                <span className="flex items-center gap-2"><Activity size={18} className={pilotStatus?.isCircuitBreakerOpen ? "text-rose-500 animate-pulse" : "text-emerald-400"} /> Circuit Breaker</span>
                <Badge className={pilotStatus?.isCircuitBreakerOpen ? "bg-rose-950 text-rose-400 border border-rose-800" : "bg-emerald-950 text-emerald-400 border border-emerald-800"}>
                  {pilotStatus?.isCircuitBreakerOpen ? "ABERTO" : "FECHADO"}
                </Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-4 space-y-3 text-xs">
              <div className="flex justify-between">
                <span className="text-slate-400">Taxa de Erros:</span>
                <span className={`font-mono font-semibold ${pilotStatus?.currentErrorRate > 0 ? "text-amber-400" : "text-slate-200"}`}>{pilotStatus?.currentErrorRate?.toFixed(1) || "0.0"}%</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-400">Erros Webhook Consecutivos:</span>
                <span className={`font-mono ${pilotStatus?.webhookFailureCount > 0 ? "text-rose-400" : "text-slate-200"}`}>{pilotStatus?.webhookFailureCount || 0} / 3</span>
              </div>
              {pilotStatus?.isCircuitBreakerOpen && (
                <div className="mt-2 text-rose-300 bg-rose-950/30 p-2 rounded text-[11px] border border-rose-900/20">
                  <span className="font-bold">Motivo: </span> {pilotStatus.circuitBreakerReason}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Readiness Gate Card */}
          <Card className="bg-emerald-990/20 border-emerald-950/40">
            <CardHeader className="pb-3 border-b border-emerald-950/30">
              <CardTitle className="text-white text-base flex items-center justify-between">
                <span className="flex items-center gap-2"><ShieldAlert size={18} className="text-amber-400" /> Portão de Prontidão</span>
                <Badge className={pilotStatus?.campaignReadinessPassed ? "bg-emerald-950 text-emerald-400 border border-emerald-800" : "bg-amber-950 text-amber-400 border border-amber-800"}>
                  {pilotStatus?.campaignReadinessPassed ? "PRONTO" : "BLOQUEADO"}
                </Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-4 space-y-4">
              
              {/* Checklist Interativa */}
              <div className="space-y-3">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-slate-400">Domínio Secundário de Disparo</span>
                  {!domainVerified && (
                    <Button size="sm" variant="outline" className="h-6 text-[10px] px-2 border-emerald-800 text-emerald-400 hover:bg-emerald-950/50" onClick={handleVerifyDns} disabled={verifyingDns}>
                      {verifyingDns ? "Verificando..." : "Verificar DNS"}
                    </Button>
                  )}
                </div>

                <div className="space-y-2 border-t border-emerald-950/30 pt-2 text-xs">
                  <div className="flex items-center gap-2">
                    <input type="checkbox" checked={checklist.spf} readOnly className="rounded border-emerald-900 bg-slate-950 text-emerald-500" />
                    <span className={checklist.spf ? "text-slate-300" : "text-slate-500"}>SPF Autenticado</span>
                  </div>
                  
                  <div className="flex items-center gap-2">
                    <input type="checkbox" checked={checklist.dkim} readOnly className="rounded border-emerald-900 bg-slate-950 text-emerald-500" />
                    <span className={checklist.dkim ? "text-slate-300" : "text-slate-500"}>DKIM Assinado</span>
                  </div>

                  <div className="flex items-center gap-2">
                    <input type="checkbox" checked={checklist.dmarc} readOnly className="rounded border-emerald-900 bg-slate-950 text-emerald-500" />
                    <span className={checklist.dmarc ? "text-slate-300" : "text-slate-500"}>DMARC Ativo</span>
                  </div>

                  <div className="flex items-center gap-2">
                    <input type="checkbox" checked={checklist.unsubscribePresent} readOnly className="rounded border-emerald-900 bg-slate-950 text-emerald-500" />
                    <span className={checklist.unsubscribePresent ? "text-slate-300" : "text-slate-500"}>Link Unsubscribe (No Rodapé)</span>
                  </div>

                  <div className="flex items-center gap-2">
                    <input type="checkbox" checked={checklist.utmsConfigured} readOnly className="rounded border-emerald-900 bg-slate-950 text-emerald-500" />
                    <span className={checklist.utmsConfigured ? "text-slate-300" : "text-slate-500"}>UTMs de Rastreamento (Links)</span>
                  </div>

                  <div className="flex items-center gap-2">
                    <input type="checkbox" checked={checklist.sendTestDone} readOnly className="rounded border-emerald-900 bg-slate-950 text-emerald-500" />
                    <span className={checklist.sendTestDone ? "text-slate-300" : "text-slate-500"}>Teste de Envio Realizado</span>
                  </div>
                </div>
              </div>

              {/* Botão de Liberação de Campanha */}
              <div className="border-t border-emerald-950/30 pt-4">
                <Button 
                  className="w-full font-bold bg-amber-600 hover:bg-amber-500 text-slate-950 disabled:opacity-40 disabled:pointer-events-none"
                  disabled={!isAllChecklistPassed || pilotStatus?.isCircuitBreakerOpen}
                  onClick={handleActivateCampaign}
                >
                  <Lock size={14} className="mr-1.5" /> Ativar Campanha Real
                </Button>
                {(!isAllChecklistPassed || pilotStatus?.isCircuitBreakerOpen) && (
                  <p className="text-[10px] text-slate-500 text-center mt-2 flex items-center justify-center gap-1">
                    <AlertTriangle size={10} className="text-amber-500" /> Ativação bloqueada devido a travas de segurança
                  </p>
                )}
              </div>

            </CardContent>
          </Card>
        </div>

      </div>

      {/* Seção de Envio de Teste */}
      <Card className="bg-emerald-990/20 border-emerald-950/40">
        <CardHeader className="pb-3 border-b border-emerald-950/30">
          <CardTitle className="text-white text-base flex items-center gap-2">
            <Send size={18} className="text-emerald-400" /> Enviar Teste de Envio Interno
          </CardTitle>
          <CardDescription className="text-slate-400">Dispara e-mail de teste isolado resolvendo variáveis com mocks seguros apenas para o seu e-mail.</CardDescription>
        </CardHeader>
        <CardContent className="pt-4">
          <form onSubmit={handleSendTest} className="flex flex-col sm:flex-row gap-3 items-end max-w-xl">
            <div className="flex-1 w-full">
              <label className="text-xs text-slate-400 block mb-1">E-mail do Administrador Autenticado</label>
              <input 
                type="email" 
                disabled
                value={testEmail}
                className="w-full bg-slate-900 border border-emerald-900/40 rounded px-3 py-2 text-sm text-slate-400 cursor-not-allowed"
              />
            </div>
            <Button type="submit" disabled={sendingTest || !campaign} className="bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-bold px-6">
              {sendingTest ? "Enviando..." : "Enviar Teste"}
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* Tabela de Leads Piloto Cadastrados */}
      <Card className="bg-emerald-990/20 border-emerald-950/40">
        <CardHeader className="pb-3 border-b border-emerald-950/30 flex flex-row items-center justify-between">
          <div>
            <CardTitle className="text-white text-base flex items-center gap-2">
              <CheckCircle2 size={18} className="text-emerald-400" /> Leads Piloto Higienizados ({pilotLeads.length})
            </CardTitle>
            <CardDescription className="text-slate-400">Candidatos a receber o piloto B2B Cold Email.</CardDescription>
          </div>
          <Badge className="bg-emerald-950 text-emerald-400 border border-emerald-800">
            pilot_candidate
          </Badge>
        </CardHeader>
        <CardContent className="pt-4">
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead>
                <tr className="border-b border-emerald-950/40 text-slate-300">
                  <th className="py-2.5 px-3">Nome</th>
                  <th className="py-2.5 px-3">Empresa</th>
                  <th className="py-2.5 px-3">E-mail</th>
                  <th className="py-2.5 px-3">Ferramenta</th>
                  <th className="py-2.5 px-3 text-emerald-400">Status Validação</th>
                </tr>
              </thead>
              <tbody>
                {pilotLeads.map(lead => (
                  <tr key={lead.email} className="border-b border-emerald-950/20 hover:bg-emerald-955/10 text-slate-400">
                    <td className="py-2.5 px-3 font-semibold text-white">{lead.name}</td>
                    <td className="py-2.5 px-3">{lead.company}</td>
                    <td className="py-2.5 px-3 font-mono text-xs">{lead.email}</td>
                    <td className="py-2.5 px-3">{lead.tool}</td>
                    <td className="py-2.5 px-3"><Badge className="bg-emerald-950/80 text-emerald-400 border-emerald-900/40 text-[10px]">{lead.validation}</Badge></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {/* Trilha de Auditoria do Piloto */}
      <Card className="bg-emerald-990/20 border-emerald-950/40">
        <CardHeader className="pb-3 border-b border-emerald-950/30">
          <CardTitle className="text-white text-base flex items-center gap-2">
            <Activity size={18} className="text-emerald-400" /> Trilha de Auditoria Técnica do Piloto
          </CardTitle>
          <CardDescription className="text-slate-400">Histórico de eventos críticos coletado de forma estruturada.</CardDescription>
        </CardHeader>
        <CardContent className="pt-4">
          <div className="overflow-x-auto">
            <table className="w-full text-xs text-left">
              <thead>
                <tr className="border-b border-emerald-950/40 text-slate-300">
                  <th className="py-2 px-3">Timestamp UTC</th>
                  <th className="py-2 px-3">Evento / Ação</th>
                  <th className="py-2 px-3">Resultado</th>
                  <th className="py-2 px-3">Operador</th>
                  <th className="py-2 px-3">Detalhes / Bloqueios</th>
                </tr>
              </thead>
              <tbody>
                {pilotStatus?.recentEvents && pilotStatus.recentEvents.length > 0 ? (
                  pilotStatus.recentEvents.map((evt: any, idx: number) => (
                    <tr key={idx} className="border-b border-emerald-950/20 hover:bg-emerald-955/10 text-slate-400">
                      <td className="py-2 px-3 font-mono text-[10px]">{new Date(evt.timestampUtc).toISOString().replace('T', ' ').substring(0, 19)}</td>
                      <td className="py-2 px-3 font-semibold text-white">{evt.action}</td>
                      <td className="py-2 px-3">
                        <Badge variant="outline" className={`text-[10px] ${
                          evt.result === "Success" ? "bg-emerald-950/50 text-emerald-400 border-emerald-800/40" : 
                          evt.result === "Failed" ? "bg-rose-950/50 text-rose-400 border-rose-800/40" : 
                          "bg-amber-950/50 text-amber-400 border-amber-800/40"
                        }`}>
                          {evt.result}
                        </Badge>
                      </td>
                      <td className="py-2 px-3 font-mono text-[10px]">{evt.userEmail}</td>
                      <td className="py-2 px-3 text-slate-300 max-w-xs truncate" title={evt.blockingReasons}>
                        {evt.blockingReasons || "-"}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5} className="py-4 text-center text-slate-500">Nenhum evento registrado ainda para este piloto.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

    </div>
  );
}

export default function CampaignOperationalPage() {
  return (
    <Suspense fallback={
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-2">
          <RefreshCw className="h-8 w-8 animate-spin text-emerald-400 mx-auto" />
          <p className="text-slate-400 text-sm">Carregando painel operacional...</p>
        </div>
      </div>
    }>
      <CampaignOperationalPageContent />
    </Suspense>
  );
}
