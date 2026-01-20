import { apiFetch } from './api';

export type PromptProvider = 'chatgpt' | 'perplexity' | 'deepseek';

export type PromptType = 'professional' | 'pas' | 'aida' | 'fab' | 'pear' | 'goat' | 'care';

export interface PromptTypeOption {
  value: PromptType;
  label: string;
  description: string;
  whatIs: string;
  whenToUse: string;
  example: string;
}

export interface GeneratePromptRequest {
  rawPrompt: string;
  provider: PromptProvider;
  promptType: PromptType;
}

export interface GeneratePromptResponse {
  finalPrompt: string;
}

export const promptTypeOptions: PromptTypeOption[] = [
  {
    value: 'professional',
    label: 'Profissional (Padrão)',
    description: 'Prompt estruturado com contexto, objetivo, público-alvo e instruções.',
    whatIs: 'Transforma qualquer ideia em um prompt profissional, claro e estruturado seguindo boas práticas de Prompt Engineering.',
    whenToUse: 'Qualquer situação onde você precisa de um prompt bem organizado: desenvolvimento de software, criação de conteúdo, análises, estudos ou tarefas gerais.',
    example: 'Preciso criar um sistema de login para minha aplicação web usando React e Node.js com autenticação JWT.'
  },
  {
    value: 'pas',
    label: 'P.A.S. - Problema, Agitar, Solução',
    description: 'Ideal para conteúdo persuasivo, vendas e marketing emocional.',
    whatIs: 'Comece identificando o problema, aprofunde-se no porquê ele dói, e então apresente sua solução.',
    whenToUse: 'Perfeito para conteúdo persuasivo, textos de vendas, e-mails de marketing ou qualquer hora que você precise convencer alguém a agir. Funciona muito bem quando você quer conteúdo emocional e envolvente.',
    example: 'Preciso de um título e subtítulo para uma página de destino de um aplicativo de produtividade. Problema: Profissionais perdem mais de 2 horas diárias em tarefas desorganizadas. Agitar: Isso leva a prazos perdidos, noites de trabalho e estresse constante que afeta a vida pessoal. Solução: Nosso aplicativo usa IA para priorizar e organizar tarefas automaticamente em menos de 5 minutos por dia.'
  },
  {
    value: 'aida',
    label: 'A.I.D.A. - Atenção, Interesse, Desejo, Ação',
    description: 'Perfeito para anúncios, campanhas e jornada de decisão.',
    whatIs: 'O funil de marketing clássico – chame a atenção, construa interesse, crie desejo e então force a ação.',
    whenToUse: 'Anúncios, descrições de produtos, campanhas de e-mail ou posts em mídias sociais. Basicamente, em qualquer lugar que você precise guiar alguém por uma jornada de tomada de decisão.',
    example: 'Escreva um anúncio no Facebook para fones de ouvido com cancelamento de ruído. Atenção: Atraia com "Ainda trabalhando da sua sala barulhenta?". Interesse: Explique como o cancelamento de ruído ativo cria um espaço de trabalho privado em qualquer lugar. Desejo: Pinte um quadro deles totalmente focados, com a produtividade disparando, o estresse derretendo. Ação: Termine com um código de desconto de 30% por tempo limitado e um CTA "Compre Agora".'
  },
  {
    value: 'fab',
    label: 'F.A.B. - Características, Vantagens, Benefícios',
    description: 'Ótimo para descrições de produtos e traduzir specs em valor.',
    whatIs: 'Conecte os pontos do que algo É (características), ao que ele FAZ (vantagens), ao que isso SIGNIFICA para o usuário (benefícios).',
    whenToUse: 'Descrições de produtos, documentação técnica que precisa ser amigável, conteúdo de comparação ou quando você precisa traduzir especificações em valor do mundo real.',
    example: 'Crie uma descrição de produto para um smartphone. Características: Câmera de 108MP, bateria de 5000mAh, tela de 120Hz. Vantagens: Tira fotos com qualidade profissional em baixa luz, dura dois dias inteiros com uma carga, a rolagem é suave como manteiga, sem atraso. Benefícios: Capture memórias perfeitas sem carregar equipamentos extras, pare de se preocupar em encontrar tomadas durante longos dias, aproveite uma experiência sem frustrações que torna seu telefone um prazer de usar.'
  },
  {
    value: 'pear',
    label: 'P.E.A.R. - Pesquisa, Extrair, Aplicar, Entregar',
    description: 'Ideal para análises, pesquisas e síntese de informações.',
    whatIs: 'Uma abordagem sistemática onde você reúne informações, extrai insights-chave, aplica-os ao seu contexto específico e, em seguida, apresenta os resultados.',
    whenToUse: 'Resumos de pesquisa, análise da concorrência, aprendizado de novos tópicos, criação de relatórios ou qualquer hora que você precise sintetizar informações de várias fontes em insights acionáveis.',
    example: 'Me ajude a entender as estratégias dos concorrentes no mercado de entrega de kits de refeições. Pesquisa: Analise os modelos de preços, públicos-alvo e pontos de venda exclusivos dos 3 principais concorrentes. Extrair: Identifique os padrões comuns e os principais diferenciadores. Aplicar: Sugira como um novo participante focado em dietas keto poderia se posicionar. Entregar: Forneça um resumo estratégico de uma página com três recomendações específicas.'
  },
  {
    value: 'goat',
    label: 'G.O.A.T. - Objetivo, Obstáculo, Ação, Transformação',
    description: 'Perfeito para storytelling, estudos de caso e narrativas.',
    whatIs: 'Defina onde você quer chegar, identifique o que está te bloqueando, descreva as etapas para superar isso e descreva o resultado final.',
    whenToUse: 'Conteúdo de desenvolvimento pessoal, estudos de caso, storytelling, cenários de coaching ou planejamento de projetos. Ótimo para conteúdo narrativo que mostra uma jornada.',
    example: 'Escreva um estudo de caso sobre a transformação digital de uma pequena empresa. Objetivo: Uma padaria local queria aumentar os pedidos online em 300%. Obstáculo: Eles não tinham presença digital e o proprietário era tecnofóbico. Ação: Implementamos uma estratégia simples no Instagram, adicionamos pedidos online por meio de uma plataforma sem código e treinamos a equipe por 3 meses. Transformação: Mostre como eles agora recebem mais de 50 pedidos online diários, contrataram 2 novos funcionários e o proprietário gerencia com confiança sua presença digital.'
  },
  {
    value: 'care',
    label: 'C.A.R.E. - Conteúdo, Ação, Resultado, Emoção',
    description: 'Ideal para depoimentos e histórias de sucesso com dados.',
    whatIs: 'Apresente o conteúdo/situação, especifique a ação tomada, mostre o resultado mensurável e conecte-o ao impacto emocional.',
    whenToUse: 'Depoimentos, histórias de sucesso, cenários de antes e depois, relatórios de impacto ou qualquer conteúdo em que você queira equilibrar dados com conexão humana.',
    example: 'Crie um depoimento de cliente para um programa de coaching de fitness. Conteúdo: Sandra, uma mulher de 45 anos que não se exercitava há 10 anos e se sentia invisível. Ação: Ela entrou em nosso programa de 90 dias, se exercitou 4 vezes por semana e seguiu nossos planos de refeições. Resultado: Perdeu 15 quilos, correu sua primeira corrida de 5 km, reduziu a medicação para pressão alta. Emoção: Termine com como ela se sente confiante em seu corpo novamente, tem energia para brincar com seus netos e finalmente se sente ela mesma.'
  },
];

export async function generatePrompt(rawPrompt: string, provider: PromptProvider, promptType: PromptType): Promise<string> {
  const request: GeneratePromptRequest = { rawPrompt, provider, promptType };

  const response = await apiFetch<GeneratePromptResponse>(
    '/prompt-generator/generate',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );

  return response.finalPrompt;
}
