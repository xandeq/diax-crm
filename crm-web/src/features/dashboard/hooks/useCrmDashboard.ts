import { useQuery } from '@tanstack/react-query';
import { getLeads, CustomerStatus as LeadStatus } from '@/services/leads';
import { getCustomers } from '@/services/customers';

export function useCrmDashboard() {
  return useQuery({
    queryKey: ['dashboard', 'crm'],
    queryFn: async () => {
      // Executar requisições em paralelo
      const [
        fLResult,
        fCResult,
        fQResult,
        fNResult,
        fCustResult,
        segColdResult,
        segWarmResult,
        segHotResult,
        recentLeadsResult,
        sourceManualResult,
        sourceScrapeResult,
        sourceImportResult,
        sourceMapResult,
      ] = await Promise.allSettled([
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Lead }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Contacted }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Qualified }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Negotiating }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Customer }),
        // Segmentos (0 = Cold, 1 = Warm, 2 = Hot)
        getLeads({ page: 1, pageSize: 1, segment: 0 }),
        getLeads({ page: 1, pageSize: 1, segment: 1 }),
        getLeads({ page: 1, pageSize: 1, segment: 2 }),
        // Recentes
        getLeads({ page: 1, pageSize: 20, sortBy: 'CreatedAt', sortDescending: true }),
        // Fontes (1 = Manual, 4 = Scraping, 10 = Import, 11 = GoogleMaps)
        getLeads({ page: 1, pageSize: 1, source: 1 }),
        getLeads({ page: 1, pageSize: 1, source: 4 }),
        getLeads({ page: 1, pageSize: 1, source: 10 }),
        getLeads({ page: 1, pageSize: 1, source: 11 }),
      ]);

      const fc = (res: PromiseSettledResult<{ totalCount?: number }>) =>
        res.status === 'fulfilled' ? res.value?.totalCount ?? 0 : 0;

      const funnel = {
        lead: fc(fLResult),
        contacted: fc(fCResult),
        qualified: fc(fQResult),
        negotiating: fc(fNResult),
        customer: fc(fCustResult),
      };

      const segments = {
        cold: fc(segColdResult),
        warm: fc(segWarmResult),
        hot: fc(segHotResult),
      };

      const sources = {
        manual: fc(sourceManualResult),
        scraping: fc(sourceScrapeResult),
        import: fc(sourceImportResult),
        googleMaps: fc(sourceMapResult),
      };

      const recentLeads = recentLeadsResult.status === 'fulfilled' ? recentLeadsResult.value?.items ?? [] : [];

      // Calcular score médio a partir dos leads carregados
      const totalCount = recentLeads.length;
      const avgLeadScore = totalCount > 0
        ? Math.round(recentLeads.reduce((acc, lead) => acc + (lead.leadScore ?? 0), 0) / totalCount)
        : 0;

      // Calcular conversões e gargalos
      const totalPipeline = funnel.lead + funnel.contacted + funnel.qualified + funnel.negotiating;
      const stages = [
        { name: 'Leads', v: totalPipeline + funnel.customer },
        { name: 'Contato', v: funnel.contacted + funnel.qualified + funnel.negotiating + funnel.customer },
        { name: 'Qualif.', v: funnel.qualified + funnel.negotiating + funnel.customer },
        { name: 'Neg.', v: funnel.negotiating + funnel.customer },
        { name: 'Clientes', v: funnel.customer },
      ];

      const stageConversions = stages.slice(0, 4).map((stage, idx) => {
        const nextStage = stages[idx + 1];
        const pct = stage.v > 0 ? (nextStage.v / stage.v) * 100 : 0;
        return {
          from: stage.name,
          to: nextStage.name,
          pct,
        };
      });

      const bottlenecks = stageConversions.map(c => ({
        stage: `${c.from} → ${c.to}`,
        drop: 100 - c.pct,
      })).sort((a, b) => b.drop - a.drop);

      // Filtrar oportunidades quentes (Hot ou leadScore alto)
      const topOpportunities = recentLeads
        .filter(l => l.segment === 2 || (l.leadScore ?? 0) >= 70)
        .sort((a, b) => (b.leadScore ?? 0) - (a.leadScore ?? 0))
        .slice(0, 8);

      // Leads sem contato recente (status ativo, mas sem contato há 7 dias ou nulo)
      const sevenDaysAgo = new Date();
      sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
      const staleLeads = recentLeads
        .filter(l => l.status !== LeadStatus.Customer && l.status !== LeadStatus.Inactive && l.status !== LeadStatus.Churned)
        .filter(l => !l.lastContactAt || new Date(l.lastContactAt) < sevenDaysAgo)
        .slice(0, 8);

      // Leads por cidade (estimado a partir de notes ou tags)
      const citiesMap: Record<string, number> = {};
      recentLeads.forEach(l => {
        // Tentar tags primeiro
        if (l.tags) {
          const tagsList = l.tags.split(',').map(t => t.trim().toLowerCase());
          const cityTag = tagsList.find(t => t.startsWith('cidade:'));
          if (cityTag) {
            const cityName = cityTag.replace('cidade:', '').trim();
            citiesMap[cityName] = (citiesMap[cityName] ?? 0) + 1;
            return;
          }
        }
        // Tentar notes
        if (l.notes) {
          const match = l.notes.match(/Cidade:\s*([^\n\r,]+)/i);
          if (match && match[1]) {
            const cityName = match[1].trim().toLowerCase();
            citiesMap[cityName] = (citiesMap[cityName] ?? 0) + 1;
            return;
          }
        }
      });

      const cityAllocation = Object.entries(citiesMap)
        .map(([name, count]) => ({
          name: name.charAt(0).toUpperCase() + name.slice(1),
          count,
        }))
        .sort((a, b) => b.count - a.count)
        .slice(0, 5);

      return {
        funnel,
        segments,
        sources,
        avgLeadScore,
        stageConversions,
        bottlenecks,
        recentLeads,
        topOpportunities,
        staleLeads,
        cityAllocation,
      };
    },
    refetchOnWindowFocus: false,
    staleTime: 60 * 1000,
  });
}
