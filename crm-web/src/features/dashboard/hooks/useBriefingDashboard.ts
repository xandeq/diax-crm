import { useQuery } from '@tanstack/react-query';
import { dailyBriefingsService } from '@/services/dailyBriefingsService';
import { BriefingItem } from '../types/dashboard.types';

export function useBriefingDashboard() {
  return useQuery({
    queryKey: ['dashboard', 'briefing'],
    queryFn: async () => {
      const todayCards = await dailyBriefingsService.getToday();

      // Se houver algum briefing de hoje, carregar o conteúdo detalhado do primeiro
      let activeBriefing = null;
      if (todayCards && todayCards.length > 0) {
        try {
          activeBriefing = await dailyBriefingsService.getById(todayCards[0].id);
        } catch (err) {
          console.error('[briefing-detail]', err);
        }
      }

      // Se não houver briefing ativo, carregar um fallback vazio estruturado
      const content = activeBriefing?.content || '';

      // Tentar extrair do HTML seções como Ideias, Copies, Notícias
      // O script Python gera seções HTML estruturadas:
      // ex: <h2>Ideias Monetizáveis</h2>, <h2>Novidades</h2>, <h2>WhatsApp Copy</h2>
      const extractSection = (html: string, titlePattern: RegExp): string => {
        const match = html.match(titlePattern);
        if (!match || match.index === undefined) return '';

        const startIdx = match.index + match[0].length;
        // Achar o próximo heading de fechamento ou fim do html
        const nextHeading = html.slice(startIdx).match(/<h[1-6]/i);
        const endIdx = nextHeading && nextHeading.index !== undefined
          ? startIdx + nextHeading.index
          : html.length;

        return html.slice(startIdx, endIdx).trim();
      };

      const tlDr = extractSection(content, /<h2>\s*(?:TL;DR|Resumo Executivo|Resumo)\s*<\/h2>/i);
      const news = extractSection(content, /<h2>\s*(?:Novidades|Notícias|Atualizações)\s*<\/h2>/i);
      const ideas = extractSection(content, /<h2>\s*(?:Ideias Monetizáveis|Ideias|Monetização)\s*<\/h2>/i);
      const whatsAppCopy = extractSection(content, /<h2>\s*(?:WhatsApp Copy|WhatsApp|Copies WhatsApp)\s*<\/h2>/i);
      const emailCopy = extractSection(content, /<h2>\s*(?:Email Copy|Email|Copies Email)\s*<\/h2>/i);

      // Limpar tags HTML para copies de texto puro se necessário
      const cleanHtml = (html: string) => {
        if (!html) return '';
        // Remover tags p, pre, code mas manter quebras de linha
        return html
          .replace(/<br\s*\/?>/gi, '\n')
          .replace(/<\/p>/gi, '\n\n')
          .replace(/<[^>]+>/g, '')
          .replace(/&quot;/g, '"')
          .replace(/&amp;/g, '&')
          .replace(/&lt;/g, '<')
          .replace(/&gt;/g, '>')
          .trim();
      };

      return {
        cards: todayCards,
        activeBriefing,
        extracted: {
          tlDr: tlDr || '<p className="text-zinc-500">Sem resumo executivo disponível.</p>',
          news: news || '<p className="text-zinc-500">Nenhuma novidade listada hoje.</p>',
          ideas: ideas || '<p className="text-zinc-500">Nenhuma ideia de monetização listada hoje.</p>',
          whatsAppCopy: cleanHtml(whatsAppCopy) || 'Nenhum script de WhatsApp disponível.',
          emailCopy: cleanHtml(emailCopy) || 'Nenhum script de email disponível.',
        }
      };
    },
    refetchOnWindowFocus: false,
    staleTime: 5 * 60 * 1000, // 5 minutos de cache
  });
}
