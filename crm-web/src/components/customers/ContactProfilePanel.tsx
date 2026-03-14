'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { EngagementStatsCard } from '@/components/EngagementStatsCard';
import { Customer } from '@/services/customers';
import { formatRelativeTime, formatDate } from '@/lib/date-utils';
import { navigateToWhatsAppSend } from '@/lib/whatsapp-navigation';
import {
  Mail,
  MessageSquare,
  Edit,
  ArrowRight,
  Smartphone,
  Globe,
  Building2,
  Tag,
  Calendar,
  FileText,
  Zap,
  TrendingUp,
} from 'lucide-react';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const STATUS_LABELS: Record<number, string> = {
  0: 'Lead',
  1: 'Contactado',
  2: 'Qualificado',
  3: 'Negociando',
  4: 'Cliente',
  5: 'Inativo',
  6: 'Descadastrado',
};

const STATUS_COLORS: Record<number, string> = {
  0: 'bg-slate-100 text-slate-700 border-slate-200',
  1: 'bg-blue-100 text-blue-700 border-blue-200',
  2: 'bg-purple-100 text-purple-700 border-purple-200',
  3: 'bg-amber-100 text-amber-700 border-amber-200',
  4: 'bg-green-100 text-green-700 border-green-200',
  5: 'bg-gray-100 text-gray-700 border-gray-200',
  6: 'bg-red-100 text-red-700 border-red-200',
};

const SEGMENT_LABELS: Record<number, string> = { 0: 'Cold', 1: 'Warm', 2: 'Hot' };
const SEGMENT_COLORS: Record<number, string> = {
  0: 'bg-sky-100 text-sky-700 border-sky-200',
  1: 'bg-amber-100 text-amber-700 border-amber-200',
  2: 'bg-rose-100 text-rose-700 border-rose-200',
};
const SEGMENT_EMOJIS: Record<number, string> = { 0: '🧊', 1: '☀️', 2: '🔥' };

interface ContactProfilePanelProps {
  customer: Customer;
  onEdit: () => void;
  onSendEmail: () => void;
  onStatusChange: (status: number) => void;
}

export function ContactProfilePanel({
  customer,
  onEdit,
  onSendEmail,
  onStatusChange,
}: ContactProfilePanelProps) {
  const router = useRouter();
  const [pendingStatus, setPendingStatus] = useState<string | null>(null);

  const handleStatusChange = async (newStatus: string) => {
    setPendingStatus(newStatus);
    try {
      await onStatusChange(parseInt(newStatus));
      setPendingStatus(null);
    } catch (error) {
      setPendingStatus(null);
    }
  };

  // Last activity
  const lastActivity = customer.lastContactAt || customer.lastEmailSentAt;
  const lastActivityText = lastActivity
    ? `Última atividade: ${formatRelativeTime(lastActivity)}`
    : 'Nunca foi contatado';

  // Score tooltip explanation
  const getScoreExplanation = () => {
    const score = customer.leadScore ?? 0;
    if (score >= 70) {
      return 'Emails abertos + Cliques em links';
    } else if (score >= 40) {
      return 'Alguns emails abertos';
    }
    return 'Sem interação ou muito fria';
  };

  return (
    <div className="flex flex-col gap-4 pb-4">
      {/* ── Header: Status + Segment + Quick Indicator ────────────────── */}
      <Card>
        <CardContent className="pt-4 pb-3">
          <div className="space-y-3">
            {/* Last Activity */}
            <div className="text-xs text-muted-foreground flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              {lastActivityText}
            </div>

            {/* Status + Segment */}
            <div className="flex flex-wrap items-center gap-2">
              {/* Status Dropdown */}
              <div className="flex-1">
                <Select value={customer.status.toString()} onValueChange={handleStatusChange} disabled={pendingStatus !== null}>
                  <SelectTrigger className={`text-xs h-8 ${STATUS_COLORS[customer.status]}`}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(STATUS_LABELS).map(([key, label]) => (
                      <SelectItem key={key} value={key}>
                        {label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Segment Badge */}
              {customer.segment !== undefined && (
                <Badge className={`text-xs ${SEGMENT_COLORS[customer.segment]}`}>
                  {SEGMENT_EMOJIS[customer.segment]} {SEGMENT_LABELS[customer.segment]}
                </Badge>
              )}
            </div>

            {/* Status converted info */}
            {customer.convertedAt && (
              <div className="text-xs text-green-700 bg-green-50 border border-green-200 rounded px-2 py-1">
                ✅ Cliente desde {formatDate(customer.convertedAt)}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* ── Engagement Stats ────────────────────────────────────────── */}
      <EngagementStatsCard customerId={customer.id} />

      {/* ── Lead Score + Explanation ────────────────────────────────── */}
      <Card>
        <CardContent className="pt-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold flex items-center gap-1">
              <TrendingUp className="h-3.5 w-3.5" />
              Lead Score
            </span>
            <span className="text-sm font-bold tabular-nums">{customer.leadScore ?? 0}</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2">
            <div
              className="bg-blue-600 h-2 rounded-full transition-all"
              style={{ width: `${Math.min(customer.leadScore ?? 0, 100)}%` }}
            ></div>
          </div>
          <div className="text-[10px] text-muted-foreground mt-1">
            {getScoreExplanation()}
          </div>
        </CardContent>
      </Card>

      {/* ── Dados Principais ────────────────────────────────────────── */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm">Informações</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          {/* Email */}
          <div>
            <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
              <Mail className="h-3 w-3" /> Email
            </div>
            <div className="font-medium break-all">{customer.email || '—'}</div>
            {customer.secondaryEmail && (
              <div className="text-xs text-muted-foreground mt-1">{customer.secondaryEmail}</div>
            )}
          </div>

          {/* Phone */}
          {customer.phone && (
            <div>
              <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                <Smartphone className="h-3 w-3" /> Telefone
              </div>
              <div className="font-medium">{customer.phone}</div>
            </div>
          )}

          {/* WhatsApp */}
          {customer.whatsApp && (
            <div>
              <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                <MessageSquare className="h-3 w-3" /> WhatsApp
              </div>
              <div className="font-medium">{customer.whatsApp}</div>
            </div>
          )}

          {/* Company */}
          {customer.companyName && (
            <div>
              <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                <Building2 className="h-3 w-3" /> Empresa
              </div>
              <div className="font-medium">{customer.companyName}</div>
            </div>
          )}

          {/* Website */}
          {customer.website && (
            <div>
              <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                <Globe className="h-3 w-3" /> Website
              </div>
              <a href={customer.website} target="_blank" rel="noopener noreferrer" className="font-medium text-blue-600 break-all hover:underline">
                {customer.website}
              </a>
            </div>
          )}

          {/* Tags */}
          {customer.tags && (
            <div>
              <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                <Tag className="h-3 w-3" /> Tags
              </div>
              <div className="flex flex-wrap gap-1">
                {customer.tags.split(',').filter(t => t.trim()).map((tag, i) => (
                  <Badge key={i} variant="secondary" className="text-[10px]">
                    {tag.trim()}
                  </Badge>
                ))}
              </div>
            </div>
          )}

          {/* Notes */}
          {customer.notes && (
            <div>
              <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                <FileText className="h-3 w-3" /> Notas
              </div>
              <div className="text-xs bg-muted/30 rounded p-2 line-clamp-3">{customer.notes}</div>
            </div>
          )}

          {/* Source */}
          {customer.sourceDescription && (
            <div>
              <div className="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                <Zap className="h-3 w-3" /> Origem
              </div>
              <div className="text-xs">{customer.sourceDescription}</div>
              {customer.sourceDetails && <div className="text-[10px] text-muted-foreground mt-1">{customer.sourceDetails}</div>}
            </div>
          )}

          {/* Created/Updated dates */}
          <div className="border-t pt-2 flex items-center justify-between text-[10px] text-muted-foreground">
            <span>Criado: {formatDate(customer.createdAt)}</span>
            {customer.updatedAt && <span>Atualizado: {formatDate(customer.updatedAt)}</span>}
          </div>
        </CardContent>
      </Card>

      {/* ── Quick Actions ────────────────────────────────────────── */}
      <Card>
        <CardContent className="pt-4">
          <div className="grid grid-cols-2 gap-2">
            <Button
              size="sm"
              variant="default"
              onClick={onSendEmail}
              className="text-xs"
            >
              <Mail className="h-3.5 w-3.5 mr-1" />
              Email
            </Button>
            {customer.whatsApp || customer.phone ? (
              <Button
                size="sm"
                variant="outline"
                onClick={() => {
                  navigateToWhatsAppSend(router, {
                    contactId: customer.id,
                    contactName: customer.name,
                    contactPhone: customer.whatsApp || customer.phone || '',
                    contactEmail: customer.email,
                    contactCompany: customer.companyName,
                  });
                }}
                className="text-xs"
              >
                <MessageSquare className="h-3.5 w-3.5 mr-1" />
                WhatsApp
              </Button>
            ) : (
              <Button
                size="sm"
                variant="outline"
                disabled
                className="text-xs"
              >
                <MessageSquare className="h-3.5 w-3.5 mr-1" />
                WhatsApp
              </Button>
            )}
            <Button
              size="sm"
              variant="outline"
              onClick={onEdit}
              className="text-xs col-span-2"
            >
              <Edit className="h-3.5 w-3.5 mr-1" />
              Editar Contato
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
