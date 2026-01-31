import { apiFetch } from './api';

export type PromptProvider = 'chatgpt' | 'perplexity' | 'deepseek';

export type PromptType = 'professional' | 'pas' | 'aida' | 'fab' | 'pear' | 'goat' | 'care' | 'rtf' | 'risen' | 'costar' | 'cot' | 'tot' | 'cod' | 'tag' | 'bab' | 'create' | 'fsp' | 'sref' | 'deep_research' | 'context_objective';

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
  model?: string;
  promptType: PromptType;
}

export interface AiModel {
  id: string;
  name: string;
  category: string;
  isDefault: boolean;
}

export interface ProviderModels {
  providerId: string;
  providerName: string;
  models: AiModel[];
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
  {
    value: 'rtf',
    label: 'Modo Tarefa Rápida - R.T.F.',
    description: 'Role, Task, Format - Para execução direta sem ambiguidade.',
    whatIs: 'Define claramente quem a IA deve ser (papel), o que ela deve fazer (tarefa) e como deve entregar a resposta (formato).',
    whenToUse: 'Tarefas operacionais do dia a dia que precisam de execução rápida: resumir reuniões, extrair próximos passos, gerar listas ou checklists.',
    example: 'Você é um gerente de vendas. Liste os próximos passos desta reunião em formato de checklist numerada.'
  },
  {
    value: 'risen',
    label: 'Modo Planejamento - R.I.S.E.N.',
    description: 'Role, Instructions, Steps, End Goal, Narrowing - Para projetos complexos.',
    whatIs: 'Estrutura avançada para tarefas complexas que exige planejamento, raciocínio em etapas e restrições claras para controlar a saída da IA.',
    whenToUse: 'Planos estruturados, estratégias detalhadas ou conteúdo sensível: onboarding de clientes, estratégias de implementação, emails críticos.',
    example: 'Atue como consultor de implantação. Crie um plano de onboarding para cliente novo. Pense em etapas claras. Objetivo: ativar em 30 dias. Não use termos técnicos, máximo 10 tópicos.'
  },
  {
    value: 'costar',
    label: 'Modo Comunicação Avançada - C.O.S.T.A.R.',
    description: 'Context, Objective, Style, Tone, Audience, Response - Para conteúdo profissional.',
    whatIs: 'Uma das estruturas mais completas para geração de conteúdo de alta qualidade, considerando contexto, público, estilo, tom e formato final.',
    whenToUse: 'Quando o resultado precisa ser bem escrito e personalizado: propostas comerciais, emails estratégicos, comunicação com decisores.',
    example: 'Contexto: Cliente insatisfeito após atraso. Objetivo: Recuperar confiança. Estilo: Consultivo. Tom: Empático. Público: Diretor financeiro. Resposta: Email profissional.'
  },
  {
    value: 'cot',
    label: 'Modo Análise - Chain of Thought',
    description: 'Raciocínio passo a passo para diagnósticos e análises.',
    whatIs: 'Força a IA a explicar seu raciocínio passo a passo antes de chegar à resposta final, aumentando precisão e reduzindo respostas superficiais.',
    whenToUse: 'Quando a IA precisa analisar dados, justificar decisões ou fazer diagnósticos: lead scoring, análise de risco de fechamento, diagnóstico de oportunidades.',
    example: 'Analise este negócio e explique seu raciocínio passo a passo antes de concluir se ele tem alta ou baixa chance de fechamento.'
  },
  {
    value: 'tot',
    label: 'Modo Estratégia - Tree of Thoughts',
    description: 'Múltiplas soluções avaliadas para decisões estratégicas.',
    whatIs: 'Gera múltiplas soluções possíveis, avalia cada uma e recomenda a melhor, simulando pensamento estratégico com comparação de cenários.',
    whenToUse: 'Quando há mais de um caminho possível: estratégia para reduzir churn, negociação complexa, planejamento de upsell ou cross-sell.',
    example: 'Gere 3 estratégias diferentes para reduzir o churn deste cliente. Avalie os prós e contras de cada uma e recomende a melhor opção.'
  },
  {
    value: 'cod',
    label: 'Modo Sumarização Densa - Chain of Density',
    description: 'Resumos progressivamente mais densos mantendo o mesmo tamanho.',
    whatIs: 'Técnica avançada que gera resumos iterativos cada vez mais densos, incorporando progressivamente mais informações relevantes sem perder entidades críticas.',
    whenToUse: 'Resumir históricos longos sem perder detalhes: tickets de suporte, threads de email, anos de notas de reuniões, timeline completa de clientes.',
    example: 'Resuma este histórico de atendimento em um único parágrafo. A cada iteração, identifique informações importantes faltantes e torne o resumo mais denso, sem aumentar o tamanho final.'
  },
  {
    value: 'tag',
    label: 'Modo Objetivo Estratégico - T.A.G.',
    description: 'Task, Action, Goal - Conecta o que fazer com o porquê fazer.',
    whatIs: 'Estrutura simples e estratégica que conecta o que fazer, como fazer e por que fazer, mantendo foco no objetivo final de negócio.',
    whenToUse: 'Avaliação de desempenho, análise comparativa de resultados, direcionamento de ações de equipe, quando o foco é decisão e resultado.',
    example: 'Tarefa: Avaliar desempenho do vendedor. Ação: Comparar vendas do último trimestre com o anterior. Objetivo: Verificar se o novo treinamento aumentou a conversão.'
  },
  {
    value: 'bab',
    label: 'Modo Transformação - B.A.B.',
    description: 'Before, After, Bridge - Foco em transformação positiva.',
    whatIs: 'Estrutura de comunicação focada em transformação, mostrando o estado atual, o estado desejado e o caminho entre eles. Diferente do PAS, enfatiza resultado e progresso.',
    whenToUse: 'Vendas consultivas, emails de reativação de leads, propostas de upsell, comunicação de valor percebido.',
    example: 'Before: Cliente perde tempo com processos manuais. After: Cliente automatiza tarefas e ganha produtividade. Bridge: Nosso sistema conecta dados e automatiza esse fluxo.'
  },
  {
    value: 'create',
    label: 'Modo Customização Avançada - C.R.E.A.T.E.',
    description: 'Character, Request, Example, Adjustment, Type, Extras - Controle total.',
    whatIs: 'Estrutura avançada que combina papel, exemplos, ajustes finos e instruções negativas, permitindo controle total da saída da IA para conteúdo altamente específico.',
    whenToUse: 'Conteúdo de marketing avançado, materiais de treinamento, scripts personalizados por perfil, quando precisa refinar ao máximo.',
    example: 'Atue como especialista em marketing B2B. Crie email de prospecção. Use este exemplo como referência. Evite linguagem agressiva e termos técnicos. Formato: email curto. Extra: destaque benefício financeiro.'
  },
  {
    value: 'fsp',
    label: 'Modo Padronização - Few-Shot Prompting',
    description: 'Aprendizado por exemplos para consistência e padronização.',
    whatIs: 'Técnica onde a IA recebe exemplos de entrada e saída antes da tarefa real, aprendendo o padrão esperado. Essencial para padronização e consistência.',
    whenToUse: 'Extração de dados, padronização de nomes, classificação de informações, limpeza de dados, quando precisa seguir um formato exato.',
    example: 'Exemplo: Texto: "João, CEO" → Cargo: CEO. Texto: "Maria, Gerente Comercial" → Cargo: Gerente Comercial. Agora extraia o cargo deste texto: "Carlos, Diretor Financeiro".'
  },
  {
    value: 'sref',
    label: 'Modo Refinamento Automático - Self-Refine',
    description: 'IA gera, critica e refina a própria resposta automaticamente.',
    whatIs: 'Técnica de controle de qualidade onde a IA gera uma resposta inicial, critica a própria resposta e produz versão refinada. Reduz erros, alucinações e problemas de tom.',
    whenToUse: 'Respostas a reclamações sensíveis, comunicação com clientes VIP, mensagens de crise ou conflito, quando precisa máxima segurança e empatia.',
    example: 'Escreva resposta para esta reclamação. Critique sua resposta buscando tom defensivo ou falta de empatia. Reescreva a versão final corrigida.'
  },
  {
    value: 'deep_research',
    label: 'Deep Research - Pesquisa Profunda',
    description: 'Transforma ideias em prompts complexos para investigação multi-etapas e síntese de fontes.',
    whatIs: 'Meta-prompt que cria um plano de investigação estruturado, orientando a IA a planejar a pesquisa, buscar em múltiplas fontes, comparar perspectivas e sintetizar resultados com citações.',
    whenToUse: 'Análises de mercado complexas, investigação de tecnologias emergentes, comparação de frameworks, estudos setoriais ou quando precisa de profundidade e validação de fontes.',
    example: 'Quero pesquisar os impactos reais da IA generativa na produtividade do setor jurídico nos últimos 2 anos.'
  },
  {
    value: 'context_objective',
    label: 'Contexto e Objetivo (Prompt Estruturado)',
    description: 'Prompt técnico orientado a entrega que separa claramente situação, tarefa e critérios de sucesso.',
    whatIs: 'Estrutura baseada em Contextual Prompting e RTG (Role/Task/Goal) para reduzir ambiguidade e orientar a resposta com foco em execução.',
    whenToUse: 'Ideal para documentação, processos de QA/UX, políticas internas, fluxos operacionais e quando é preciso reduzir o “achismo” do modelo.',
    example: 'Preciso definir o fluxo de onboarding de novos desenvolvedores, considerando Gitflow e sprints quinzenais.'
  },
];

export async function getPromptModels(): Promise<ProviderModels[]> {
  return await apiFetch<ProviderModels[]>('/prompt-generator/models');
}

export async function generatePrompt(
  rawPrompt: string,
  provider: PromptProvider,
  promptType: PromptType,
  model?: string
): Promise<string> {
  const request: GeneratePromptRequest = { rawPrompt, provider, promptType, model };

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
