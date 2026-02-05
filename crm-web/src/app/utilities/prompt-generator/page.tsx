'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { useAuth } from '@/contexts/AuthContext';
import { AiProvider as CatalogProvider, getAiCatalog } from '@/services/aiCatalog';
import {
  generatePrompt,
  getPromptById,
  getPromptHistory,
  PromptGeneratorError,
  PromptProvider,
  PromptType,
  promptTypeOptions,
  UserPromptHistory
} from '@/services/promptGenerator';
import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import {
  AlertCircle,
  Check,
  Clock,
  Copy,
  ExternalLink,
  History,
  LayoutTemplate,
  Lightbulb,
  RefreshCw,
  Sparkles,
  Wand2
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Removed static AI_PROVIDERS. Now loaded from API.

export default function PromptGeneratorPage() {
    const { isAuthenticated, isLoading: authLoading } = useAuth();
    const router = useRouter();

    const [providers, setProviders] = useState<CatalogProvider[]>([]);
    const [isLoadingCatalog, setIsLoadingCatalog] = useState(true);

    const [loading, setLoading] = useState(false);
    const [rawPrompt, setRawPrompt] = useState('');
    const [generatedPrompt, setGeneratedPrompt] = useState('');
    const [selectedProvider, setSelectedProvider] = useState<string>(''); // Stores Provider Key (e.g. 'openai')
    const [selectedModel, setSelectedModel] = useState<string>('');       // Stores Model Key (e.g. 'gpt-4o')
    const [selectedPromptType, setSelectedPromptType] = useState<PromptType>('professional');
    const [copied, setCopied] = useState(false);
    const [history, setHistory] = useState<UserPromptHistory[]>([]);
    const [loadingHistory, setLoadingHistory] = useState(false);
    const [errorObj, setErrorObj] = useState<{
        message: string;
        isRetryable: boolean;
        correlationId?: string;
    } | null>(null);

    // Derived state for current provider object
    const currentProvider = providers.find(p => p.key === selectedProvider);
    const currentModels = currentProvider?.models || [];

  // Detalhes da técnica selecionada (Importado do promptGenerator.ts)
  const selectedTypeDetails = promptTypeOptions.find(t => t.value === selectedPromptType);

  // Efeito para verificar autenticação e carregar dados
  useEffect(() => {
    // Aguardar a verificação de auth terminar
    if (authLoading) return;

    // Redirecionar para login se não autenticado
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }

    const loadData = async () => {
        setIsLoadingCatalog(true);
        try {
            // Load Catalog
            const catalog = await getAiCatalog();
            setProviders(catalog);

            // Set default provider/model if available
            if (catalog.length > 0) {
                const firstProv = catalog[0];
                setSelectedProvider(firstProv.key);
                if (firstProv.models.length > 0) {
                    setSelectedModel(firstProv.models[0].modelKey);
                }
            }
        } catch (error) {
            console.error("Failed to load AI catalog", error);
            toast.error("Erro ao carregar catálogo de IA.");
        } finally {
            setIsLoadingCatalog(false);
        }

        loadHistory();
    };

    loadData();
  }, [authLoading, isAuthenticated, router]);

  const loadHistory = async () => {
    try {
      setLoadingHistory(true);
      const data = await getPromptHistory(10); // Pegar os últimos 10
      setHistory(data);
    } catch (err) {
      console.error('Erro ao carregar histórico:', err);
    } finally {
      setLoadingHistory(false);
    }
  };

  const handleLoadHistoryItem = async (id: string) => {
    try {
      setLoading(true);
      const detail = await getPromptById(id);

      // Preencher o form com os dados do histórico
      setRawPrompt(detail.originalInput);
      setGeneratedPrompt(detail.generatedPrompt);
      setSelectedProvider(detail.provider); // Cast removed, handling string

      // Se houver tipo de prompt salvo, tenta setar
      if (detail.promptType) {
        setSelectedPromptType(detail.promptType as PromptType);
      }

      if (detail.model) {
        setSelectedModel(detail.model);
      }

      toast.success('Prompt carregado do histórico!');

      // Scroll para o topo para ver o resultado
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (err) {
      toast.error('Não foi possível carregar os detalhes do prompt.');
    } finally {
      setLoading(false);
    }
  };

  // Efeito para atualizar o modelo padrão quando o provider muda
  useEffect(() => {
    const provider = providers.find(p => p.key === selectedProvider);
    if (provider && provider.models.length > 0) {
      // Se o modelo atual não pertence a este provider, pega o primeiro da lista
      const modelExists = provider.models.some(m => m.modelKey === selectedModel);
      if (!modelExists) {
        setSelectedModel(provider.models[0].modelKey);
      }
    }
  }, [selectedProvider, selectedModel, providers]);

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
        selectedProvider as PromptProvider,
        selectedPromptType,
        selectedModel
      );

      setGeneratedPrompt(result);
      toast.success('Prompt gerado com sucesso!');

      // Atualizar o histórico após gerar um novo
      loadHistory();
    } catch (err) {
      if (err instanceof PromptGeneratorError) {
        setErrorObj({
          message: err.message,
          isRetryable: !err.isConfiguration(),
          correlationId: err.correlationId
        });

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

  // Mostrar loading enquanto verifica autenticação
  if (authLoading || !isAuthenticated) {
    return (
      <div className="flex justify-center items-center h-screen">
        <RefreshCw className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8 max-w-6xl space-y-8">

      {/* Header */}
      <div className="flex flex-col space-y-2">
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <Wand2 className="h-8 w-8 text-primary" />
          Gerador de Prompts IA
        </h1>
        <p className="text-muted-foreground text-lg">
          Transforme ideias simples em prompts profissionais usando frameworks comprovados.
        </p>
      </div>

      {/* Seleção de Provider (Estilo Tabs/Cards) */}
      <section className="space-y-3">
        <Label className="text-base font-semibold">1. Escolha a Inteligência Artificial</Label>

        {isLoadingCatalog ? (
            <div className="flex gap-4">
                {[1, 2, 3].map(i => <div key={i} className="h-24 w-1/3 bg-muted animate-pulse rounded-xl" />)}
            </div>
        ) : (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
          {providers.map((provider) => (
            <div
              key={provider.key}
              onClick={() => {
                setSelectedProvider(provider.key);
                // Auto select first model
                if (provider.models.length > 0) setSelectedModel(provider.models[0].modelKey);
                else setSelectedModel('');
              }}
              className={`
                cursor-pointer rounded-xl border p-4 transition-all hover:bg-muted/50 relative overflow-hidden
                ${selectedProvider === provider.key
                  ? `ring-2 ring-primary border-transparent bg-primary/5`
                  : 'border-muted hover:border-primary/50'}
              `}
            >
              <div className="flex flex-col items-center justify-center text-center gap-2 h-full z-10 relative">
                <span className={`font-semibold ${selectedProvider === provider.key ? 'text-primary' : 'text-foreground'}`}>
                  {provider.name}
                </span>
                {selectedProvider === provider.key && (
                  <Badge variant="secondary" className="text-[10px] h-5 px-1.5 absolute top-0 right-0 m-2">
                    Ativo
                  </Badge>
                )}
              </div>
            </div>
          ))}
        </div>
        )}
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">

        {/* Coluna Esquerda: Configuração e Input (7 cols) */}
        <div className="lg:col-span-7 space-y-6">

          <Card className="border-muted shadow-sm">
            <CardContent className="p-6 space-y-6">

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Seleção de Modelo */}
                <div className="space-y-2">
                  <Label>Modelo ({currentProvider?.name})</Label>
                  <Select
                    value={selectedModel}
                    onValueChange={setSelectedModel}
                    disabled={!currentProvider || currentProvider.models.length === 0}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione o modelo" />
                    </SelectTrigger>
                    <SelectContent>
                      {currentProvider?.models.map((model) => (
                        <SelectItem key={model.modelKey} value={model.modelKey}>
                           {model.displayName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {/* Seleção de Técnica */}
                <div className="space-y-2">
                  <Label className="flex items-center gap-2">
                    <LayoutTemplate className="w-4 h-4" />
                    Técnica / Framework
                  </Label>
                  <Select value={selectedPromptType} onValueChange={(val) => setSelectedPromptType(val as PromptType)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent className="max-h-[300px]">
                      {promptTypeOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {/* Input com Label Dinâmico */}
              <div className="space-y-3">
                <div className="flex justify-between items-center">
                    <Label htmlFor="input-prompt" className="text-base font-medium">
                      O que você deseja criar?
                    </Label>
                    <span className="text-xs text-muted-foreground">
                       Use palavras-chave simples
                    </span>
                </div>
                <Textarea
                  id="input-prompt"
                  placeholder="Ex: Preciso de um email de vendas frio para oferecer serviços de marketing digital para dentistas..."
                  className="min-h-[160px] resize-none text-base p-4 border-muted focus:border-primary transition-colors"
                  value={rawPrompt}
                  onChange={(e) => setRawPrompt(e.target.value)}
                />
              </div>

              {/* Botão de Ação */}
              <Button
                onClick={handleGenerate}
                disabled={loading || !rawPrompt.trim()}
                className="w-full h-12 text-lg shadow-md transition-all hover:scale-[1.01]"
              >
                {loading ? (
                  <>
                    <RefreshCw className="mr-2 h-5 w-5 animate-spin" />
                    Processando...
                  </>
                ) : (
                  <>
                    <Sparkles className="mr-2 h-5 w-5" />
                    Gerar Prompt Otimizado
                  </>
                )}
              </Button>
            </CardContent>
          </Card>

          {/* Área de Erros */}
          {errorObj && (
            <div className="flex flex-col gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-900 animate-in fade-in slide-in-from-top-2">
              <div className="flex items-start gap-2">
                <AlertCircle className="h-5 w-5 shrink-0 mt-0.5" />
                <div className="flex-1 text-sm font-medium">
                  {errorObj.message}
                </div>
              </div>
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

          {/* Área de Resultados */}
          {generatedPrompt && (
            <div className="space-y-3 animate-in fade-in slide-in-from-bottom-4 duration-500">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                   <Label className="text-lg font-bold text-primary">Resultado</Label>
                   <Badge variant="outline" className="text-xs font-normal">
                      Otimizado para {currentProvider?.name}
                   </Badge>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={copyToClipboard}
                  className="h-9 px-3 hover:bg-muted"
                >
                  {copied ? (
                    <>
                      <Check className="mr-2 h-4 w-4 text-green-600" /> <span className="text-green-600">Copiado</span>
                    </>
                  ) : (
                    <>
                      <Copy className="mr-2 h-4 w-4" /> Copiar Texto
                    </>
                  )}
                </Button>
              </div>
              <div className="relative rounded-lg border bg-muted/30 p-1">
                <Textarea
                  readOnly
                  value={generatedPrompt}
                  className="min-h-[400px] w-full border-0 bg-transparent p-4 font-mono text-sm leading-relaxed focus-visible:ring-0"
                />
              </div>
            </div>
          )}
        </div>

        {/* Coluna Direita: Guia Educacional (5 cols) */}
        <div className="lg:col-span-5 space-y-6">
          <div className="sticky top-6 space-y-6">

            {/* Card Educacional */}
            <Card className="border-l-4 border-l-primary shadow-md overflow-hidden bg-gradient-to-br from-card to-secondary/10">
              <CardHeader className="pb-3 border-b bg-muted/20">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <Lightbulb className="h-5 w-5 text-yellow-500" />
                  Entenda: {selectedTypeDetails?.label || 'Selecione uma técnica'}
                </CardTitle>
              </CardHeader>
              <CardContent className="p-6 space-y-5">

                {selectedTypeDetails ? (
                  <>
                    <div className="space-y-2">
                      <h4 className="flex items-center gap-2 font-semibold text-primary text-sm uppercase tracking-wide">
                        <span className="w-1.5 h-1.5 rounded-full bg-primary" />
                        O que é
                      </h4>
                      <p className="text-sm text-foreground/80 leading-relaxed">
                        {selectedTypeDetails.whatIs}
                      </p>
                    </div>

                    <div className="space-y-2">
                       <h4 className="flex items-center gap-2 font-semibold text-primary text-sm uppercase tracking-wide">
                        <span className="w-1.5 h-1.5 rounded-full bg-primary" />
                        Quando usar
                      </h4>
                      <p className="text-sm text-foreground/80 leading-relaxed">
                        {selectedTypeDetails.whenToUse}
                      </p>
                    </div>

                    <div className="mt-4 pt-4 border-t border-dashed">
                      <div className="flex items-center justify-between mb-2">
                        <h4 className="font-semibold text-xs uppercase text-muted-foreground">Exemplo Prático</h4>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-6 text-xs hover:text-primary hover:bg-primary/10"
                          onClick={() => setRawPrompt(selectedTypeDetails.example)}
                        >
                          <Copy className="w-3 h-3 mr-1" />
                          Usar Exemplo
                        </Button>
                      </div>
                      <div className="bg-muted p-3 rounded-md border text-xs italic text-muted-foreground">
                        "{selectedTypeDetails.example}"
                      </div>
                    </div>
                  </>
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    Selecione uma técnica para ver os detalhes e exemplos.
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Dica Geral */}
            <div className="bg-blue-50 dark:bg-blue-950/20 p-4 rounded-lg border border-blue-100 dark:border-blue-900 text-sm flex gap-3 items-start">
              <div className="mt-0.5 min-w-fit">
                 <Sparkles className="h-4 w-4 text-blue-500" />
              </div>
              <div className="space-y-1">
                <p className="font-medium text-blue-900 dark:text-blue-100">Como funciona?</p>
                <p className="text-blue-800/80 dark:text-blue-200/80 leading-relaxed text-xs">
                  O Gerador pega sua ideia bruta e aplica a estrutura <strong>{selectedTypeDetails?.label.split(' - ')[0]}</strong> para criar um prompt perfeito para o <strong>{currentProvider?.name}</strong>.
                </p>
              </div>
            </div>

          </div>
        </div>
      </div>

      {/* Seção de Histórico */}
      <section className="space-y-4 pt-8 border-t border-dashed">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <History className="h-5 w-5 text-primary" />
            <h2 className="text-xl font-bold tracking-tight">Seus Prompts Recentes</h2>
          </div>
          <Button variant="ghost" size="sm" onClick={loadHistory} disabled={loadingHistory} className="text-xs h-8">
            <RefreshCw className={`h-3 w-3 mr-2 ${loadingHistory ? 'animate-spin' : ''}`} />
            Atualizar
          </Button>
        </div>

        {loadingHistory && history.length === 0 ? (
          <div className="flex items-center justify-center p-12 border rounded-xl bg-muted/5">
            <RefreshCw className="h-6 w-6 animate-spin text-muted-foreground mr-3" />
            <span className="text-muted-foreground">Carregando histórico...</span>
          </div>
        ) : history.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {history.map((item) => (
              <Card key={item.id} className="hover:border-primary/50 transition-colors shadow-sm overflow-hidden flex flex-col group">
                <CardContent className="p-4 flex flex-col h-full space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <Badge variant="outline" className="text-[10px] uppercase font-bold py-0 h-5">
                      {item.promptType || 'Padrão'}
                    </Badge>
                    <div className="flex items-center text-[10px] text-muted-foreground">
                      <Clock className="w-3 h-3 mr-1" />
                      {formatDistanceToNow(new Date(item.createdAt), { addSuffix: true, locale: ptBR })}
                    </div>
                  </div>

                  <p className="text-sm text-foreground/80 line-clamp-3 italic flex-1 bg-muted/30 p-2 rounded border border-muted/50">
                    "{item.inputPreview}..."
                  </p>

                  <div className="flex items-center justify-between pt-2 border-t border-dashed mt-auto">
                    <div className="flex items-center gap-1.5 overflow-hidden">
                       <span className="text-[10px] font-medium text-muted-foreground truncate max-w-[80px]">
                          {item.provider}
                       </span>
                    </div>
                    <Button
                      size="sm"
                      variant="secondary"
                      className="h-7 text-xs px-3 group-hover:bg-primary group-hover:text-primary-foreground transition-all"
                      onClick={() => handleLoadHistoryItem(item.id)}
                    >
                      Ver Detalhes
                      <ExternalLink className="w-3 h-3 ml-1.5" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center p-12 border rounded-xl bg-muted/5 text-center">
            <Sparkles className="h-10 w-10 text-muted/30 mb-3" />
            <h3 className="text-lg font-medium text-muted-foreground">Nenhum prompt salvo ainda</h3>
            <p className="text-sm text-muted-foreground max-w-xs mx-auto mt-1">
              Comece a gerar seus prompts otimizados e eles aparecerão aqui automaticamente.
            </p>
          </div>
        )}
      </section>
    </div>
  );
}
