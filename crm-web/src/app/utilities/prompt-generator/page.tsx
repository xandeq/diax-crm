'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/contexts/AuthContext';
import { generatePrompt, getPromptModels, promptTypeOptions, type PromptProvider, type PromptType, type ProviderModels } from '@/services/promptGenerator';
import { AlertCircle, Check, Copy, Loader2, Sparkles } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';

const providerOptions: { value: PromptProvider; label: string }[] = [
  { value: 'chatgpt', label: 'ChatGPT' },
  { value: 'perplexity', label: 'Perplexity' },
  { value: 'deepseek', label: 'DeepSeek' },
  { value: 'gemini', label: 'Google Gemini' },
];

export default function PromptGeneratorPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [rawPrompt, setRawPrompt] = useState('');
  const [finalPrompt, setFinalPrompt] = useState('');
  const [provider, setProvider] = useState<PromptProvider>('chatgpt');
  const [availableModels, setAvailableModels] = useState<ProviderModels[]>([]);
  const [selectedModel, setSelectedModel] = useState<string>('');
  const [promptType, setPromptType] = useState<PromptType>('professional');
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingModels, setIsLoadingModels] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [pageReady, setPageReady] = useState(false);

  const currentProviderModels = availableModels.find(m => m.providerId === provider)?.models || [];

  // Load models from backend
  const loadModels = useCallback(async () => {
    try {
      setIsLoadingModels(true);
      const models = await getPromptModels();
      setAvailableModels(models);

      // Load initial values from localStorage
      const savedProvider = localStorage.getItem('diax_prompt_provider');
      const savedModel = localStorage.getItem('diax_prompt_model');

      if (savedProvider && (savedProvider === 'chatgpt' || savedProvider === 'perplexity' || savedProvider === 'deepseek' || savedProvider === 'gemini')) {
        setProvider(savedProvider as PromptProvider);

        // Only set model if it exists for this provider
        const providerData = models.find(m => m.providerId === savedProvider);
        if (savedModel && providerData?.models.some(m => m.id === savedModel)) {
          setSelectedModel(savedModel);
        } else {
          const defaultModel = providerData?.models.find(m => m.isDefault)?.id || providerData?.models[0]?.id || '';
          setSelectedModel(defaultModel);
        }
      } else {
        const defaultProviderData = models.find(m => m.providerId === 'chatgpt');
        const defaultModel = defaultProviderData?.models.find(m => m.isDefault)?.id || defaultProviderData?.models[0]?.id || '';
        setSelectedModel(defaultModel);
      }
    } catch (err) {
      console.error('Failed to load AI models:', err);
    } finally {
      setIsLoadingModels(false);
    }
  }, []);

  useEffect(() => {
    loadModels();
  }, [loadModels]);

  // Persist provider and model
  useEffect(() => {
    if (pageReady) {
      localStorage.setItem('diax_prompt_provider', provider);
      if (selectedModel) {
        localStorage.setItem('diax_prompt_model', selectedModel);
      }
    }
  }, [provider, selectedModel, pageReady]);

  // Reset model when provider changes (if not current)
  const handleProviderChange = (newProvider: PromptProvider) => {
    setProvider(newProvider);
    const providerData = availableModels.find(m => m.providerId === newProvider);
    const defaultModel = providerData?.models.find(m => m.isDefault)?.id || providerData?.models[0]?.id || '';
    setSelectedModel(defaultModel);
  };

  const selectedPromptType = promptTypeOptions.find((option) => option.value === promptType);

  useEffect(() => {
    if (authLoading) return;

    if (!isAuthenticated) {
      router.push('/login');
      return;
    }

    setPageReady(true);
  }, [isAuthenticated, authLoading, router]);

  const handleGenerate = async () => {
    if (!rawPrompt.trim()) {
      setError('Por favor, descreva o que você precisa no prompt');
      return;
    }

    setIsLoading(true);
    setError(null);
    setFinalPrompt('');

    try {
      const result = await generatePrompt(rawPrompt, provider, promptType, selectedModel);
      setFinalPrompt(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar prompt');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async () => {
    if (!finalPrompt) return;

    try {
      await navigator.clipboard.writeText(finalPrompt);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      setError('Erro ao copiar prompt');
    }
  };

  const handleClear = () => {
    setRawPrompt('');
    setFinalPrompt('');
    setError(null);
    setCopied(false);
  };

  if (authLoading || !pageReady) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <Badge variant="section">Utilitários</Badge>
          <h2 className="text-3xl font-serif tracking-tight">Gerador de Prompts Profissionais</h2>
          <p className="text-muted-foreground">
            Transforme um prompt bruto em um prompt claro, estruturado e pronto para uso
          </p>
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-3 p-4 rounded-xl bg-destructive/10 text-destructive border border-destructive/20">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <p className="text-sm font-medium">{error}</p>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Prompt inicial</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <textarea
              value={rawPrompt}
              onChange={(e) => setRawPrompt(e.target.value)}
              placeholder="Descreva rapidamente o que você precisa..."
              className="w-full min-h-[300px] p-4 font-mono text-sm rounded-lg border border-input bg-background resize-y focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
              disabled={isLoading}
            />

            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">Tipo de Prompt</label>
                <select
                  value={promptType}
                  onChange={(e) => setPromptType(e.target.value as PromptType)}
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  disabled={isLoading}
                >
                  {promptTypeOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <p className="text-xs text-muted-foreground">
                  {selectedPromptType?.description}
                </p>
              </div>

              {selectedPromptType && (
                <div className="rounded-lg border border-border bg-muted/30 p-4 text-sm space-y-3">
                  <div>
                    <p className="font-semibold text-foreground">O que é</p>
                    <p className="text-muted-foreground">{selectedPromptType.whatIs}</p>
                  </div>
                  <div>
                    <p className="font-semibold text-foreground">Quando usar</p>
                    <p className="text-muted-foreground">{selectedPromptType.whenToUse}</p>
                  </div>
                  <div>
                    <p className="font-semibold text-foreground">Exemplo</p>
                    <div className="mt-2 rounded-md border border-border bg-background p-3 font-mono text-xs text-muted-foreground whitespace-pre-wrap">
                      {selectedPromptType.example}
                    </div>
                  </div>
                </div>
              )}

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-sm font-medium">Provider</label>
                  <select
                    value={provider}
                    onChange={(e) => handleProviderChange(e.target.value as PromptProvider)}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    disabled={isLoading || isLoadingModels}
                  >
                    {providerOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium flex items-center gap-2">
                    Modelo
                    {isLoadingModels && <Loader2 className="h-3 w-3 animate-spin" />}
                  </label>
                  <select
                    value={selectedModel}
                    onChange={(e) => setSelectedModel(e.target.value)}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    disabled={isLoading || isLoadingModels || currentProviderModels.length === 0}
                  >
                    {/* Unique Categories */}
                    {Array.from(new Set(currentProviderModels.map(m => m.category))).map(category => (
                      <optgroup key={category} label={category}>
                        {currentProviderModels
                          .filter(m => m.category === category)
                          .map(model => (
                            <option key={model.id} value={model.id}>
                              {model.name} {model.isDefault ? '(Padrão)' : ''}
                            </option>
                          ))
                        }
                      </optgroup>
                    ))}
                  </select>
                </div>
              </div>

              <div className="flex gap-2 justify-end">
                <Button
                  onClick={handleGenerate}
                  disabled={isLoading || !rawPrompt.trim() || isLoadingModels}
                  className="min-w-[160px]"
                >
                  {isLoading ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Gerando...
                    </>
                  ) : (
                    <>
                      <Sparkles className="mr-2 h-4 w-4" />
                      Gerar Prompt
                    </>
                  )}
                </Button>
                <Button
                  onClick={handleClear}
                  variant="outline"
                  disabled={isLoading}
                >
                  Limpar
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Prompt profissional</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="relative">
              <textarea
                value={finalPrompt}
                readOnly
                placeholder="O prompt final aparecerá aqui..."
                className="w-full min-h-[300px] p-4 font-mono text-sm rounded-lg border border-input bg-muted/30 resize-y focus:outline-none"
              />
              {finalPrompt && (
                <Button
                  onClick={handleCopy}
                  size="sm"
                  variant="secondary"
                  className="absolute top-2 right-2"
                >
                  {copied ? (
                    <>
                      <Check className="mr-2 h-3 w-3" />
                      Copiado!
                    </>
                  ) : (
                    <>
                      <Copy className="mr-2 h-3 w-3" />
                      Copiar
                    </>
                  )}
                </Button>
              )}
            </div>
            {finalPrompt && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Badge variant="outline" className="font-mono">
                  {finalPrompt.length.toLocaleString()} caracteres
                </Badge>
                <span>•</span>
                <span>{finalPrompt.split('\n').length} linhas</span>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
