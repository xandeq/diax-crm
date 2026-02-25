import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';

/**
 * Normaliza numero de telefone brasileiro para formato da Evolution API.
 * Remove caracteres nao-numericos, tira zero inicial do DDD, e garante prefixo 55.
 * Ex: "027998345690" → "5527998345690"
 */
export function normalizePhoneBR(raw: string | null | undefined): string {
  if (!raw) return '';

  // Remove tudo que nao e digito
  let cleaned = raw.replace(/\D/g, '');

  if (cleaned.length < 10) return cleaned; // muito curto, retorna como esta

  // Se comeca com 0 (formato antigo com zero do DDD), remove
  if (cleaned.startsWith('0') && !cleaned.startsWith('00')) {
    cleaned = cleaned.substring(1);
  }

  // Se nao comeca com 55 (Brasil), adiciona
  if (!cleaned.startsWith('55')) {
    cleaned = '55' + cleaned;
  }

  return cleaned;
}

/**
 * Parametros para navegar ao envio de WhatsApp no Outreach.
 * Usado por qualquer pagina (Leads, Clientes, Dashboard, etc.)
 */
export interface WhatsAppContactParams {
  contactId: string;
  contactName: string;
  contactPhone: string;
  contactEmail?: string;
  contactCompany?: string;
}

/**
 * Constroi a URL para a aba WhatsApp do Outreach com contato pre-preenchido.
 * O telefone e normalizado automaticamente (ex: 027... → 5527...).
 */
export function buildWhatsAppSendUrl(params: WhatsAppContactParams): string {
  const qs = new URLSearchParams({
    tab: 'whatsapp',
    contactId: params.contactId,
    contactName: params.contactName,
    contactPhone: normalizePhoneBR(params.contactPhone),
  });
  if (params.contactEmail) qs.set('contactEmail', params.contactEmail);
  if (params.contactCompany) qs.set('contactCompany', params.contactCompany);
  return `/outreach?${qs.toString()}`;
}

/**
 * Navega para a aba WhatsApp do Outreach com contato pre-preenchido.
 * Centraliza a logica de navegacao para WhatsApp em todo o CRM.
 */
export function navigateToWhatsAppSend(
  router: AppRouterInstance,
  params: WhatsAppContactParams,
) {
  router.push(buildWhatsAppSendUrl(params));
}
