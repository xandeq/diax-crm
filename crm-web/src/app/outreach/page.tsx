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
  Flame,
  Loader2,
  Mail,
  RefreshCw,
  Send,
  Settings,
  Snowflake,
  Thermometer,
  TrendingUp,
  Users,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

import {
  getOutreachConfig,
  getOutreachDashboard,
  getReadyLeads,
  OutreachConfigResponse,
  OutreachDashboardResponse,
  ReadyLeadResponse,
  runSegmentation,
  sendOutreachCampaign,
  updateOutreachConfig,
  UpdateOutreachConfigRequest,
} from '@/services/outreach';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';

// ─── Types ──────────────────────────────────────────────────────────────────

type ActiveTab = 'dashboard' | 'configuracao' | 'templates' | 'leads';

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
              <Label htmlFor="emailCooldownDays">Cooldown (dias)</Label>
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

function TemplatesTab({ config, loading, onSave, saving }: TemplatesTabProps) {
  const [hotSubject, setHotSubject] = useState('');
  const [hotBody, setHotBody] = useState('');
  const [warmSubject, setWarmSubject] = useState('');
  const [warmBody, setWarmBody] = useState('');
  const [coldSubject, setColdSubject] = useState('');
  const [coldBody, setColdBody] = useState('');

  useEffect(() => {
    if (!config) return;
    setHotSubject(config.hotTemplateSubject ?? '');
    setHotBody(config.hotTemplateBody ?? '');
    setWarmSubject(config.warmTemplateSubject ?? '');
    setWarmBody(config.warmTemplateBody ?? '');
    setColdSubject(config.coldTemplateSubject ?? '');
    setColdBody(config.coldTemplateBody ?? '');
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
                  <TableRow key={lead.id}>
                    <TableCell className="font-medium text-slate-900">
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

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function OutreachPage() {
  const [activeTab, setActiveTab] = useState<ActiveTab>('dashboard');

  // Dashboard data
  const [dashboard, setDashboard] = useState<OutreachDashboardResponse | null>(null);
  const [dashboardLoading, setDashboardLoading] = useState(true);

  // Config data
  const [config, setConfig] = useState<OutreachConfigResponse | null>(null);
  const [configLoading, setConfigLoading] = useState(true);

  // Ready leads
  const [leads, setLeads] = useState<ReadyLeadResponse[]>([]);
  const [leadsLoading, setLeadsLoading] = useState(false);

  // Action states
  const [segmenting, setSegmenting] = useState(false);
  const [sending, setSending] = useState(false);
  const [savingConfig, setSavingConfig] = useState(false);

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

  useEffect(() => {
    loadDashboard();
    loadConfig();
  }, []);

  // Load leads lazily when tab is first opened
  useEffect(() => {
    if (activeTab === 'leads' && leads.length === 0 && !leadsLoading) {
      loadLeads();
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
        <DashboardTab
          dashboard={dashboard}
          loading={dashboardLoading}
          onSegment={handleSegment}
          onSend={handleSend}
          segmenting={segmenting}
          sending={sending}
          onRefresh={loadDashboard}
        />
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
    </div>
  );
}
