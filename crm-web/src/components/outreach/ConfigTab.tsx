'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';

import { OutreachConfigResponse, UpdateOutreachConfigRequest } from '@/services/outreach';
import { ToggleSwitch } from '@/components/outreach/OutreachShared';

// ─── Configuration Tab ────────────────────────────────────────────────────────

export interface ConfigTabProps {
  config: OutreachConfigResponse | null;
  loading: boolean;
  onSave: (data: UpdateOutreachConfigRequest) => void;
  saving: boolean;
}

export function ConfigTab({ config, loading, onSave, saving }: ConfigTabProps) {
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
