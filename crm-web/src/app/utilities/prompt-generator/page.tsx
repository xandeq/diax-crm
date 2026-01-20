'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/contexts/AuthContext';
import { generatePrompt, promptTypeOptions, type PromptProvider, type PromptType } from '@/services/promptGenerator';
import { AlertCircle, Check, Copy, Loader2, Sparkles } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

const providerOptions: { value: PromptProvider; label: string }[] = [
  { value: 'chatgpt', label: 'ChatGPT' },
  { value: 'perplexity', label: 'Perplexity' },
  { value: 'deepseek', label: 'DeepSeek' },
];

export default function PromptGeneratorPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [rawPrompt, setRawPrompt] = useState('');
  const [finalPrompt, setFinalPrompt] = useState('');
  const [provider, setProvider] = useState<PromptProvider>('chatgpt');
  const [promptType, setPromptType] = useState<PromptType>('professional');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [pageReady, setPageReady] = useState(false);

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
      const result = await generatePrompt(rawPrompt, provider, promptType);
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
                  {promptTypeOptions.find((option) => option.value === promptType)?.description}
                </p>
              </div>

              <div className="grid gap-3 md:grid-cols-[1fr_auto] items-end">
                <div className="space-y-2">
                  <label className="text-sm font-medium">Provider</label>
                  <select
                    value={provider}
                    onChange={(e) => setProvider(e.target.value as PromptProvider)}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    disabled={isLoading}
                  >
                    {providerOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="flex gap-2">
                  <Button
                    onClick={handleGenerate}
                    disabled={isLoading || !rawPrompt.trim()}
                    className="min-w-[160px]"
                  >
                    {isLoading ? (
                      <>
                        <Sparkles className="mr-2 h-4 w-4 animate-spin" />
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
