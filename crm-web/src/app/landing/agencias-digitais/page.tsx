"use client";

import React, { useEffect, useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { 
  ArrowRight, CheckCircle2, MessageSquare, DollarSign, 
  Brain, Users, Play, Calendar, ShieldCheck, XCircle, ChevronRight 
} from "lucide-react";
import { toast } from "sonner";

function LandingPageContent() {
  const searchParams = useSearchParams();
  
  // State for UTM parameters
  const [utms, setUtms] = useState({
    source: "",
    medium: "",
    campaign: "",
    content: ""
  });

  // State for lead form
  const [formData, setFormData] = useState({
    nome: "",
    empresa: "",
    email: "",
    whatsapp: "",
    ferramentaAtual: "Pipedrive",
    site: ""
  });

  const [formSubmitted, setFormSubmitted] = useState(false);
  const [showVideoModal, setShowVideoModal] = useState(false);

  useEffect(() => {
    // Preserve UTMs from URL
    const utmSource = searchParams.get("utm_source") || "";
    const utmMedium = searchParams.get("utm_medium") || "";
    const utmCampaign = searchParams.get("utm_campaign") || "";
    const utmContent = searchParams.get("utm_content") || "";

    const parsedUtms = {
      source: utmSource,
      medium: utmMedium,
      campaign: utmCampaign,
      content: utmContent
    };

    setUtms(parsedUtms);

    // Save UTMs in local storage to preserve across sessions if needed
    if (utmSource) {
      localStorage.setItem("diax_landing_utms", JSON.stringify(parsedUtms));
    }
  }, [searchParams]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    // Simular o recebimento do lead (modo draft/homologação)
    console.log("Lead Capturado (Modo Homologação):", {
      ...formData,
      utms,
      capturedAt: new Date().toISOString()
    });

    setFormSubmitted(true);
    toast.success("Demonstração agendada com sucesso! Entraremos em contato em breve.");
  };

  return (
    <div className="min-h-screen text-slate-100 font-sans" style={{ background: '#0F1A14' }}>
      
      {/* Header Falso (Sem navegação interna de admin) */}
      <header className="border-b border-emerald-950/40 bg-emerald-995/50 backdrop-blur-md sticky top-0 z-50">
        <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-emerald-500 to-emerald-400 flex items-center justify-center font-bold text-white text-sm">
              D
            </div>
            <span className="font-bold text-lg tracking-tight text-white">DIAX <span className="text-emerald-400">CRM</span></span>
          </div>
          <div>
            <Badge variant="outline" className="border-emerald-700/50 text-emerald-400">
              Campanha de Prospecção 2026
            </Badge>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative pt-12 pb-20 overflow-hidden">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_30%,rgba(16,185,129,0.08),transparent_50%)]" />
        <div className="max-w-6xl mx-auto px-4 relative">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
            
            {/* Texto Hero */}
            <div className="lg:col-span-7 space-y-6 text-left">
              <Badge variant="secondary" className="bg-emerald-950/60 border border-emerald-800/40 text-emerald-400 px-3 py-1 text-xs font-semibold">
                Para Agências Digitais (5 a 30 funcionários)
              </Badge>
              
              <h1 className="text-4xl md:text-5xl lg:text-6xl font-extrabold text-white tracking-tight leading-tight">
                Seu Comercial e Financeiro unidos. <br />
                <span className="bg-clip-text text-transparent bg-gradient-to-r from-emerald-400 via-teal-300 to-emerald-500">
                  Com WhatsApp Nativo.
                </span>
              </h1>
              
              <p className="text-lg text-slate-400 leading-relaxed max-w-xl">
                Pare de perder leads e histórico comercial entre Pipedrive, planilhas e integradores caros de WhatsApp. Centralize tudo no único CRM em português construído para agências.
              </p>

              <div className="flex flex-wrap gap-4 pt-2">
                <Button 
                  size="lg" 
                  className="bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-bold"
                  onClick={() => setShowVideoModal(true)}
                >
                  <Play className="mr-2 h-4 w-4 fill-current" /> Ver demo em vídeo
                </Button>
                <Button 
                  size="lg" 
                  variant="outline" 
                  className="border-emerald-800/50 hover:bg-emerald-950/40 text-emerald-400"
                  onClick={() => {
                    document.getElementById("lead-form")?.scrollIntoView({ behavior: "smooth" });
                  }}
                >
                  <Calendar className="mr-2 h-4 w-4" /> Agendar demonstração
                </Button>
              </div>

              {/* Badges de Destaque */}
              <div className="grid grid-cols-3 gap-4 pt-6 border-t border-emerald-950/40 max-w-lg">
                <div>
                  <h3 className="text-white font-bold text-2xl">R$ 300</h3>
                  <p className="text-xs text-slate-500">Valor fixo mensal</p>
                </div>
                <div>
                  <h3 className="text-white font-bold text-2xl">100%</h3>
                  <p className="text-xs text-slate-500">Em Português</p>
                </div>
                <div>
                  <h3 className="text-white font-bold text-2xl">Nativo</h3>
                  <p className="text-xs text-slate-500">Integração WhatsApp</p>
                </div>
              </div>
            </div>

            {/* Formulário Hero */}
            <div className="lg:col-span-5">
              <Card id="lead-form" className="bg-emerald-990/90 border border-emerald-900/40 shadow-2xl relative">
                <CardHeader>
                  <CardTitle className="text-white text-xl">Solicitar Demonstração Completa</CardTitle>
                  <CardDescription className="text-slate-400">
                    Entenda como economizar até R$ 300/mês e otimizar seu processo comercial.
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {!formSubmitted ? (
                    <form onSubmit={handleSubmit} className="space-y-4">
                      <div>
                        <label className="text-xs font-semibold text-slate-400 block mb-1">Seu Nome *</label>
                        <input 
                          type="text" 
                          name="nome"
                          required
                          value={formData.nome}
                          onChange={handleInputChange}
                          placeholder="Ex: João Silva" 
                          className="w-full bg-slate-900/60 border border-emerald-800/40 rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-emerald-500"
                        />
                      </div>

                      <div>
                        <label className="text-xs font-semibold text-slate-400 block mb-1">Nome da Agência *</label>
                        <input 
                          type="text" 
                          name="empresa"
                          required
                          value={formData.empresa}
                          onChange={handleInputChange}
                          placeholder="Ex: Agência Alfa" 
                          className="w-full bg-slate-900/60 border border-emerald-800/40 rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-emerald-500"
                        />
                      </div>

                      <div>
                        <label className="text-xs font-semibold text-slate-400 block mb-1">E-mail Corporativo *</label>
                        <input 
                          type="email" 
                          name="email"
                          required
                          value={formData.email}
                          onChange={handleInputChange}
                          placeholder="Ex: joao@agenciaalfa.com" 
                          className="w-full bg-slate-900/60 border border-emerald-800/40 rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-emerald-500"
                        />
                      </div>

                      <div>
                        <label className="text-xs font-semibold text-slate-400 block mb-1">WhatsApp de Contato *</label>
                        <input 
                          type="tel" 
                          name="whatsapp"
                          required
                          value={formData.whatsapp}
                          onChange={handleInputChange}
                          placeholder="Ex: (11) 99999-9999" 
                          className="w-full bg-slate-900/60 border border-emerald-800/40 rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-emerald-500"
                        />
                      </div>

                      <div>
                        <label className="text-xs font-semibold text-slate-400 block mb-1">Ferramenta Comercial Atual</label>
                        <select 
                          name="ferramentaAtual"
                          value={formData.ferramentaAtual}
                          onChange={handleInputChange}
                          className="w-full bg-slate-900/60 border border-emerald-800/40 rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-emerald-500"
                        >
                          <option value="Pipedrive">Pipedrive</option>
                          <option value="RD Station">RD Station</option>
                          <option value="Notion/Trello">Notion / Trello</option>
                          <option value="Planilhas">Planilhas Excel/Google Sheets</option>
                          <option value="Outro">Outro CRM / Sem ferramenta</option>
                        </select>
                      </div>

                      {/* Dados ocultos de UTM */}
                      <input type="hidden" name="utm_source" value={utms.source} />
                      <input type="hidden" name="utm_medium" value={utms.medium} />
                      <input type="hidden" name="utm_campaign" value={utms.campaign} />
                      <input type="hidden" name="utm_content" value={utms.content} />

                      <Button type="submit" className="w-full bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-bold py-2.5">
                        Agendar minha demo grátis
                      </Button>
                    </form>
                  ) : (
                    <div className="text-center py-8 space-y-4">
                      <div className="w-12 h-12 bg-emerald-500/20 text-emerald-400 rounded-full flex items-center justify-center mx-auto text-xl font-bold">
                        ✓
                      </div>
                      <h3 className="text-white font-bold text-lg">Obrigado pelo contato, {formData.nome}!</h3>
                      <p className="text-sm text-slate-400">
                        Nossa equipe comercial entrará em contato via WhatsApp no número {formData.whatsapp} em menos de 2 horas comerciais para realizar sua demonstração de 20 minutos.
                      </p>
                      {utms.source && (
                        <div className="text-[10px] text-slate-600 bg-slate-950/40 p-2 rounded">
                          Código de Tracking Preservado: {utms.source} | {utms.content}
                        </div>
                      )}
                    </div>
                  )}
                </CardContent>
              </Card>
            </div>

          </div>
        </div>
      </section>

      {/* Grid de Dores e Benefícios */}
      <section className="py-20 bg-slate-950/40 border-y border-emerald-950/30">
        <div className="max-w-6xl mx-auto px-4">
          <div className="text-center max-w-2xl mx-auto mb-16">
            <Badge className="bg-emerald-950 text-emerald-400 border border-emerald-800 mb-2">Por que o DIAX CRM?</Badge>
            <h2 className="text-3xl font-bold text-white">Criado especificamente para resolver as maiores dores das agências de marketing</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            
            {/* Card 1: WhatsApp */}
            <Card className="bg-emerald-990/40 border border-emerald-950/30">
              <CardHeader className="pb-2">
                <div className="w-10 h-10 rounded-lg bg-emerald-500/10 text-emerald-400 flex items-center justify-center mb-2">
                  <MessageSquare size={20} />
                </div>
                <CardTitle className="text-white text-lg font-bold">WhatsApp Nativo</CardTitle>
              </CardHeader>
              <CardContent className="text-sm text-slate-400">
                Grave todas as conversas do time de vendas automaticamente. Sem necessidade de pagar integradores terceiros ou assinar APIs oficiais caríssimas.
              </CardContent>
            </Card>

            {/* Card 2: Financeiro */}
            <Card className="bg-emerald-990/40 border border-emerald-950/30">
              <CardHeader className="pb-2">
                <div className="w-10 h-10 rounded-lg bg-emerald-500/10 text-emerald-400 flex items-center justify-center mb-2">
                  <DollarSign size={20} />
                </div>
                <CardTitle className="text-white text-lg font-bold">Financeiro Integrado</CardTitle>
              </CardHeader>
              <CardContent className="text-sm text-slate-400">
                Contratos fechados no comercial geram o faturamento de recorrências imediatamente no módulo financeiro. Reduz o tempo administrativo a zero.
              </CardContent>
            </Card>

            {/* Card 3: IA Leads */}
            <Card className="bg-emerald-990/40 border border-emerald-950/30">
              <CardHeader className="pb-2">
                <div className="w-10 h-10 rounded-lg bg-emerald-500/10 text-emerald-400 flex items-center justify-center mb-2">
                  <Brain size={20} />
                </div>
                <CardTitle className="text-white text-lg font-bold">Módulo de Leads com IA</CardTitle>
              </CardHeader>
              <CardContent className="text-sm text-slate-400">
                Nossa IA enriquece dados de contatos e faz a triagem inteligente de fit das empresas antes do primeiro e-mail ou mensagem de prospecção.
              </CardContent>
            </Card>

            {/* Card 4: Preço */}
            <Card className="bg-emerald-990/40 border border-emerald-950/30">
              <CardHeader className="pb-2">
                <div className="w-10 h-10 rounded-lg bg-emerald-500/10 text-emerald-400 flex items-center justify-center mb-2">
                  <Users size={20} />
                </div>
                <CardTitle className="text-white text-lg font-bold">R$ 300 / Mês Flat</CardTitle>
              </CardHeader>
              <CardContent className="text-sm text-slate-400">
                Valor fixo mensal sem cobrança surpresa por novo usuário comercial. Ideal para agências que desejam crescer o time sem explodir o caixa.
              </CardContent>
            </Card>

          </div>
        </div>
      </section>

      {/* Tabela de Comparação */}
      <section className="py-20 max-w-4xl mx-auto px-4">
        <div className="text-center mb-12">
          <h2 className="text-3xl font-bold text-white">Como nos comparamos ao mercado?</h2>
          <p className="text-slate-400 mt-2 text-sm">Compare a stack fragmentada tradicional com a simplicidade integrada do DIAX CRM.</p>
        </div>

        <div className="border border-emerald-900/30 rounded-lg overflow-hidden bg-slate-950/20">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-emerald-950 bg-emerald-995/40 text-slate-300">
                <th className="p-4 font-semibold">Recurso</th>
                <th className="p-4 font-semibold text-emerald-400">DIAX CRM</th>
                <th className="p-4 font-semibold text-slate-500">Stack Tradicional (Pipedrive/RD + WhatsApp + ERP)</th>
              </tr>
            </thead>
            <tbody>
              <tr className="border-b border-emerald-950/40">
                <td className="p-4 font-medium text-white">Integração WhatsApp</td>
                <td className="p-4 text-emerald-400 flex items-center gap-1.5"><ShieldCheck size={16} /> Nativa (Grátis)</td>
                <td className="p-4 text-slate-500 flex items-center gap-1.5"><XCircle size={16} /> Requer integrador pago (R$ 150+/mês)</td>
              </tr>
              <tr className="border-b border-emerald-950/40">
                <td className="p-4 font-medium text-white">Módulo Financeiro</td>
                <td className="p-4 text-emerald-400 flex items-center gap-1.5"><ShieldCheck size={16} /> Integrado ao Pipeline</td>
                <td className="p-4 text-slate-500 flex items-center gap-1.5"><XCircle size={16} /> Sistemas separados ou planilhas manuais</td>
              </tr>
              <tr className="border-b border-emerald-950/40">
                <td className="p-4 font-medium text-white">IA de Qualificação</td>
                <td className="p-4 text-emerald-400 flex items-center gap-1.5"><ShieldCheck size={16} /> Inclusa nativamente</td>
                <td className="p-4 text-slate-500 flex items-center gap-1.5"><XCircle size={16} /> Não possui ou cobra adicionais caros</td>
              </tr>
              <tr className="border-b border-emerald-950/40">
                <td className="p-4 font-medium text-white">Modelo de Cobrança</td>
                <td className="p-4 text-emerald-400 flex items-center gap-1.5"><ShieldCheck size={16} /> R$ 300 Fixo Mensal</td>
                <td className="p-4 text-slate-500 flex items-center gap-1.5"><XCircle size={16} /> Por usuário (escala e fica caro)</td>
              </tr>
              <tr>
                <td className="p-4 font-semibold text-white">Custo Estimado (Time de 5 pessoas)</td>
                <td className="p-4 font-bold text-emerald-400">R$ 300 / mês</td>
                <td className="p-4 text-slate-500">R$ 750+ / mês</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-emerald-950/40 bg-slate-950/60 py-8 text-center text-xs text-slate-600">
        <div className="max-w-6xl mx-auto px-4 space-y-4">
          <p>© {new Date().getFullYear()} DIAX CRM. Todos os direitos reservados. Foco em privacidade de dados em conformidade com a LGPD.</p>
          <p className="text-[10px]">As informações de marcas registradas Pipedrive e RD Station são de propriedade de seus respectivos titulares. Valores simulados com base nas tabelas públicas em 2026.</p>
        </div>
      </footer>

      {/* Video Modal Mock (Popup) */}
      {showVideoModal && (
        <div className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4" onClick={() => setShowVideoModal(false)}>
          <div className="bg-slate-900 border border-emerald-900/40 rounded-lg overflow-hidden max-w-3xl w-full" onClick={e => e.stopPropagation()}>
            <div className="p-4 bg-slate-950 flex justify-between items-center border-b border-emerald-950/40">
              <h3 className="text-white font-bold">Demonstração DIAX CRM - Agências Digitais</h3>
              <button onClick={() => setShowVideoModal(false)} className="text-slate-400 hover:text-white font-bold text-sm">Fechar (X)</button>
            </div>
            <div className="relative aspect-video bg-black flex items-center justify-center">
              {/* Vídeo Mockup representativo */}
              <div className="text-center p-8 space-y-2">
                <Play className="w-16 h-16 text-emerald-400 mx-auto animate-pulse" />
                <p className="text-white font-bold text-lg">Carregando vídeo explicativo...</p>
                <p className="text-xs text-slate-500">Fluxo: Captura de Lead → WhatsApp Automatizado → Geração de Cobrança no Fechamento</p>
              </div>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}

export default function LandingPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen flex items-center justify-center bg-[#0F1A14] text-white">
        Carregando página...
      </div>
    }>
      <LandingPageContent />
    </Suspense>
  );
}
