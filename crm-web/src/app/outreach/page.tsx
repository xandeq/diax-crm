'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  BarChart3,
  Mail,
  MessageSquare,
  Settings,
  Users,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

import {
  getOutreachConfig,
  getOutreachDashboard,
  getReadyLeads,
  getWhatsAppReadyLeads,
  getWhatsAppStatus,
  OutreachConfigResponse,
  OutreachDashboardResponse,
  ReadyLeadResponse,
  runSegmentation,
  sendOutreachCampaign,
  sendWhatsApp,
  sendWhatsAppCampaign,
  sendWhatsAppFollowUp,
  updateOutreachConfig,
  UpdateOutreachConfigRequest,
  WhatsAppConnectionStatus,
  WhatsAppReadyLeadResponse,
  getValidEmailCount,
} from '@/services/outreach';
import { EmailMarketingComposerModal } from '@/components/email/EmailMarketingComposerModal';
import { EmailQueueSection } from '@/components/outreach/EmailQueueSection';
import { ContactOption } from '@/components/outreach/ContactSearchInput';
import { WhatsAppTab } from '@/components/outreach/WhatsAppTab';
import { ActiveTab } from '@/components/outreach/OutreachShared';
import { DashboardTab } from '@/components/outreach/DashboardTab';
import { ConfigTab } from '@/components/outreach/ConfigTab';
import { TemplatesTab } from '@/components/outreach/TemplatesTab';
import { LeadsTab } from '@/components/outreach/LeadsTab';
import { useRouter, useSearchParams } from 'next/navigation';

// ─── Tab Bar ─────────────────────────────────────────────────────────────────

interface TabBarProps {
  active: ActiveTab;
  onChange: (tab: ActiveTab) => void;
}

const TABS: { id: ActiveTab; label: string; icon: React.ReactNode }[] = [
  { id: 'dashboard', label: 'Dashboard', icon: <BarChart3 className="h-4 w-4" /> },
  { id: 'configuracao', label: 'Configuração', icon: <Settings className="h-4 w-4" /> },
  { id: 'templates', label: 'Templates', icon: <Mail className="h-4 w-4" /> },
  { id: 'leads', label: 'Leads Prontos', icon: <Users className="h-4 w-4" /> },
  { id: 'whatsapp', label: 'WhatsApp', icon: <MessageSquare className="h-4 w-4" /> },
];

function TabBar({ active, onChange }: TabBarProps) {
  return (
    <div className="flex gap-1 border-b border-slate-200 mb-6">
      {TABS.map((tab) => (
        <button
          key={tab.id}
          type="button"
          onClick={() => onChange(tab.id)}
          className={[
            'flex items-center gap-2 px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px',
            active === tab.id
              ? 'border-slate-900 text-slate-900'
              : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300',
          ].join(' ')}
        >
          {tab.icon}
          {tab.label}
        </button>
      ))}
    </div>
  );
}

// ─── Email Marketing Section ──────────────────────────────────────────────────

function EmailMarketingSection({
  validEmailCount,
  onComposeClick,
}: {
  validEmailCount: number | null;
  onComposeClick: () => void;
}) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Email Marketing
            {validEmailCount !== null && (
              <Badge variant="secondary" className="ml-1 font-normal">
                {validEmailCount} contatos com email
              </Badge>
            )}
          </CardTitle>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <p className="text-sm text-slate-500">
          Envie um email personalizado com imagens para todos os contatos com email válido.
          Suporta variáveis como {'{{nome}}'}, {'{{empresa}}'}, {'{{email}}'}, {'{{website}}'}.
        </p>
        <Button onClick={onComposeClick} disabled={validEmailCount === 0}>
          <Mail className="mr-2 h-4 w-4" />
          Compor Email Marketing
        </Button>
      </CardContent>
    </Card>
  );
}


// ─── Main Page ────────────────────────────────────────────────────────────────

export default function OutreachPage() {
  const searchParams = useSearchParams();
  const router = useRouter();

  // Deep link params (e.g., from Customers/Leads page)
  const urlTab = searchParams.get('tab') as ActiveTab | null;
  const urlContactId = searchParams.get('contactId');
  const urlContactName = searchParams.get('contactName');
  const urlContactPhone = searchParams.get('contactPhone');
  const urlContactEmail = searchParams.get('contactEmail');
  const urlContactCompany = searchParams.get('contactCompany');

  const [activeTab, setActiveTab] = useState<ActiveTab>(urlTab || 'dashboard');

  // Initial contact for WhatsApp pre-fill (from URL deep link)
  const [initialContact, setInitialContact] = useState<ContactOption | null>(() => {
    if (urlContactId && urlContactName && urlContactPhone) {
      return {
        id: urlContactId,
        name: urlContactName,
        whatsApp: urlContactPhone,
        phone: urlContactPhone,
        email: urlContactEmail || '',
        companyName: urlContactCompany || null,
      };
    }
    return null;
  });

  // Clean URL after consuming params (prevent re-fill on refresh)
  useEffect(() => {
    if (urlContactId) {
      router.replace('/outreach', { scroll: false });
    }
  }, []);

  // Dashboard data
  const [dashboard, setDashboard] = useState<OutreachDashboardResponse | null>(null);
  const [dashboardLoading, setDashboardLoading] = useState(true);

  // Config data
  const [config, setConfig] = useState<OutreachConfigResponse | null>(null);
  const [configLoading, setConfigLoading] = useState(true);

  // Ready leads
  const [leads, setLeads] = useState<ReadyLeadResponse[]>([]);
  const [leadsLoading, setLeadsLoading] = useState(false);

  // WhatsApp data
  const [whatsAppStatus, setWhatsAppStatus] = useState<WhatsAppConnectionStatus | null>(null);
  const [whatsAppLeads, setWhatsAppLeads] = useState<WhatsAppReadyLeadResponse[]>([]);
  const [whatsAppLoading, setWhatsAppLoading] = useState(false);

  // Action states
  const [segmenting, setSegmenting] = useState(false);
  const [sending, setSending] = useState(false);
  const [savingConfig, setSavingConfig] = useState(false);
  const [sendingManualWhatsApp, setSendingManualWhatsApp] = useState(false);
  const [sendingWhatsAppCampaign, setSendingWhatsAppCampaign] = useState(false);
  const [sendingWhatsAppFollowUp, setSendingWhatsAppFollowUp] = useState(false);

  // Email Marketing
  const [validEmailCount, setValidEmailCount] = useState<number | null>(null);
  const [composerOpen, setComposerOpen] = useState(false);

  // ── Initial loads ──────────────────────────────────────────────────────────

  const loadDashboard = async () => {
    setDashboardLoading(true);
    try {
      const data = await getOutreachDashboard();
      setDashboard(data);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao carregar o dashboard de outreach.');
    } finally {
      setDashboardLoading(false);
    }
  };

  const loadConfig = async () => {
    setConfigLoading(true);
    try {
      const data = await getOutreachConfig();
      setConfig(data);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao carregar as configurações de outreach.');
    } finally {
      setConfigLoading(false);
    }
  };

  const loadLeads = async () => {
    setLeadsLoading(true);
    try {
      const data = await getReadyLeads(50);
      setLeads(data);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao carregar leads prontos.');
    } finally {
      setLeadsLoading(false);
    }
  };

  const loadWhatsAppData = async () => {
    setWhatsAppLoading(true);
    try {
      const [status, readyLeads] = await Promise.all([
        getWhatsAppStatus(),
        getWhatsAppReadyLeads(),
      ]);
      setWhatsAppStatus(status);
      setWhatsAppLeads(readyLeads);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao carregar dados do WhatsApp.');
    } finally {
      setWhatsAppLoading(false);
    }
  };

  const loadValidEmailCount = async () => {
    try {
      const data = await getValidEmailCount();
      setValidEmailCount(data.count);
    } catch {
      // Silently fail – non-critical
    }
  };

  useEffect(() => {
    loadDashboard();
    loadConfig();
    loadValidEmailCount();
  }, []);

  // Load leads lazily when tab is first opened
  useEffect(() => {
    if (activeTab === 'leads' && leads.length === 0 && !leadsLoading) {
      loadLeads();
    }
  }, [activeTab]);

  // Load WhatsApp data lazily when tab is first opened
  useEffect(() => {
    if (activeTab === 'whatsapp' && !whatsAppStatus && !whatsAppLoading) {
      loadWhatsAppData();
    }
  }, [activeTab]);

  // ── Action Handlers ────────────────────────────────────────────────────────

  const handleSegment = async () => {
    setSegmenting(true);
    try {
      const result = await runSegmentation();
      toast.success(
        `Segmentação concluída: ${result.totalProcessed} leads processados — ${result.hotCount} quentes, ${result.warmCount} mornos, ${result.coldCount} frios.`
      );
      await loadDashboard();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao executar a segmentação.');
    } finally {
      setSegmenting(false);
    }
  };

  const handleSend = async () => {
    setSending(true);
    try {
      const result = await sendOutreachCampaign();
      toast.success(
        `Campanha enfileirada: ${result.queuedCount} emails agendados, ${result.skippedCount} ignorados.`
      );
      await loadDashboard();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao disparar a campanha de outreach.');
    } finally {
      setSending(false);
    }
  };

  const handleSaveConfig = async (data: UpdateOutreachConfigRequest) => {
    setSavingConfig(true);
    try {
      const updated = await updateOutreachConfig(data);
      setConfig(updated);
      toast.success('Configurações salvas com sucesso.');
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao salvar as configurações.');
    } finally {
      setSavingConfig(false);
    }
  };

  const handleSendManualWhatsApp = async (request: { customerId?: string; phoneNumber?: string; message: string }) => {
    setSendingManualWhatsApp(true);
    try {
      const result = await sendWhatsApp(request);
      if (result.success) {
        toast.success('Mensagem WhatsApp enviada com sucesso.');
      } else {
        toast.error(result.error ?? 'Erro ao enviar mensagem WhatsApp.');
      }
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao enviar mensagem WhatsApp.');
    } finally {
      setSendingManualWhatsApp(false);
    }
  };

  const handleSendWhatsAppCampaign = async () => {
    setSendingWhatsAppCampaign(true);
    try {
      const result = await sendWhatsAppCampaign();
      toast.success(
        `Campanha WhatsApp: ${result.sentCount} enviados, ${result.skippedCount} ignorados, ${result.failedCount} falhas.`
      );
      await loadDashboard();
      await loadWhatsAppData();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao enviar campanha WhatsApp.');
    } finally {
      setSendingWhatsAppCampaign(false);
    }
  };

  const handleSendWhatsAppFollowUp = async () => {
    setSendingWhatsAppFollowUp(true);
    try {
      const result = await sendWhatsAppFollowUp();
      toast.success(
        `Follow-up WhatsApp: ${result.sentCount} enviados, ${result.skippedCount} ignorados, ${result.failedCount} falhas.`
      );
      await loadDashboard();
      await loadWhatsAppData();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao enviar follow-up WhatsApp.');
    } finally {
      setSendingWhatsAppFollowUp(false);
    }
  };

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6 p-8 pt-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight font-display text-slate-900">
            Outreach
          </h1>
          <p className="text-slate-500">
            Gerencie a captação, segmentação e envio de emails para seus leads.
          </p>
        </div>
      </div>

      {/* PRO banner */}
      <div className="flex items-center justify-between rounded-lg border border-blue-600 bg-blue-600 px-4 py-3 text-sm text-white">
        <span>O painel de Outreach foi integrado ao <strong>Email Marketing PRO</strong> com 3 providers, smart preselection e suppression list.</span>
        <a
          href="/email-marketing/pro?tab=outreach"
          className="ml-4 shrink-0 rounded-md bg-white px-3 py-1 text-xs font-semibold text-blue-700 hover:bg-blue-50"
        >
          Ir para PRO →
        </a>
      </div>

      {/* Tab Navigation */}
      <TabBar active={activeTab} onChange={setActiveTab} />

      {/* Tab Content */}
      {activeTab === 'dashboard' && (
        <>
          <EmailMarketingSection
            validEmailCount={validEmailCount}
            onComposeClick={() => setComposerOpen(true)}
          />
          <DashboardTab
            dashboard={dashboard}
            loading={dashboardLoading}
            onSegment={handleSegment}
            onSend={handleSend}
            segmenting={segmenting}
            sending={sending}
            onRefresh={loadDashboard}
          />
          <EmailQueueSection />
          <EmailMarketingComposerModal
            open={composerOpen}
            onOpenChange={setComposerOpen}
            validEmailCount={validEmailCount ?? 0}
            onSent={(count) => {
              toast.success(`Email marketing enfileirado para ${count} contatos.`);
              loadDashboard();
              loadValidEmailCount();
            }}
          />
        </>
      )}

      {activeTab === 'configuracao' && (
        <ConfigTab
          config={config}
          loading={configLoading}
          onSave={handleSaveConfig}
          saving={savingConfig}
        />
      )}

      {activeTab === 'templates' && (
        <TemplatesTab
          config={config}
          loading={configLoading}
          onSave={handleSaveConfig}
          saving={savingConfig}
        />
      )}

      {activeTab === 'leads' && (
        <LeadsTab
          leads={leads}
          loading={leadsLoading}
          onRefresh={loadLeads}
        />
      )}

      {activeTab === 'whatsapp' && (
        <WhatsAppTab
          dashboard={dashboard}
          connectionStatus={whatsAppStatus}
          whatsAppLeads={whatsAppLeads}
          loading={whatsAppLoading}
          onRefresh={loadWhatsAppData}
          onSendManual={handleSendManualWhatsApp}
          onSendCampaign={handleSendWhatsAppCampaign}
          onSendFollowUp={handleSendWhatsAppFollowUp}
          sendingManual={sendingManualWhatsApp}
          sendingCampaign={sendingWhatsAppCampaign}
          sendingFollowUp={sendingWhatsAppFollowUp}
          initialContact={initialContact}
          onInitialContactConsumed={() => setInitialContact(null)}
        />
      )}
    </div>
  );
}
