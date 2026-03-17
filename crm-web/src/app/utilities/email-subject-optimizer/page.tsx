'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import { useAiCatalog } from '@/hooks/useAiCatalog';
import { ProviderModelSelector } from '@/components/ai/ProviderModelSelector';
import { generateSubjectLines, type GenerateSubjectLinesResponse } from '@/services/emailOptimization';
import { AlertCircle, Copy, Eraser, Loader2, Sparkles, TrendingUp } from 'lucide-react';
import { useEffect, useState } from 'react';

interface SubjectLineResult {
  text: string;
  angle: string;
  estimatedOpenRate: number;
  copied?: boolean;
}

export default function EmailSubjectOptimizerPage() {
  const { providers, selectedProvider, selectedModel, setSelectedProvider, setSelectedModel, isReady } = useAiCatalog();

  const [baseMessage, setBaseMessage] = useState('');
  const [targetAudience, setTargetAudience] = useState('');
  const [results, setResults] = useState<SubjectLineResult[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [requestId, setRequestId] = useState<string | null>(null);

  if (!isReady) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const handleGenerate = async () => {
    if (!baseMessage.trim()) {
      setError('Por favor, descreva a mensagem ou email base para otimização.');
      return;
    }

    setIsLoading(true);
    setError(null);
    setResults([]);

    try {
      const response = await generateSubjectLines({
        baseMessage,
        provider: selectedProvider,
        model: selectedModel || undefined,
        targetAudience: targetAudience || undefined,
        temperature: 0.7,
      });

      setResults((response.subjectLines ?? []).map((s) => ({ ...s, copied: false })));
      setRequestId(response.requestId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar subject lines');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async (index: number) => {
    const text = results[index].text;
    try {
      await navigator.clipboard.writeText(text);
      const newResults = [...results];
      newResults[index].copied = true;
      setResults(newResults);
      setTimeout(() => {
        const updatedResults = [...results];
        updatedResults[index].copied = false;
        setResults(updatedResults);
      }, 2000);
    } catch (err) {
      setError('Erro ao copiar subject line');
    }
  };

  const handleClear = () => {
    setBaseMessage('');
    setTargetAudience('');
    setResults([]);
    setError(null);
    setRequestId(null);
  };

  return (
    <div className="space-y-6 pb-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <Sparkles className="h-8 w-8 text-amber-500" />
          Otimizador de Subject Lines de Email
        </h1>
        <p className="text-muted-foreground mt-2">
          Gere subject lines otimizadas com alto potencial de abertura usando IA
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Input Section */}
        <div className="lg:col-span-1 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Configuração</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <ProviderModelSelector
                providers={providers}
                selectedProvider={selectedProvider}
                selectedModel={selectedModel}
                onProviderChange={setSelectedProvider}
                onModelChange={setSelectedModel}
              />

              <div className="space-y-2">
                <Label htmlFor="baseMessage" className="font-semibold">
                  Mensagem Base *
                </Label>
                <Textarea
                  id="baseMessage"
                  placeholder="Descreva o conteúdo principal do email ou a mensagem que será enviada..."
                  value={baseMessage}
                  onChange={(e) => setBaseMessage(e.target.value)}
                  className="min-h-[120px] resize-none"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="targetAudience" className="font-semibold">
                  Público-alvo (Opcional)
                </Label>
                <Input
                  id="targetAudience"
                  placeholder="Ex: Executivos de tech, pequenos negócios, freelancers..."
                  value={targetAudience}
                  onChange={(e) => setTargetAudience(e.target.value)}
                />
              </div>

              {error && (
                <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 flex gap-3 text-sm">
                  <AlertCircle className="h-4 w-4 text-destructive flex-shrink-0 mt-0.5" />
                  <p className="text-destructive">{error}</p>
                </div>
              )}

              <div className="flex gap-2 pt-2">
                <Button onClick={handleGenerate} disabled={isLoading || !baseMessage.trim()} className="flex-1">
                  {isLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Gerando...
                    </>
                  ) : (
                    <>
                      <Sparkles className="h-4 w-4 mr-2" />
                      Gerar
                    </>
                  )}
                </Button>
                <Button
                  variant="outline"
                  onClick={handleClear}
                  disabled={isLoading || (!baseMessage && !results.length)}
                  size="sm"
                >
                  <Eraser className="h-4 w-4" />
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Results Section */}
        <div className="lg:col-span-2">
          {results.length > 0 ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-semibold">Subject Lines Otimizadas</h2>
                {requestId && <Badge variant="secondary">{requestId.slice(0, 8)}...</Badge>}
              </div>

              <div className="grid gap-3">
                {results.map((subject, index) => (
                  <Card key={index} className="hover:border-primary/50 transition-colors">
                    <CardContent className="pt-6">
                      <div className="space-y-3">
                        {/* Subject text */}
                        <div>
                          <p className="font-mono text-sm break-words bg-muted p-3 rounded border">
                            {subject.text}
                          </p>
                        </div>

                        {/* Metadata row */}
                        <div className="flex items-center justify-between gap-2 flex-wrap">
                          <div className="flex items-center gap-2">
                            <Badge variant="outline" className="text-xs">
                              {subject.angle}
                            </Badge>
                          </div>

                          <div className="flex items-center gap-4">
                            <div className="flex items-center gap-1">
                              <TrendingUp className="h-4 w-4 text-green-600" />
                              <span className="text-sm font-medium">
                                {(subject.estimatedOpenRate * 100).toFixed(0)}%
                              </span>
                            </div>

                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleCopy(index)}
                              className="text-muted-foreground hover:text-foreground"
                            >
                              <Copy className="h-4 w-4" />
                            </Button>
                          </div>
                        </div>

                        {/* Copied feedback */}
                        {subject.copied && (
                          <p className="text-xs text-green-600 font-medium">✓ Copiado para área de transferência</p>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>

              <Card className="border-dashed">
                <CardContent className="pt-6">
                  <div className="text-sm text-muted-foreground space-y-2">
                    <p>
                      <strong>Dica:</strong> Os subject lines foram classificados por técnica (ângulo) e taxa estimada de
                      abertura.
                    </p>
                    <p>Teste-os com seu público para encontrar o que funciona melhor. Cada email é único!</p>
                  </div>
                </CardContent>
              </Card>
            </div>
          ) : (
            <Card className="h-full flex items-center justify-center min-h-[400px]">
              <CardContent className="text-center">
                <Sparkles className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
                <p className="text-muted-foreground">
                  Preencha os campos e clique em "Gerar" para criar subject lines otimizadas
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
