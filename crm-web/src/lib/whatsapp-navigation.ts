import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';

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
 */
export function buildWhatsAppSendUrl(params: WhatsAppContactParams): string {
  const qs = new URLSearchParams({
    tab: 'whatsapp',
    contactId: params.contactId,
    contactName: params.contactName,
    contactPhone: params.contactPhone,
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
