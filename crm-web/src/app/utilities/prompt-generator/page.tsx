'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { generatePrompt, PromptGeneratorError, PromptProvider, PromptType } from '@/services/promptGenerator';
import {
  AlertCircle,
  Check,
  Copy,
  RefreshCw,
  Sparkles,
  Wand2
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Lista estática de providers e modelos como fallback e referência
const AI_PROVIDERS = [
  {
    id: 'chatgpt',
    name: 'ChatGPT (OpenAI)',
    models: [
      { id: 'gpt-4o-mini', name: 'GPT-4o Mini' },
      { id: 'gpt-4o', name: 'GPT-4o' },
      { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo' }
    ]
  },
  {
    id: 'gemini',
    name: 'Google Gemini',
    models: [
      { id: 'gemini-1.5-flash', name: 'Gemini 1.5 Flash' },
      { id: 'gemini-1.5-pro', name: 'Gemini 1.5 Pro' }
    ]
  },
  {
    id: 'perplexity',
    name: 'Perplexity AI',
    models: [
      { id: 'sonar-pro', name: 'Sonar Pro' },
      { id: 'sonar', name: 'Sonar' }
    ]
  },
  {
    id: 'deepseek',
    name: 'DeepSeek',
    models: [
      { id: 'deepseek-chat', name: 'DeepSeek Chat (V3)' },
      { id: 'deepseek-reasoner', name: 'DeepSeek Reasoner (R1)' }
    ]
  },
  {
    id: 'openrouter',
    name: 'OpenRouter',
    models: [
      { id: 'openai/gpt-4o-mini', name: 'GPT-4o Mini (via OpenRouter)' },
      { id: 'anthropic/claude-3.5-sonnet', name: 'Claude 3.5 Sonnet' },
      { id: 'meta-llama/llama-3.1-70b-instruct', name: 'Llama 3.1 70B' }
    ]
  }
];

const PROMPT_TYPES = [
  { id: 'professional', name: 'Profissional', icon: '👔' },
  { id: 'pas', name: 'P.A.S. (Problema-Agitação-Solução)', icon: '📢' },
  { id: 'aida', name: 'A.I.D.A. (Atenção-Interesse-Desejo-Ação)', icon: '📣' },
  { id: 'fab', name: 'F.A.B. (Features-Advantages-Benefits)', icon: '💎' },
  { id: 'pear', name: 'P.E.A.R. (Problema-Exemplo-Ação-Resultado)', icon: '🍐' },
  { id: 'goat', name: 'G.O.A.T. (Greatest of All Time)', icon: '🐐' },
  { id: 'care', name: 'C.A.R.E. (Contexto-Ação-Resultado-Exemplo)', icon: '❤️' },
  { id: 'rtf', name: 'R.T.F. (Role-Task-Format)', icon: '📝' },
  { id: 'risen', name: 'R.I.S.E.N.', icon: '🌄' },
  { id: 'costar', name: 'C.O.S.T.A.R.', icon: '⭐' },
  { id: 'cot', name: 'Chain of Thought', icon: '🧠' },
  { id: 'tot', name: 'Tree of Thoughts', icon: '🌳' },
  { id: 'cod', name: 'Chain of Density', icon: '⛓️' },
  { id: 'tag', name: 'T.A.G.', icon: '🏷️' },
  { id: 'bab', name: 'Before-After-Bridge', icon: '🌉' },
  { id: 'create', name: 'C.R.E.A.T.E.', icon: '🎨' },
  { id: 'fsp', name: 'Few-Shot Prompting', icon: '🎯' },
  { id: 'sref', name: 'Self-Refine', icon: '🔄' },
  { id: 'deep_research', name: 'Deep Research', icon: '🔍' },
  { id: 'context_objective', name: 'Contexto e Objetivo', icon: '🎯' }
];

export default function PromptGeneratorPage() {
  const [loading, setLoading] = useState(false);
  const [rawPrompt, setRawPrompt] = useState('');
  const [generatedPrompt, setGeneratedPrompt] = useState('');
  const [selectedProvider, setSelectedProvider] = useState<PromptProvider>('chatgpt');
  const [selectedModel, setSelectedModel] = useState('gpt-4o-mini');
  const [selectedPromptType, setSelectedPromptType] = useState<PromptType>('professional');
  const [copied, setCopied] = useState(false);
  const [errorObj, setErrorObj] = useState<{
    message: string;
    isRetryable: boolean;
    correlationId?: string;
  } | null>(null);

  // Efeito para atualizar o modelo padrão quando o provider muda
  useEffect(() => {
    const provider = AI_PROVIDERS.find(p => p.id === selectedProvider);
    if (provider && provider.models.length > 0) {
      // Se o modelo atual não pertence a este provider, pega o primeiro da lista
      const modelExists = provider.models.some(m => m.id === selectedModel);
      if (!modelExists) {
        setSelectedModel(provider.models[0].id);
      }
    }
  }, [selectedProvider, selectedModel]); // Added selectedModel to dependency array to satisfy exhaustive-deps, though logic handles update

  const handleGenerate = async () => {
    if (!rawPrompt.trim()) {
      toast.error('Por favor, digite uma descrição para o prompt.');
      return;
    }

    setLoading(true);
    setGeneratedPrompt('');
    setErrorObj(null);

    try {
      const result = await generatePrompt(
        rawPrompt,
        selectedProvider,
        selectedPromptType,
        selectedModel
      );

      setGeneratedPrompt(result);
      toast.success('Prompt gerado com sucesso!');
    } catch (err) {
      if (err instanceof PromptGeneratorError) {
        setErrorObj({
          message: err.message,
          isRetryable: !err.isConfiguration(),
          correlationId: err.correlationId
        });

        // Mantemos toast para feedback visual rápido, mas não bloqueante
        if (!err.isConfiguration()) {
             toast.error(err.message);
        }
      } else {
        setErrorObj({
          message: 'Ocorreu um erro inesperado. Tente novamente.',
          isRetryable: true
        });
        toast.error('Ocorreu um erro inesperado.');
      }
    } finally {
      setLoading(false);
    }
  };

  const copyToClipboard = async () => {
    if (!generatedPrompt) return;
    await navigator.clipboard.writeText(generatedPrompt);
    setCopied(true);
    toast.success('Prompt copiado para a área de transferência!');
    setTimeout(() => setCopied(false), 2000);
  };

  // Helper para pegar os modelos do provider atual
  const currentModels = AI_PROVIDERS.find(p => p.id === selectedProvider)?.models || [];

  return (
    <div className="container mx-auto py-8 max-w-5xl space-y-8">
      <div className="flex flex-col space-y-2">
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <Wand2 className="h-8 w-8 text-primary" />
          Gerador de Prompts IA
        </h1>
        <p className="text-muted-foreground">
          Crie prompts de alta qualidade otimizados para diferentes modelos de IA
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-[350px_1fr]">
        {/* Sidebar de Configuração */}
        <div className="space-y-6">
          <Card>
            <CardContent className="p-6 space-y-6">
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label>Provedor de IA</Label>
                  <Select value={selectedProvider} onValueChange={(val) => setSelectedProvider(val as PromptProvider)}>
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione o provider" />
                    </SelectTrigger>
                    <SelectContent>
                      {AI_PROVIDERS.map((provider) => (
                        <SelectItem key={provider.id} value={provider.id}>
                          {provider.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Modelo</Label>
                  <Select value={selectedModel} onValueChange={setSelectedModel}>
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione o modelo" />
                    </SelectTrigger>
                    <SelectContent>
                      {currentModels.map((model) => (
                        <SelectItem key={model.id} value={model.id}>
                          {model.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Tipo de Prompt</Label>
                  <Select value={selectedPromptType} onValueChange={(val) => setSelectedPromptType(val as PromptType)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {PROMPT_TYPES.map((type) => (
                        <SelectItem key={type.id} value={type.id}>
                          <span className="mr-2">{type.icon}</span>
                          {type.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="bg-muted/50 p-4 rounded-lg text-sm space-y-2">
                <p className="font-medium flex items-center gap-2">
                  <Sparkles className="h-4 w-4 text-yellow-500" />
                  Dica Pro
                </p>
                <p className="text-muted-foreground">
                  Modelos diferentes respondem melhor a estruturas diferentes. O sistema adapta automaticamente seu pedido para a "linguagem" do modelo escolhido.
                </p>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Área Principal */}
        <div className="space-y-6">
          <Card className="h-full flex flex-col">
            <CardContent className="p-6 flex-1 flex flex-col space-y-6">
              <div className="space-y-2">
                <Label htmlFor="input-prompt">Descreva o que você precisa</Label>
                <Textarea
                  id="input-prompt"
                  placeholder="Ex: Preciso de um email de vendas frio para oferecer serviços de marketing digital para dentistas..."
                  className="min-h-[120px] resize-none text-base"
                  value={rawPrompt}
                  onChange={(e) => setRawPrompt(e.target.value)}
                />
              </div>

              <div className="flex justify-end">
                <Button
                  onClick={handleGenerate}
                  disabled={loading || !rawPrompt.trim()}
                  className="w-full md:w-auto"
                >
                  {loading ? (
                    <>
                      <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
                      Otimizando...
                    </>
                  ) : (
                    <>
                      <Sparkles className="mr-2 h-4 w-4" />
                      Gerar Prompt Otimizado
                    </>
                  )}
                </Button>
              </div>

              {errorObj && (
                <div className="flex flex-col gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-900">
                  <div className="flex items-start gap-2">
                    <AlertCircle className="h-5 w-5 shrink-0 mt-0.5" />
                    <div className="flex-1 text-sm font-medium">
                      {errorObj.message}
                    </div>
                  </div>
                  {errorObj.correlationId && (
                    <div className="text-xs text-red-700/80 ml-7">
                      ID de Suporte: {errorObj.correlationId}
                    </div>
                  )}
                  {errorObj.isRetryable && (
                    <Button
                      variant="link"
                      className="text-red-900 underline h-auto p-0 ml-7 justify-start font-semibold"
                      onClick={handleGenerate}
                    >
                      Tentar novamente
                    </Button>
                  )}
                </div>
              )}

              {generatedPrompt && (
                <div className="space-y-2 animate-in fade-in slide-in-from-bottom-4 duration-500">
                  <div className="flex items-center justify-between">
                    <Label className="text-base font-semibold text-primary">Resultado Gerado</Label>
                    <div className="flex gap-2">
                      <Badge variant="outline" className="text-xs font-normal">
                        {AI_PROVIDERS.find(p => p.id === selectedProvider)?.name}
                      </Badge>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={copyToClipboard}
                        className="h-8 px-2"
                      >
                        {copied ? (
                          <>
                            <Check className="mr-2 h-3 w-3" /> Copiado
                          </>
                        ) : (
                          <>
                            <Copy className="mr-2 h-3 w-3" /> Copiar
                          </>
                        )}
                      </Button>
                    </div>
                  </div>
                  <div className="relative">
                    <Textarea
                      readOnly
                      value={generatedPrompt}
                      className="min-h-[300px] font-mono text-sm bg-muted/30 resize-y"
                    />
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
