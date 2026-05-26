'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Textarea } from '@/components/ui/textarea';
import {
  AlertTriangle,
  BarChart3,
  ChevronLeft,
  ChevronRight,
  Flame,
  Loader2,
  Mail,
  MessageSquare,
  Phone,
  RefreshCw,
  Search,
  Send,
  Settings,
  Snowflake,
  Thermometer,
  TrendingUp,
  Users,
  X,
} from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
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
import { ContactSearchInput, ContactOption } from '@/components/outreach/ContactSearchInput';
import { WhatsAppTab } from '@/components/outreach/WhatsAppTab';
import {
  ActiveTab,
  getSegmentBadge,
  StatCard,
  StatusIndicator,
  ToggleSwitch,
} from '@/components/outreach/OutreachShared';
import { normalizePhoneBR } from '@/lib/whatsapp-navigation';
import { formatDateShort } from '@/lib/date-utils';
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

// ─── Dashboard Tab ────────────────────────────────────────────────────────────

interface DashboardTabProps {
  dashboard: OutreachDashboardResponse | null;
  loading: boolean;
  onSegment: () => void;
  onSend: () => void;
  segmenting: boolean;
  sending: boolean;
  onRefresh: () => void;
}

function DashboardTab({
  dashboard,
  loading,
  onSegment,
  onSend,
  segmenting,
  sending,
  onRefresh,
}: DashboardTabProps) {
  if (loading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (!dashboard) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <AlertTriangle className="h-10 w-10 mb-3 text-amber-500" />
        <p className="font-medium">Não foi possível carregar o dashboard.</p>
        <Button variant="outline" className="mt-4" onClick={onRefresh}>
          <RefreshCw className="mr-2 h-4 w-4" /> Tentar novamente
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Action Buttons */}
      <div className="flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-3">
          <Button
            onClick={onSegment}
            disabled={segmenting || !dashboard.segmentationEnabled}
            variant="outline"
          >
            {segmenting ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <TrendingUp className="mr-2 h-4 w-4" />
            )}
            {segmenting ? 'Segmentando...' : 'Segmentar Agora'}
          </Button>
          <Button
            onClick={onSend}
            disabled={sending || !dashboard.sendEnabled}
          >
            {sending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Send className="mr-2 h-4 w-4" />
            )}
            {sending ? 'Enviando...' : 'Enviar Campanha'}
          </Button>
        </div>
        <Button variant="ghost" size="sm" onClick={onRefresh}>
          <RefreshCw className="mr-2 h-4 w-4" /> Atualizar
        </Button>
      </div>

      {/* Module Status */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Status dos Módulos</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-6">
          <StatusIndicator enabled={dashboard.importEnabled} label="Importação" />
          <StatusIndicator enabled={dashboard.segmentationEnabled} label="Segmentação" />
          <StatusIndicator enabled={dashboard.sendEnabled} label="Envio" />
          <StatusIndicator enabled={dashboard.whatsAppSendEnabled} label="WhatsApp" />
        </CardContent>
      </Card>

      {/* Lead Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">Leads</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-4">
          <StatCard
            title="Total de Leads"
            value={dashboard.totalLeads}
            icon={<Users className="h-4 w-4 text-slate-600" />}
            accent="bg-slate-100"
          />
          <StatCard
            title="Quentes"
            value={dashboard.hotLeads}
            icon={<Flame className="h-4 w-4 text-red-600" />}
            accent="bg-red-50"
            description="Alto potencial de conversão"
          />
          <StatCard
            title="Mornos"
            value={dashboard.warmLeads}
            icon={<Thermometer className="h-4 w-4 text-orange-600" />}
            accent="bg-orange-50"
            description="Potencial médio"
          />
          <StatCard
            title="Frios"
            value={dashboard.coldLeads}
            icon={<Snowflake className="h-4 w-4 text-blue-600" />}
            accent="bg-blue-50"
            description="Baixo engajamento"
          />
          <StatCard
            title="Sem Segmento"
            value={dashboard.unsegmentedLeads}
            icon={<Users className="h-4 w-4 text-slate-400" />}
            accent="bg-slate-50"
            description="Aguardando segmentação"
          />
        </div>
      </div>

      {/* Email Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">Emails Enviados</h2>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatCard
            title="Hoje"
            value={dashboard.emailsSentToday}
            icon={<Mail className="h-4 w-4 text-indigo-600" />}
            accent="bg-indigo-50"
          />
          <StatCard
            title="Esta Semana"
            value={dashboard.emailsSentThisWeek}
            icon={<Mail className="h-4 w-4 text-indigo-600" />}
            accent="bg-indigo-50"
          />
          <StatCard
            title="Este Mês"
            value={dashboard.emailsSentThisMonth}
            icon={<Mail className="h-4 w-4 text-indigo-600" />}
            accent="bg-indigo-50"
          />
        </div>
      </div>

      {/* WhatsApp Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">WhatsApp</h2>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatCard
            title="Enviados Hoje"
            value={dashboard.whatsAppSentToday}
            icon={<MessageSquare className="h-4 w-4 text-green-600" />}
            accent="bg-green-50"
          />
          <StatCard
            title="Enviados Esta Semana"
            value={dashboard.whatsAppSentThisWeek}
            icon={<MessageSquare className="h-4 w-4 text-green-600" />}
            accent="bg-green-50"
          />
          <StatCard
            title="Leads Prontos (WhatsApp)"
            value={dashboard.whatsAppReadyCount}
            icon={<MessageSquare className="h-4 w-4 text-green-600" />}
            accent="bg-green-50"
          />
        </div>
      </div>

      {/* Queue Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">Fila de Envio</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <StatCard
            title="Aguardando"
            value={dashboard.pendingInQueue}
            icon={<Send className="h-4 w-4 text-yellow-600" />}
            accent="bg-yellow-50"
            description="Na fila para envio"
          />
          <StatCard
            title="Falhas"
            value={dashboard.failedInQueue}
            icon={<AlertTriangle className="h-4 w-4 text-red-600" />}
            accent="bg-red-50"
            description="Envios com erro"
          />
        </div>
      </div>
    </div>
  );
}

// ─── Configuration Tab ────────────────────────────────────────────────────────

interface ConfigTabProps {
  config: OutreachConfigResponse | null;
  loading: boolean;
  onSave: (data: UpdateOutreachConfigRequest) => void;
  saving: boolean;
}

function ConfigTab({ config, loading, onSave, saving }: ConfigTabProps) {
  const [apifyDatasetUrl, setApifyDatasetUrl] = useState('');
  const [apifyApiToken, setApifyApiToken] = useState('');
  const [importEnabled, setImportEnabled] = useState(false);
  const [segmentationEnabled, setSegmentationEnabled] = useState(false);
  const [sendEnabled, setSendEnabled] = useState(false);
  const [dailyEmailLimit, setDailyEmailLimit] = useState(50);
  const [emailCooldownDays, setEmailCooldownDays] = useState(7);
  const [whatsAppSendEnabled, setWhatsAppSendEnabled] = useState(false);
  const [dailyWhatsAppLimit, setDailyWhatsAppLimit] = useState(50);
  const [whatsAppCooldownDays, setWhatsAppCooldownDays] = useState(7);

  // Sync local state when config loads
  useEffect(() => {
    if (!config) return;
    setApifyDatasetUrl(config.apifyDatasetUrl ?? '');
    setApifyApiToken(config.apifyApiToken ?? '');
    setImportEnabled(config.importEnabled);
    setSegmentationEnabled(config.segmentationEnabled);
    setSendEnabled(config.sendEnabled);
    setDailyEmailLimit(config.dailyEmailLimit);
    setEmailCooldownDays(config.emailCooldownDays);
    setWhatsAppSendEnabled(config.whatsAppSendEnabled);
    setDailyWhatsAppLimit(config.dailyWhatsAppLimit);
    setWhatsAppCooldownDays(config.whatsAppCooldownDays);
  }, [config]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave({
      apifyDatasetUrl: apifyDatasetUrl || undefined,
      apifyApiToken: apifyApiToken || undefined,
      importEnabled,
      segmentationEnabled,
      sendEnabled,
      dailyEmailLimit,
      emailCooldownDays,
      whatsAppSendEnabled,
      dailyWhatsAppLimit,
      whatsAppCooldownDays,
    });
  };

  if (loading) {
    return (
      <div className="space-y-4 max-w-2xl">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} className="h-12 rounded-lg" />
        ))}
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6 max-w-2xl">
      {/* Apify Integration */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Integração Apify</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="apifyDatasetUrl">URL do Dataset Apify</Label>
            <Input
              id="apifyDatasetUrl"
              value={apifyDatasetUrl}
              onChange={(e) => setApifyDatasetUrl(e.target.value)}
              placeholder="https://api.apify.com/v2/datasets/..."
            />
            <p className="text-xs text-slate-500">
              URL do dataset de onde os leads serão importados.
            </p>
          </div>
          <div className="space-y-2">
            <Label htmlFor="apifyApiToken">Token de API Apify</Label>
            <Input
              id="apifyApiToken"
              type="password"
              autoComplete="off"
              value={apifyApiToken}
              onChange={(e) => setApifyApiToken(e.target.value)}
              placeholder="apify_api_..."
            />
            <p className="text-xs text-slate-500">
              Token de autenticação para a API do Apify.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Module Toggles */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Módulos</CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="importEnabled" className="text-sm font-medium text-slate-700">
                Importação Automática
              </Label>
              <p className="text-xs text-slate-500 mt-0.5">
                Importar leads automaticamente do Apify.
              </p>
            </div>
            <ToggleSwitch
              id="importEnabled"
              checked={importEnabled}
              onCheckedChange={setImportEnabled}
            />
          </div>
          <div className="border-t border-slate-100" />
          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="segmentationEnabled" className="text-sm font-medium text-slate-700">
                Segmentação Automática
              </Label>
              <p className="text-xs text-slate-500 mt-0.5">
                Classificar leads em Quente, Morno e Frio.
              </p>
            </div>
            <ToggleSwitch
              id="segmentationEnabled"
              checked={segmentationEnabled}
              onCheckedChange={setSegmentationEnabled}
            />
          </div>
          <div className="border-t border-slate-100" />
          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="sendEnabled" className="text-sm font-medium text-slate-700">
                Envio de Emails
              </Label>
              <p className="text-xs text-slate-500 mt-0.5">
                Habilitar o disparo de emails para os leads.
              </p>
            </div>
            <ToggleSwitch
              id="sendEnabled"
              checked={sendEnabled}
              onCheckedChange={setSendEnabled}
            />
          </div>
          <div className="border-t border-slate-100" />
          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="whatsAppSendEnabled" className="text-sm font-medium text-slate-700">
                Envio de WhatsApp
              </Label>
              <p className="text-xs text-slate-500 mt-0.5">
                Habilitar o disparo de mensagens via WhatsApp para os leads.
              </p>
            </div>
            <ToggleSwitch
              id="whatsAppSendEnabled"
              checked={whatsAppSendEnabled}
              onCheckedChange={setWhatsAppSendEnabled}
            />
          </div>
        </CardContent>
      </Card>

      {/* Sending Limits */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Limites de Envio</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="dailyEmailLimit">Limite Diário de Emails</Label>
              <Input
                id="dailyEmailLimit"
                type="number"
                min={1}
                max={10000}
                value={dailyEmailLimit}
                onChange={(e) => setDailyEmailLimit(Number(e.target.value))}
              />
              <p className="text-xs text-slate-500">Máximo de emails enviados por dia.</p>
            </div>
            <div className="space-y-2">
              <Label htmlFor="emailCooldownDays">Cooldown Email (dias)</Label>
              <Input
                id="emailCooldownDays"
                type="number"
                min={0}
                max={365}
                value={emailCooldownDays}
                onChange={(e) => setEmailCooldownDays(Number(e.target.value))}
              />
              <p className="text-xs text-slate-500">
                Dias mínimos entre emails para o mesmo lead.
              </p>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="dailyWhatsAppLimit">Limite Diário de WhatsApp</Label>
              <Input
                id="dailyWhatsAppLimit"
                type="number"
                min={1}
                max={10000}
                value={dailyWhatsAppLimit}
                onChange={(e) => setDailyWhatsAppLimit(Number(e.target.value))}
              />
              <p className="text-xs text-slate-500">Máximo de mensagens WhatsApp enviadas por dia.</p>
            </div>
            <div className="space-y-2">
              <Label htmlFor="whatsAppCooldownDays">Cooldown WhatsApp (dias)</Label>
              <Input
                id="whatsAppCooldownDays"
                type="number"
                min={0}
                max={365}
                value={whatsAppCooldownDays}
                onChange={(e) => setWhatsAppCooldownDays(Number(e.target.value))}
              />
              <p className="text-xs text-slate-500">
                Dias mínimos entre mensagens WhatsApp para o mesmo lead.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button type="submit" disabled={saving}>
          {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {saving ? 'Salvando...' : 'Salvar Configurações'}
        </Button>
      </div>
    </form>
  );
}

// ─── Templates Tab ────────────────────────────────────────────────────────────

interface TemplatesTabProps {
  config: OutreachConfigResponse | null;
  loading: boolean;
  onSave: (data: UpdateOutreachConfigRequest) => void;
  saving: boolean;
}

const PLACEHOLDERS = ['{{nome}}', '{{empresa}}', '{{email}}', '{{website}}'];

interface TemplateEditorProps {
  title: string;
  colorClass: string;
  icon: React.ReactNode;
  subject: string;
  body: string;
  onSubjectChange: (v: string) => void;
  onBodyChange: (v: string) => void;
}

function TemplateEditor({
  title,
  colorClass,
  icon,
  subject,
  body,
  onSubjectChange,
  onBodyChange,
}: TemplateEditorProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className={`text-base flex items-center gap-2 ${colorClass}`}>
          {icon}
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label>Assunto</Label>
          <Input
            value={subject}
            onChange={(e) => onSubjectChange(e.target.value)}
            placeholder={`Assunto do email para lead ${title.toLowerCase()}...`}
          />
        </div>
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label>Corpo do Email (HTML)</Label>
            <div className="flex flex-wrap gap-1">
              {PLACEHOLDERS.map((p) => (
                <code
                  key={p}
                  className="text-xs bg-slate-100 text-slate-600 px-1.5 py-0.5 rounded font-mono cursor-help"
                  title="Variável disponível"
                >
                  {p}
                </code>
              ))}
            </div>
          </div>
          <Textarea
            value={body}
            onChange={(e) => onBodyChange(e.target.value)}
            placeholder="<p>Olá {{nome}}, ...</p>"
            className="min-h-[180px] font-mono text-sm"
          />
        </div>
      </CardContent>
    </Card>
  );
}

interface WhatsAppTemplateEditorProps {
  title: string;
  colorClass: string;
  icon: React.ReactNode;
  body: string;
  onBodyChange: (v: string) => void;
}

function WhatsAppTemplateEditor({
  title,
  colorClass,
  icon,
  body,
  onBodyChange,
}: WhatsAppTemplateEditorProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className={`text-base flex items-center gap-2 ${colorClass}`}>
          {icon}
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label>Mensagem WhatsApp</Label>
            <div className="flex flex-wrap gap-1">
              {PLACEHOLDERS.map((p) => (
                <code
                  key={p}
                  className="text-xs bg-slate-100 text-slate-600 px-1.5 py-0.5 rounded font-mono cursor-help"
                  title="Variável disponível"
                >
                  {p}
                </code>
              ))}
            </div>
          </div>
          <Textarea
            value={body}
            onChange={(e) => onBodyChange(e.target.value)}
            placeholder="Olá {{nome}}, ..."
            className="min-h-[150px] text-sm"
          />
        </div>
      </CardContent>
    </Card>
  );
}

function TemplatesTab({ config, loading, onSave, saving }: TemplatesTabProps) {
  const [hotSubject, setHotSubject] = useState('');
  const [hotBody, setHotBody] = useState('');
  const [warmSubject, setWarmSubject] = useState('');
  const [warmBody, setWarmBody] = useState('');
  const [coldSubject, setColdSubject] = useState('');
  const [coldBody, setColdBody] = useState('');
  const [whatsAppHotTemplate, setWhatsAppHotTemplate] = useState('');
  const [whatsAppWarmTemplate, setWhatsAppWarmTemplate] = useState('');
  const [whatsAppColdTemplate, setWhatsAppColdTemplate] = useState('');
  const [whatsAppFollowUpTemplate, setWhatsAppFollowUpTemplate] = useState('');

  useEffect(() => {
    if (!config) return;
    setHotSubject(config.hotTemplateSubject ?? '');
    setHotBody(config.hotTemplateBody ?? '');
    setWarmSubject(config.warmTemplateSubject ?? '');
    setWarmBody(config.warmTemplateBody ?? '');
    setColdSubject(config.coldTemplateSubject ?? '');
    setColdBody(config.coldTemplateBody ?? '');
    setWhatsAppHotTemplate(config.whatsAppHotTemplate ?? '');
    setWhatsAppWarmTemplate(config.whatsAppWarmTemplate ?? '');
    setWhatsAppColdTemplate(config.whatsAppColdTemplate ?? '');
    setWhatsAppFollowUpTemplate(config.whatsAppFollowUpTemplate ?? '');
  }, [config]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave({
      hotTemplateSubject: hotSubject || undefined,
      hotTemplateBody: hotBody || undefined,
      warmTemplateSubject: warmSubject || undefined,
      warmTemplateBody: warmBody || undefined,
      coldTemplateSubject: coldSubject || undefined,
      coldTemplateBody: coldBody || undefined,
      whatsAppHotTemplate: whatsAppHotTemplate || undefined,
      whatsAppWarmTemplate: whatsAppWarmTemplate || undefined,
      whatsAppColdTemplate: whatsAppColdTemplate || undefined,
      whatsAppFollowUpTemplate: whatsAppFollowUpTemplate || undefined,
    });
  };

  if (loading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-64 rounded-xl" />
        ))}
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Email Templates */}
      <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide">Templates de Email</h2>
      <TemplateEditor
        title="Quente"
        colorClass="text-red-600"
        icon={<Flame className="h-4 w-4" />}
        subject={hotSubject}
        body={hotBody}
        onSubjectChange={setHotSubject}
        onBodyChange={setHotBody}
      />
      <TemplateEditor
        title="Morno"
        colorClass="text-orange-600"
        icon={<Thermometer className="h-4 w-4" />}
        subject={warmSubject}
        body={warmBody}
        onSubjectChange={setWarmSubject}
        onBodyChange={setWarmBody}
      />
      <TemplateEditor
        title="Frio"
        colorClass="text-blue-600"
        icon={<Snowflake className="h-4 w-4" />}
        subject={coldSubject}
        body={coldBody}
        onSubjectChange={setColdSubject}
        onBodyChange={setColdBody}
      />

      {/* WhatsApp Templates */}
      <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide pt-4">Templates de WhatsApp</h2>
      <WhatsAppTemplateEditor
        title="WhatsApp Quente"
        colorClass="text-red-600"
        icon={<Flame className="h-4 w-4" />}
        body={whatsAppHotTemplate}
        onBodyChange={setWhatsAppHotTemplate}
      />
      <WhatsAppTemplateEditor
        title="WhatsApp Morno"
        colorClass="text-orange-600"
        icon={<Thermometer className="h-4 w-4" />}
        body={whatsAppWarmTemplate}
        onBodyChange={setWhatsAppWarmTemplate}
      />
      <WhatsAppTemplateEditor
        title="WhatsApp Frio"
        colorClass="text-blue-600"
        icon={<Snowflake className="h-4 w-4" />}
        body={whatsAppColdTemplate}
        onBodyChange={setWhatsAppColdTemplate}
      />
      <WhatsAppTemplateEditor
        title="WhatsApp Follow-Up"
        colorClass="text-purple-600"
        icon={<MessageSquare className="h-4 w-4" />}
        body={whatsAppFollowUpTemplate}
        onBodyChange={setWhatsAppFollowUpTemplate}
      />

      <div className="flex justify-end">
        <Button type="submit" disabled={saving}>
          {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {saving ? 'Salvando...' : 'Salvar Templates'}
        </Button>
      </div>
    </form>
  );
}

// ─── Ready Leads Tab ──────────────────────────────────────────────────────────

interface LeadsTabProps {
  leads: ReadyLeadResponse[];
  loading: boolean;
  onRefresh: () => void;
}

type LeadSortKey = 'name' | 'email' | 'segment' | 'leadScore' | 'companyName' | 'lastEmailSentAt';
const SEGMENT_ORDER: Record<string, number> = { hot: 3, warm: 2, cold: 1 };

function LeadsTab({ leads, loading, onRefresh }: LeadsTabProps) {
  const router = useRouter();
  const [search, setSearch]       = useState('');
  const [segFilter, setSegFilter] = useState<string>('');
  const [sortKey, setSortKey]     = useState<LeadSortKey>('leadScore');
  const [sortDir, setSortDir]     = useState<'asc' | 'desc'>('desc');
  const [page, setPage]           = useState(1);
  const PAGE_SIZE = 25;

  const toggleSort = (key: LeadSortKey) => {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    else { setSortKey(key); setSortDir('desc'); }
  };

  const filtered = leads
    .filter(l => {
      const q = search.toLowerCase();
      const matchSearch = !q || l.name.toLowerCase().includes(q) || l.email.toLowerCase().includes(q) || (l.companyName ?? '').toLowerCase().includes(q);
      const matchSeg = !segFilter || l.segment.toLowerCase() === segFilter;
      return matchSearch && matchSeg;
    })
    .sort((a, b) => {
      let va: string | number = '';
      let vb: string | number = '';
      if (sortKey === 'name')           { va = a.name; vb = b.name; }
      else if (sortKey === 'email')     { va = a.email; vb = b.email; }
      else if (sortKey === 'segment')   { va = SEGMENT_ORDER[a.segment.toLowerCase()] ?? 0; vb = SEGMENT_ORDER[b.segment.toLowerCase()] ?? 0; }
      else if (sortKey === 'leadScore') { va = a.leadScore; vb = b.leadScore; }
      else if (sortKey === 'companyName') { va = a.companyName ?? ''; vb = b.companyName ?? ''; }
      else if (sortKey === 'lastEmailSentAt') { va = a.lastEmailSentAt ?? ''; vb = b.lastEmailSentAt ?? ''; }
      if (va < vb) return sortDir === 'asc' ? -1 : 1;
      if (va > vb) return sortDir === 'asc' ? 1 : -1;
      return 0;
    });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const firstItem  = filtered.length > 0 ? (page - 1) * PAGE_SIZE + 1 : 0;
  const lastItem   = Math.min(page * PAGE_SIZE, filtered.length);

  const LeadColHead = ({ col, label, className = '' }: { col: LeadSortKey; label: string; className?: string }) => (
    <TableHead
      className={`cursor-pointer select-none whitespace-nowrap hover:bg-slate-50 transition-colors ${className}`}
      onClick={() => { toggleSort(col); setPage(1); }}
    >
      <span className="flex items-center gap-0.5">
        {label}
        {sortKey === col
          ? <span className="ml-1 text-[10px] text-slate-700">{sortDir === 'asc' ? '↑' : '↓'}</span>
          : <span className="ml-1 opacity-20 text-[10px]">↕</span>}
      </span>
    </TableHead>
  );

  if (loading) {
    return (
      <Card>
        <CardContent className="p-4 space-y-2">
          {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-10 w-full rounded" />)}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      {/* ── Header ── */}
      <CardHeader className="pb-2">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <CardTitle className="text-base flex items-center gap-2">
              <Users className="h-4 w-4 text-slate-500" />
              Leads Prontos
            </CardTitle>
            <Badge variant="secondary" className="font-normal tabular-nums">
              {filtered.length !== leads.length
                ? `${filtered.length} de ${leads.length}`
                : leads.length}
            </Badge>
          </div>
          <div className="flex items-center gap-2">
            <div className="relative">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Nome, email ou empresa…"
                className="h-8 pl-8 w-48 text-sm"
                value={search}
                onChange={e => { setSearch(e.target.value); setPage(1); }}
              />
              {search && (
                <button className="absolute right-2 top-2 text-muted-foreground hover:text-foreground" onClick={() => { setSearch(''); setPage(1); }}>
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
            <Button variant="outline" size="sm" className="h-8" onClick={onRefresh}>
              <RefreshCw className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
        {/* Segment filter chips */}
        <div className="flex gap-1.5 pt-1 flex-wrap">
          {[
            { value: '',     label: 'Todos'   },
            { value: 'hot',  label: '🔥 Hot'  },
            { value: 'warm', label: '☀️ Warm' },
            { value: 'cold', label: '🧊 Cold' },
          ].map(opt => (
            <button
              key={opt.value}
              onClick={() => { setSegFilter(opt.value); setPage(1); }}
              className={`rounded-full px-3 py-0.5 text-xs font-medium border transition-colors ${
                segFilter === opt.value
                  ? 'bg-slate-900 text-white border-slate-900'
                  : 'bg-background text-slate-500 border-slate-200 hover:border-slate-400 hover:text-slate-700'
              }`}
            >
              {opt.label}
            </button>
          ))}
          {(search || segFilter) && (
            <button
              onClick={() => { setSearch(''); setSegFilter(''); setPage(1); }}
              className="flex items-center gap-1 rounded-full px-3 py-0.5 text-xs border border-border text-muted-foreground hover:bg-muted ml-1"
            >
              <X className="h-3 w-3" /> Limpar
            </button>
          )}
        </div>
      </CardHeader>

      <CardContent className="p-0">
        {leads.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-slate-400">
            <Users className="h-10 w-10 mb-3 opacity-20" />
            <p className="font-medium text-slate-500 text-sm">Nenhum lead disponível</p>
            <p className="text-xs text-slate-400 mt-1">Execute a segmentação para classificar os leads.</p>
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-slate-400">
            <Search className="h-8 w-8 mb-2 opacity-20" />
            <p className="text-sm">Nenhum resultado para os filtros aplicados.</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow className="bg-slate-50/60">
                    <LeadColHead col="name"            label="Nome"         className="pl-5 w-[180px]" />
                    <LeadColHead col="email"           label="Email"        className="w-[200px]" />
                    <LeadColHead col="segment"         label="Segmento"     className="w-[110px]" />
                    <LeadColHead col="leadScore"       label="Score"        className="w-[80px] text-right" />
                    <LeadColHead col="companyName"     label="Empresa"      className="w-[160px]" />
                    <LeadColHead col="lastEmailSentAt" label="Último Envio" className="w-[150px]" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {paginated.map(lead => (
                    <TableRow
                      key={lead.id}
                      className="cursor-pointer hover:bg-slate-50/60 transition-colors"
                      onClick={() => router.push(`/leads?search=${encodeURIComponent(lead.name)}`)}
                    >
                      <TableCell className="pl-5 font-medium text-blue-700 hover:underline text-sm truncate max-w-[180px]" title={lead.name}>
                        {lead.name}
                      </TableCell>
                      <TableCell className="text-slate-500 text-xs truncate max-w-[200px]" title={lead.email}>
                        {lead.email}
                      </TableCell>
                      <TableCell>{getSegmentBadge(lead.segment)}</TableCell>
                      <TableCell className="text-right pr-5">
                        <span className={`font-bold tabular-nums text-sm ${lead.leadScore >= 70 ? 'text-green-600' : lead.leadScore >= 40 ? 'text-amber-600' : 'text-slate-500'}`}>
                          {lead.leadScore}
                        </span>
                      </TableCell>
                      <TableCell className="text-slate-500 text-xs truncate max-w-[160px]" title={lead.companyName ?? ''}>
                        {lead.companyName ?? <span className="text-slate-300">—</span>}
                      </TableCell>
                      <TableCell className="text-slate-400 text-xs whitespace-nowrap">
                        {lead.lastEmailSentAt ? formatDateShort(lead.lastEmailSentAt) : <span className="text-slate-300">Nunca</span>}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between border-t px-5 py-3">
                <p className="text-xs text-slate-500">
                  Mostrando {firstItem}–{lastItem} de {filtered.length} leads
                </p>
                <div className="flex items-center gap-1">
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(1)}             disabled={page === 1}>«</Button>
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(p => p - 1)}    disabled={page === 1}><ChevronLeft className="h-3.5 w-3.5" /></Button>
                  <span className="px-3 text-xs text-slate-600 whitespace-nowrap">{page} / {totalPages}</span>
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(p => p + 1)}    disabled={page >= totalPages}><ChevronRight className="h-3.5 w-3.5" /></Button>
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(totalPages)}    disabled={page >= totalPages}>»</Button>
                </div>
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
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
