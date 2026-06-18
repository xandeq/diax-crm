import { useQuery } from '@tanstack/react-query';
import { getOutreachDashboard, getWhatsAppStatus, getEmailQueue } from '@/services/outreach';
import { getEmailCampaigns } from '@/services/emailMarketing';
import { getProviderHealth } from '@/services/emailProviders';

export function useMarketingDashboard() {
  return useQuery({
    queryKey: ['dashboard', 'marketing'],
    queryFn: async () => {
      const [
        outreachResult,
        whatsappResult,
        campaignsResult,
        queueResult,
        providerHealthResult,
      ] = await Promise.allSettled([
        getOutreachDashboard(),
        getWhatsAppStatus(),
        getEmailCampaigns(1, 10),
        getEmailQueue(1, 15),
        getProviderHealth(),
      ]);

      const getVal = <T>(res: PromiseSettledResult<T>, fallback: T): T =>
        res.status === 'fulfilled' ? res.value : fallback;

      const outreach = getVal(outreachResult, null);
      const whatsapp = getVal(whatsappResult, null);
      
      const campaignsPaged = getVal(campaignsResult, null);
      const campaignsList = Array.isArray(campaignsPaged?.items) ? campaignsPaged.items : [];
      
      const queuePaged = getVal(queueResult, null);
      const queueList = Array.isArray(queuePaged?.items) ? queuePaged.items : [];
      
      const providerHealthRaw = getVal(providerHealthResult, null);
      const providerHealth = Array.isArray(providerHealthRaw) ? providerHealthRaw : [];

      // Calcular taxas agregadas
      let totalSent = 0;
      let totalDelivered = 0;
      let totalOpened = 0;
      let totalClicked = 0;
      let totalBounced = 0;
      let totalUnsubscribed = 0;

      campaignsList.forEach(c => {
        totalSent += c.sentCount || 0;
        totalDelivered += c.deliveredCount || 0;
        totalOpened += c.openCount || 0;
        totalClicked += c.clickCount || 0;
        totalBounced += c.bounceCount || 0;
        totalUnsubscribed += c.unsubscribeCount || 0;
      });

      const openRate = totalSent > 0 ? (totalOpened / totalSent) * 100 : 0;
      const clickRate = totalSent > 0 ? (totalClicked / totalSent) * 100 : 0;
      const bounceRate = totalSent > 0 ? (totalBounced / totalSent) * 100 : 0;
      const unsubscribeRate = totalSent > 0 ? (totalUnsubscribed / totalSent) * 100 : 0;

      // WhatsApp status
      const formattedWhatsapp = {
        connected: whatsapp?.isConnected ?? false,
        instanceName: whatsapp?.instanceName ?? 'default',
        status: whatsapp?.isConnected ? 'online' as const : 'offline' as const,
        sentToday: outreach?.whatsAppSentToday ?? 0,
        queuedMessages: outreach?.whatsAppReadyCount ?? 0,
      };

      // Fila de e-mails
      const formattedQueue = queueList.map(item => ({
        id: item.id,
        recipientName: item.recipientName,
        recipientEmail: item.recipientEmail,
        subject: item.subject,
        status: item.status, // 0 = Queued, 1 = Processing, 2 = Sent, 3 = Failed
        scheduledAt: item.scheduledAt,
        attemptCount: item.attemptCount,
        lastError: item.lastError,
      }));

      // Próximo horário seguro (estimativa simples: a cada 5 minutos roda o worker)
      const nextSafeSendTime = new Date();
      const mins = nextSafeSendTime.getMinutes();
      const offset = 5 - (mins % 5);
      nextSafeSendTime.setMinutes(mins + offset);
      nextSafeSendTime.setSeconds(0);
      nextSafeSendTime.setMilliseconds(0);

      // Performance por canal (estimado)
      const channels = [
        {
          channel: 'Email' as const,
          spend: 0, // Email SMTP geralmente tem custo fixo/gratuito
          revenue: totalSent * 0.15, // Estimativa comercial: R$ 0.15 por e-mail enviado convertido
          leads: totalOpened,
          cpc: 0,
          ctr: clickRate,
          roas: 0,
        },
        {
          channel: 'WhatsApp' as const,
          spend: 0,
          revenue: (outreach?.whatsAppSentThisWeek ?? 0) * 0.45,
          leads: outreach?.whatsAppReadyCount ?? 0,
          cpc: 0,
          ctr: 18.5, // Estimativa de resposta
          roas: 0,
        }
      ];

      return {
        outreach,
        whatsapp: formattedWhatsapp,
        campaigns: campaignsList,
        queue: formattedQueue,
        providerHealth,
        stats: {
          totalSent,
          totalDelivered,
          openRate,
          clickRate,
          bounceRate,
          unsubscribeRate,
        },
        nextSafeSendTime: nextSafeSendTime.toISOString(),
        channels,
      };
    },
    refetchOnWindowFocus: false,
    staleTime: 60 * 1000,
  });
}
