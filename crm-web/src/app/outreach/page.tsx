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
  getEmailQueue,
  EmailQueueItemResponse,
  EmailQueueStatus,
  PagedEmailQueueResponse,
  getValidEmailCount,
} from '@/services/outreach';
import { EmailMarketingComposerModal } from '@/components/email/EmailMarketingComposerModal';
import { searchContacts, Customer } from '@/services/customers';
import { normalizePhoneBR } from '@/lib/whatsapp-navigation';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { useRouter, useSearchParams } from 'next/navigation';

// ─── Types ──────────────────────────────────────────────────────────────────

type ActiveTab = 'dashboard' | 'configuracao' | 'templates' | 'leads' | 'whatsapp';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function formatDateShort(dateStr: string | null): string {
  if (!dateStr) return '–';
  try {
    return format(parseISO(dateStr), "dd/MM/yyyy 'às' HH:mm", { locale: ptBR });
  } catch {
    return dateStr;
  }
}

function getSegmentBadge(segment: string) {
  const lower = segment.toLowerCase();
  if (lower === 'hot') {
    return (
      <Badge className="bg-red-100 text-red-700 hover:bg-red-100 flex items-center gap-1 w-fit">
        <Flame className="h-3 w-3" />
        Quente
      </Badge>
    );
  }
  if (lower === 'warm') {
    return (
      <Badge className="bg-orange-100 text-orange-700 hover:bg-orange-100 flex items-center gap-1 w-fit">
        <Thermometer className="h-3 w-3" />
        Morno
      </Badge>
    );
  }
  return (
    <Badge className="bg-blue-100 text-blue-700 hover:bg-blue-100 flex items-center gap-1 w-fit">
      <Snowflake className="h-3 w-3" />
      Frio
    </Badge>
  );
}

// ─── Custom Toggle / Switch ───────────────────────────────────────────────────

interface ToggleSwitchProps {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  id?: string;
}

function ToggleSwitch({ checked, onCheckedChange, disabled, id }: ToggleSwitchProps) {
  return (
    <button
      id={id}
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onCheckedChange(!checked)}
      className={[
        'relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
        checked ? 'bg-slate-900' : 'bg-slate-200',
      ].join(' ')}
    >
      <span
        className={[
          'pointer-events-none block h-5 w-5 rounded-full bg-white shadow-lg ring-0 transition-transform',
          checked ? 'translate-x-5' : 'translate-x-0',
        ].join(' ')}
      />
    </button>
  );
}

// ─── Stat Card ────────────────────────────────────────────────────────────────

interface StatCardProps {
  title: string;
  value: number | string;
  icon: React.ReactNode;
  description?: string;
  accent?: string;
}

function StatCard({ title, value, icon, description, accent }: StatCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-slate-600">{title}</CardTitle>
        <span className={`h-8 w-8 rounded-md flex items-center justify-center ${accent ?? 'bg-slate-100'}`}>
          {icon}
        </span>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-slate-900">{value}</div>
        {description && (
          <p className="text-xs text-slate-500 mt-1">{description}</p>
        )}
      </CardContent>
    </Card>
  );
}

// ─── Status Indicator ─────────────────────────────────────────────────────────

function StatusIndicator({ enabled, label }: { enabled: boolean; label: string }) {
  return (
    <div className="flex items-center gap-2">
      <span
        className={`inline-block h-2 w-2 rounded-full ${enabled ? 'bg-green-500' : 'bg-slate-300'}`}
      />
      <span className="text-sm text-slate-700">{label}</span>
      <span className={`text-xs font-medium ${enabled ? 'text-green-600' : 'text-slate-400'}`}>
        {enabled ? 'Ativo' : 'Inativo'}
      </span>
    </div>
  );
}

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

function LeadsTab({ leads, loading, onRefresh }: LeadsTabProps) {
  const router = useRouter();
  if (loading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 8 }).map((_, i) => (
          <Skeleton key={i} className="h-12 rounded-lg" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-slate-500">
          {leads.length === 0
            ? 'Nenhum lead pronto para outreach.'
            : `${leads.length} lead${leads.length !== 1 ? 's' : ''} prontos para contato.`}
        </p>
        <Button variant="outline" size="sm" onClick={onRefresh}>
          <RefreshCw className="mr-2 h-4 w-4" /> Atualizar
        </Button>
      </div>

      {leads.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-16 text-slate-400">
            <Users className="h-10 w-10 mb-3" />
            <p className="font-medium text-slate-500">Nenhum lead disponível</p>
            <p className="text-sm mt-1">
              Execute a segmentação para classificar os leads e prepará-los para o envio.
            </p>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Segmento</TableHead>
                  <TableHead className="text-right">Score</TableHead>
                  <TableHead>Empresa</TableHead>
                  <TableHead>Último Envio</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {leads.map((lead) => (
                  <TableRow
                    key={lead.id}
                    className="cursor-pointer hover:bg-slate-50 transition-colors"
                    onClick={() => router.push(`/leads?search=${encodeURIComponent(lead.name)}`)}
                  >
                    <TableCell className="font-medium text-blue-700 hover:text-blue-900 hover:underline">
                      {lead.name}
                    </TableCell>
                    <TableCell className="text-slate-600 text-sm">
                      {lead.email}
                    </TableCell>
                    <TableCell>{getSegmentBadge(lead.segment)}</TableCell>
                    <TableCell className="text-right">
                      <span className="font-semibold text-slate-700">{lead.leadScore}</span>
                    </TableCell>
                    <TableCell className="text-slate-600 text-sm">
                      {lead.companyName ?? '–'}
                    </TableCell>
                    <TableCell className="text-slate-500 text-xs">
                      {formatDateShort(lead.lastEmailSentAt)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
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

// ─── Email Queue Section ──────────────────────────────────────────────────────

const emailQueueStatusConfig: Record<
  EmailQueueStatus,
  { label: string; className: string }
> = {
  [EmailQueueStatus.Queued]: {
    label: 'Na fila',
    className: 'bg-yellow-100 text-yellow-700 hover:bg-yellow-100',
  },
  [EmailQueueStatus.Processing]: {
    label: 'Processando',
    className: 'bg-blue-100 text-blue-700 hover:bg-blue-100',
  },
  [EmailQueueStatus.Sent]: {
    label: 'Enviado',
    className: 'bg-green-100 text-green-700 hover:bg-green-100',
  },
  [EmailQueueStatus.Failed]: {
    label: 'Falhou',
    className: 'bg-red-100 text-red-700 hover:bg-red-100',
  },
};

function EmailQueueSection() {
  const [queue, setQueue] = useState<PagedEmailQueueResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);

  const loadQueue = useCallback(async (p = 1) => {
    setLoading(true);
    try {
      const data = await getEmailQueue(p, 15);
      setQueue(data);
      setPage(p);
    } catch {
      toast.error('Erro ao carregar fila de emails.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadQueue();
  }, [loadQueue]);

  if (loading && !queue) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Histórico de Envios
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-10 w-full" />
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!queue || queue.totalCount === 0) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Histórico de Envios
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-slate-500">Nenhum email na fila de envio.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Histórico de Envios
            <Badge variant="secondary" className="ml-1 font-normal">
              {queue.totalCount}
            </Badge>
          </CardTitle>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => loadQueue(page)}
            disabled={loading}
          >
            {loading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4" />
            )}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Destinatário</TableHead>
                <TableHead>Assunto</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Agendado</TableHead>
                <TableHead>Enviado</TableHead>
                <TableHead className="text-right">Tentativas</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {queue.items.map((item) => {
                const statusCfg =
                  emailQueueStatusConfig[item.status] ??
                  emailQueueStatusConfig[EmailQueueStatus.Queued];
                return (
                  <TableRow key={item.id}>
                    <TableCell>
                      <div className="min-w-0">
                        <div className="font-medium text-slate-900 truncate max-w-[180px]">
                          {item.recipientName}
                        </div>
                        <div className="text-xs text-slate-500 truncate max-w-[180px]">
                          {item.recipientEmail}
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-slate-700 truncate block max-w-[220px]">
                        {item.subject}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Badge className={statusCfg.className}>
                        {statusCfg.label}
                      </Badge>
                      {item.status === EmailQueueStatus.Failed && item.lastError && (
                        <p className="text-xs text-red-500 mt-1 max-w-[160px] truncate" title={item.lastError}>
                          {item.lastError}
                        </p>
                      )}
                    </TableCell>
                    <TableCell className="text-xs text-slate-500 whitespace-nowrap">
                      {formatDateShort(item.scheduledAt)}
                    </TableCell>
                    <TableCell className="text-xs text-slate-500 whitespace-nowrap">
                      {item.sentAt ? formatDateShort(item.sentAt) : '–'}
                    </TableCell>
                    <TableCell className="text-right">
                      <span className="text-sm font-medium text-slate-700">
                        {item.attemptCount}
                      </span>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>

        {/* Pagination */}
        {queue.totalPages > 1 && (
          <div className="flex items-center justify-between mt-4">
            <span className="text-sm text-slate-500">
              Página {queue.page} de {queue.totalPages} ({queue.totalCount} emails)
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => loadQueue(page - 1)}
                disabled={!queue.hasPreviousPage || loading}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => loadQueue(page + 1)}
                disabled={!queue.hasNextPage || loading}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

// ─── Contact Search Autocomplete ──────────────────────────────────────────────

interface ContactOption {
  id: string;
  name: string;
  whatsApp: string | null;
  phone: string | null;
  email: string;
  companyName: string | null;
}

function ContactSearchInput({
  onSelect,
  selectedContact,
  onClear,
}: {
  onSelect: (contact: ContactOption) => void;
  selectedContact: ContactOption | null;
  onClear: () => void;
}) {
  const [searchText, setSearchText] = useState('');
  const [results, setResults] = useState<ContactOption[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [searching, setSearching] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Debounced search
  useEffect(() => {
    if (searchText.length < 2) {
      setResults([]);
      setIsOpen(false);
      return;
    }

    const timer = setTimeout(async () => {
      setSearching(true);
      try {
        const res = await searchContacts(searchText, 10);
        const contacts: ContactOption[] = res.items.map((c: Customer) => ({
          id: c.id,
          name: c.name,
          whatsApp: c.whatsApp ?? null,
          phone: c.phone ?? null,
          email: c.email,
          companyName: c.companyName ?? null,
        }));
        setResults(contacts);
        setIsOpen(contacts.length > 0);
      } catch {
        setResults([]);
      } finally {
        setSearching(false);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [searchText]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  if (selectedContact) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 border rounded-md bg-slate-50">
        <div className="flex-1 min-w-0">
          <span className="font-medium text-slate-900">{selectedContact.name}</span>
          {selectedContact.companyName && (
            <span className="text-xs text-slate-400 ml-2">({selectedContact.companyName})</span>
          )}
          <span className="text-sm text-green-600 ml-2 font-mono">
            {normalizePhoneBR(selectedContact.whatsApp ?? selectedContact.phone) || selectedContact.email}
          </span>
        </div>
        <button
          type="button"
          onClick={onClear}
          className="p-1 hover:bg-slate-200 rounded-md transition-colors"
          title="Limpar seleção"
        >
          <X className="h-4 w-4 text-slate-500" />
        </button>
      </div>
    );
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
        <Input
          value={searchText}
          onChange={(e) => {
            setSearchText(e.target.value);
            if (e.target.value.length >= 2) setIsOpen(true);
          }}
          onFocus={() => results.length > 0 && setIsOpen(true)}
          placeholder="Buscar por nome, email ou empresa..."
          className="pl-9"
        />
        {searching && (
          <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 animate-spin text-slate-400" />
        )}
      </div>
      {isOpen && results.length > 0 && (
        <div className="absolute z-50 w-full mt-1 bg-white border border-slate-200 rounded-md shadow-lg max-h-64 overflow-y-auto">
          {results.map((contact) => (
            <button
              key={contact.id}
              type="button"
              className="w-full text-left px-3 py-2.5 hover:bg-slate-50 flex items-center justify-between border-b border-slate-100 last:border-0 transition-colors"
              onClick={() => {
                onSelect(contact);
                setSearchText('');
                setIsOpen(false);
              }}
            >
              <div className="min-w-0">
                <span className="font-medium text-slate-900">{contact.name}</span>
                {contact.companyName && (
                  <span className="text-xs text-slate-400 ml-2">({contact.companyName})</span>
                )}
              </div>
              <span className="text-sm text-green-600 ml-3 font-mono shrink-0">
                {normalizePhoneBR(contact.whatsApp ?? contact.phone) || '–'}
              </span>
            </button>
          ))}
        </div>
      )}
      {isOpen && searchText.length >= 2 && results.length === 0 && !searching && (
        <div className="absolute z-50 w-full mt-1 bg-white border border-slate-200 rounded-md shadow-lg p-3 text-sm text-slate-500 text-center">
          Nenhum contato encontrado.
        </div>
      )}
    </div>
  );
}

// ─── WhatsApp Tab ─────────────────────────────────────────────────────────────

interface WhatsAppTabProps {
  dashboard: OutreachDashboardResponse | null;
  connectionStatus: WhatsAppConnectionStatus | null;
  whatsAppLeads: WhatsAppReadyLeadResponse[];
  loading: boolean;
  onRefresh: () => void;
  onSendManual: (request: { customerId?: string; phoneNumber?: string; message: string }) => void;
  onSendCampaign: () => void;
  onSendFollowUp: () => void;
  sendingManual: boolean;
  sendingCampaign: boolean;
  sendingFollowUp: boolean;
  initialContact?: ContactOption | null;
  onInitialContactConsumed?: () => void;
}

function WhatsAppTab({
  dashboard,
  connectionStatus,
  whatsAppLeads,
  loading,
  onRefresh,
  onSendManual,
  onSendCampaign,
  onSendFollowUp,
  sendingManual,
  sendingCampaign,
  sendingFollowUp,
  initialContact,
  onInitialContactConsumed,
}: WhatsAppTabProps) {
  const router = useRouter();
  const [selectedContact, setSelectedContact] = useState<ContactOption | null>(null);
  const [manualPhoneNumber, setManualPhoneNumber] = useState('');
  const [manualMessage, setManualMessage] = useState('');
  const formRef = useRef<HTMLDivElement>(null);

  // Pre-fill from deep link (Customers/Leads page → outreach WhatsApp tab)
  useEffect(() => {
    if (initialContact) {
      setSelectedContact(initialContact);
      setManualPhoneNumber('');
      onInitialContactConsumed?.();
      // Scroll to form after brief render delay
      setTimeout(() => {
        formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 300);
    }
  }, [initialContact]);

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  }

  const handleManualSend = () => {
    if (!manualMessage.trim()) {
      toast.error('Digite uma mensagem antes de enviar.');
      return;
    }
    if (!selectedContact && !manualPhoneNumber.trim()) {
      toast.error('Selecione um contato ou informe um número de WhatsApp.');
      return;
    }
    onSendManual({
      customerId: selectedContact?.id,
      phoneNumber: !selectedContact ? manualPhoneNumber.trim() : undefined,
      message: manualMessage,
    });
    setManualMessage('');
    setSelectedContact(null);
    setManualPhoneNumber('');
  };

  // Pre-fill form when clicking a lead from the table
  const handleSelectLeadForSend = (lead: WhatsAppReadyLeadResponse) => {
    setSelectedContact({
      id: lead.id,
      name: lead.name,
      whatsApp: lead.whatsApp,
      phone: lead.phone,
      email: '',
      companyName: null,
    });
    setManualPhoneNumber('');
    formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  };

  return (
    <div className="space-y-6">
      {/* Connection Status */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <MessageSquare className="h-4 w-4" />
            Status da Conexão WhatsApp
          </CardTitle>
        </CardHeader>
        <CardContent>
          {connectionStatus ? (
            <div className="flex items-center gap-4 flex-wrap">
              <Badge
                className={
                  connectionStatus.isConnected
                    ? 'bg-green-100 text-green-700 hover:bg-green-100'
                    : 'bg-red-100 text-red-700 hover:bg-red-100'
                }
              >
                {connectionStatus.isConnected ? 'Conectado' : 'Desconectado'}
              </Badge>
              <span className="text-sm text-slate-600">
                Estado: <span className="font-medium">{connectionStatus.state}</span>
              </span>
              {connectionStatus.instanceName && (
                <span className="text-sm text-slate-600">
                  Instância: <span className="font-medium">{connectionStatus.instanceName}</span>
                </span>
              )}
              <Button variant="ghost" size="sm" onClick={onRefresh} className="ml-auto">
                <RefreshCw className="mr-2 h-4 w-4" /> Atualizar
              </Button>
            </div>
          ) : (
            <p className="text-sm text-slate-500">Não foi possível obter o status da conexão.</p>
          )}
        </CardContent>
      </Card>

      {/* Stats */}
      {dashboard && (
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
            title="Leads Prontos"
            value={dashboard.whatsAppReadyCount}
            icon={<Users className="h-4 w-4 text-green-600" />}
            accent="bg-green-50"
          />
        </div>
      )}

      {/* Manual Send */}
      <Card ref={formRef}>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Send className="h-4 w-4" />
            Envio Manual
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Contact Search */}
          <div className="space-y-2">
            <Label>Destinatário</Label>
            <ContactSearchInput
              selectedContact={selectedContact}
              onSelect={(contact) => {
                setSelectedContact(contact);
                setManualPhoneNumber('');
              }}
              onClear={() => setSelectedContact(null)}
            />
            <p className="text-xs text-slate-500">
              Busque por nome, email ou empresa para encontrar o contato.
            </p>
          </div>

          {/* Divider */}
          {!selectedContact && (
            <div className="flex items-center gap-3">
              <div className="flex-1 border-t border-slate-200" />
              <span className="text-xs font-medium text-slate-400 uppercase">ou</span>
              <div className="flex-1 border-t border-slate-200" />
            </div>
          )}

          {/* Direct Phone Number */}
          {!selectedContact && (
            <div className="space-y-2">
              <Label htmlFor="manualPhoneNumber" className="flex items-center gap-2">
                <Phone className="h-3.5 w-3.5 text-slate-500" />
                Número WhatsApp
              </Label>
              <Input
                id="manualPhoneNumber"
                value={manualPhoneNumber}
                onChange={(e) => setManualPhoneNumber(e.target.value)}
                placeholder="Ex: 5527999001234"
                type="tel"
              />
              <p className="text-xs text-slate-500">
                Formato: código do país + DDD + número (ex: 5527999001234).
              </p>
            </div>
          )}

          {/* Selected contact phone display */}
          {selectedContact && (
            <div className="flex items-center gap-2 text-sm text-slate-600">
              <Phone className="h-3.5 w-3.5 text-green-600" />
              <span>Número:</span>
              <span className="font-mono font-medium text-green-700">
                {normalizePhoneBR(selectedContact.whatsApp ?? selectedContact.phone) || 'Sem número'}
              </span>
            </div>
          )}

          {/* Message */}
          <div className="space-y-2">
            <Label htmlFor="manualMessage">Mensagem</Label>
            <Textarea
              id="manualMessage"
              value={manualMessage}
              onChange={(e) => setManualMessage(e.target.value)}
              placeholder="Digite a mensagem para enviar via WhatsApp..."
              className="min-h-[120px]"
            />
          </div>

          {/* Send Button */}
          <div className="flex justify-end">
            <Button onClick={handleManualSend} disabled={sendingManual}>
              {sendingManual ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Send className="mr-2 h-4 w-4" />
              )}
              {sendingManual ? 'Enviando...' : 'Enviar WhatsApp'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Ações Rápidas</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-3">
          <Button onClick={onSendCampaign} disabled={sendingCampaign} variant="outline">
            {sendingCampaign ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Send className="mr-2 h-4 w-4" />
            )}
            {sendingCampaign ? 'Enviando...' : 'Enviar Campanha WhatsApp'}
          </Button>
          <Button onClick={onSendFollowUp} disabled={sendingFollowUp} variant="outline">
            {sendingFollowUp ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <MessageSquare className="mr-2 h-4 w-4" />
            )}
            {sendingFollowUp ? 'Enviando...' : 'Enviar Follow-up WhatsApp'}
          </Button>
        </CardContent>
      </Card>

      {/* Ready Leads Table */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide">
            Leads Prontos para WhatsApp
          </h2>
          <Button variant="outline" size="sm" onClick={onRefresh}>
            <RefreshCw className="mr-2 h-4 w-4" /> Atualizar
          </Button>
        </div>

        <div className="flex items-start gap-2 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
          <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
          <span>
            Apenas leads <strong>com segmento definido</strong> (Cold / Warm / Hot) e <strong>com WhatsApp ou telefone</strong> aparecem aqui.
            Leads importados sem segmentação não serão listados — execute a segmentação na aba <strong>Configuração</strong> para incluí-los.
          </span>
        </div>

        {whatsAppLeads.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-16 text-slate-400">
              <MessageSquare className="h-10 w-10 mb-3" />
              <p className="font-medium text-slate-500">Nenhum lead pronto para WhatsApp</p>
              <p className="text-sm mt-1">
                Leads com número de WhatsApp ou telefone e segmento definido aparecerão aqui.
              </p>
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nome</TableHead>
                    <TableHead>WhatsApp</TableHead>
                    <TableHead>Segmento</TableHead>
                    <TableHead className="text-right">Score</TableHead>
                    <TableHead className="text-right">Enviados</TableHead>
                    <TableHead>Último Envio</TableHead>
                    <TableHead className="w-10"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {whatsAppLeads.map((lead) => (
                    <TableRow
                      key={lead.id}
                      className="cursor-pointer hover:bg-slate-50 transition-colors"
                      onClick={() => router.push(`/leads?search=${encodeURIComponent(lead.name)}`)}
                    >
                      <TableCell className="font-medium text-blue-700 hover:text-blue-900 hover:underline">
                        {lead.name}
                      </TableCell>
                      <TableCell className="text-slate-600 text-sm font-mono">
                        {normalizePhoneBR(lead.whatsApp ?? lead.phone) || '–'}
                      </TableCell>
                      <TableCell>{getSegmentBadge(lead.segmentLabel)}</TableCell>
                      <TableCell className="text-right">
                        <span className="font-semibold text-slate-700">
                          {lead.leadScore ?? '–'}
                        </span>
                      </TableCell>
                      <TableCell className="text-right">
                        <span className="font-semibold text-slate-700">
                          {lead.whatsAppSentCount}
                        </span>
                      </TableCell>
                      <TableCell className="text-slate-500 text-xs">
                        {formatDateShort(lead.lastWhatsAppSentAt)}
                      </TableCell>
                      <TableCell onClick={(e) => e.stopPropagation()}>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleSelectLeadForSend(lead)}
                          title={`Enviar WhatsApp para ${lead.name}`}
                          className="h-8 w-8 p-0"
                        >
                          <Send className="h-3.5 w-3.5 text-green-600" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao carregar o dashboard de outreach.');
    } finally {
      setDashboardLoading(false);
    }
  };

  const loadConfig = async () => {
    setConfigLoading(true);
    try {
      const data = await getOutreachConfig();
      setConfig(data);
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao carregar as configurações de outreach.');
    } finally {
      setConfigLoading(false);
    }
  };

  const loadLeads = async () => {
    setLeadsLoading(true);
    try {
      const data = await getReadyLeads(50);
      setLeads(data);
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao carregar leads prontos.');
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao carregar dados do WhatsApp.');
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao executar a segmentação.');
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao disparar a campanha de outreach.');
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao salvar as configurações.');
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao enviar mensagem WhatsApp.');
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao enviar campanha WhatsApp.');
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
    } catch (err: any) {
      toast.error(err?.message ?? 'Erro ao enviar follow-up WhatsApp.');
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
