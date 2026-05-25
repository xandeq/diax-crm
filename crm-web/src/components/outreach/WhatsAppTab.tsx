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
  Loader2,
  MessageSquare,
  Phone,
  RefreshCw,
  Send,
  Users,
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';

import {
  OutreachDashboardResponse,
  WhatsAppConnectionStatus,
  WhatsAppReadyLeadResponse,
} from '@/services/outreach';
import { ContactSearchInput, ContactOption } from '@/components/outreach/ContactSearchInput';
import { StatCard, getSegmentBadge } from '@/components/outreach/OutreachShared';
import { normalizePhoneBR } from '@/lib/whatsapp-navigation';
import { formatDateShort } from '@/lib/date-utils';

// ─── Props ────────────────────────────────────────────────────────────────────

export interface WhatsAppTabProps {
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

// ─── Component ────────────────────────────────────────────────────────────────

export function WhatsAppTab({
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

  useEffect(() => {
    if (initialContact) {
      setSelectedContact(initialContact);
      setManualPhoneNumber('');
      onInitialContactConsumed?.();
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

          {!selectedContact && (
            <div className="flex items-center gap-3">
              <div className="flex-1 border-t border-slate-200" />
              <span className="text-xs font-medium text-slate-400 uppercase">ou</span>
              <div className="flex-1 border-t border-slate-200" />
            </div>
          )}

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

          {selectedContact && (
            <div className="flex items-center gap-2 text-sm text-slate-600">
              <Phone className="h-3.5 w-3.5 text-green-600" />
              <span>Número:</span>
              <span className="font-mono font-medium text-green-700">
                {normalizePhoneBR(selectedContact.whatsApp ?? selectedContact.phone) || 'Sem número'}
              </span>
            </div>
          )}

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
            Apenas leads <strong>com segmento definido</strong> (Cold / Warm / Hot) e{' '}
            <strong>com WhatsApp ou telefone</strong> aparecem aqui. Leads importados sem
            segmentação não serão listados — execute a segmentação na aba{' '}
            <strong>Configuração</strong> para incluí-los.
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
