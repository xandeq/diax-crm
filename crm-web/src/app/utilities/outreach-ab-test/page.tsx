'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import { useAiCatalog } from '@/hooks/useAiCatalog';
import { ProviderModelSelector } from '@/components/ai/ProviderModelSelector';
import { generateAbVariations, type OutreachVariationDto } from '@/services/outreachAbTest';
import { AlertCircle, Copy, Eraser, Loader2, Mail, MessageCircle, Sparkles, TrendingUp } from 'lucide-react';
import { useState } from 'react';

const TONE_COLORS: Record<string, string> = {
  profissional: 'bg-blue-100 text-blue-800 border-blue-200',
  casual: 'bg-green-100 text-green-800 border-green-200',
  urgente: 'bg-orange-100 text-orange-800 border-orange-200',
};

const TONE_ICONS: Record<string, string> = {
  profissional: '💼',
  casual: '😊',
  urgente: '⚡',
};

interface VariationWithCopy extends OutreachVariationDto {
  copiedSubject?: boolean;
  copiedBody?: boolean;
}

export default function OutreachAbTestPage() {
  const { providers, selectedProvider, selectedModel, setSelectedProvider, setSelectedModel, isReady } = useAiCatalog();

  const [baseMessage, setBaseMessage] = useState('');
  const [targetAudience, setTargetAudience] = useState('');
  const [industry, setIndustry] = useState('');
  const [goal, setGoal] = useState('');
  const [results, setResults] = useState<VariationWithCopy[]>([]);
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
      setError('Por favor, descreva a mensagem base ou oferta para gerar variações.');
      return;
    }

    setIsLoading(true);
    setError(null);
    setResults([]);

    try {
      const response = await generateAbVariations({
        baseMessage,
        provider: selectedProvider,
        model: selectedModel || undefined,
        targetAudience: targetAudience || undefined,
        industry: industry || undefined,
        goal: goal || undefined,
        temperature: 0.8,
      });

      setResults(response.variations.map((v) => ({ ...v, copiedSubject: false, copiedBody: false })));
      setRequestId(response.requestId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar variações de outreach');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async (index: number, field: 'subject' | 'body') => {
    const text = field === 'subject' ? results[index].subject : results[index].body;
    try {
      await navigator.clipboard.writeText(text);
      const newResults = [...results];
      if (field === 'subject') newResults[index].copiedSubject = true;
      else newResults[index].copiedBody = true;
      setResults(newResults);
      setTimeout(() => {
        setResults((prev) => {
          const updated = [...prev];
          if (field === 'subject') updated[index].copiedSubject = false;
          else updated[index].copiedBody = false;
          return updated;
        });
      }, 2000);
    } catch {
      setError('Erro ao copiar texto');
    }
  };

  const handleClear = () => {
    setBaseMessage('');
    setTargetAudience('');
    setIndustry('');
    setGoal('');
    setResults([]);
    setError(null);
    setRequestId(null);
  };

  return (
    <div className="space-y-6 pb-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <MessageCircle className="h-8 w-8 text-violet-500" />
          Teste A/B de Outreach
        </h1>
        <p className="text-muted-foreground mt-2">
          Gere 3 variações de mensagem de outreach com tons diferentes para descobrir o que funciona melhor
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
                  Mensagem Base / Oferta *
                </Label>
                <Textarea
                  id="baseMessage"
                  placeholder="Descreva sua oferta, serviço ou proposta que será enviada por outreach..."
                  value={baseMessage}
                  onChange={(e) => setBaseMessage(e.target.value)}
                  className="min-h-[120px] resize-none"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="targetAudience" className="font-semibold">
                  Público-alvo
                </Label>
                <Input
                  id="targetAudience"
                  placeholder="Ex: CTOs de startups, donos de restaurantes..."
                  value={targetAudience}
                  onChange={(e) => setTargetAudience(e.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="industry" className="font-semibold">
                  Indústria / Nicho
                </Label>
                <Input
                  id="industry"
                  placeholder="Ex: SaaS, E-commerce, Saúde..."
                  value={industry}
                  onChange={(e) => setIndustry(e.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="goal" className="font-semibold">
                  Objetivo
                </Label>
                <Input
                  id="goal"
                  placeholder="Ex: Agendar reunião, vender serviço, gerar lead..."
                  value={goal}
                  onChange={(e) => setGoal(e.target.value)}
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
                      Gerar Variações
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
                <h2 className="text-xl font-semibold">Variações Geradas</h2>
                {requestId && <Badge variant="secondary">{requestId.slice(0, 8)}...</Badge>}
              </div>

              <div className="grid gap-4">
                {results.map((variation, index) => {
                  const toneKey = variation.tone.toLowerCase();
                  const toneColor = TONE_COLORS[toneKey] || 'bg-gray-100 text-gray-800 border-gray-200';
                  const toneIcon = TONE_ICONS[toneKey] || '📝';

                  return (
                    <Card key={index} className="hover:border-primary/50 transition-colors">
                      <CardHeader className="pb-3">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <Badge variant="outline" className="text-lg font-bold px-3 py-1">
                              {variation.label}
                            </Badge>
                            <Badge className={`${toneColor} border`}>
                              {toneIcon} {variation.tone}
                            </Badge>
                          </div>
                          <div className="flex items-center gap-1">
                            <TrendingUp className="h-4 w-4 text-green-600" />
                            <span className="text-sm font-bold text-green-700">
                              {(variation.estimatedResponseRate * 100).toFixed(0)}% resp. rate
                            </span>
                          </div>
                        </div>
                      </CardHeader>
                      <CardContent className="space-y-4">
                        {/* Subject */}
                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <Label className="text-xs text-muted-foreground uppercase tracking-wider flex items-center gap-1">
                              <Mail className="h-3 w-3" /> Subject Line
                            </Label>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleCopy(index, 'subject')}
                              className="h-6 px-2 text-xs"
                            >
                              {variation.copiedSubject ? '✓ Copiado' : <Copy className="h-3 w-3" />}
                            </Button>
                          </div>
                          <p className="font-mono text-sm bg-muted p-2.5 rounded border">{variation.subject}</p>
                        </div>

                        {/* Body */}
                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <Label className="text-xs text-muted-foreground uppercase tracking-wider">Mensagem</Label>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleCopy(index, 'body')}
                              className="h-6 px-2 text-xs"
                            >
                              {variation.copiedBody ? '✓ Copiado' : <Copy className="h-3 w-3" />}
                            </Button>
                          </div>
                          <div className="text-sm bg-muted p-3 rounded border whitespace-pre-wrap max-h-[300px] overflow-y-auto">
                            {variation.body}
                          </div>
                        </div>

                        {/* Rationale */}
                        <div className="pt-2 border-t">
                          <p className="text-xs text-muted-foreground italic">
                            <strong>Por que funciona:</strong> {variation.rationale}
                          </p>
                        </div>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>

              <Card className="border-dashed">
                <CardContent className="pt-6">
                  <div className="text-sm text-muted-foreground space-y-2">
                    <p>
                      <strong>Como usar:</strong> Envie cada variação para um grupo diferente de leads (mesmo tamanho)
                      e compare as taxas de resposta após 7 dias.
                    </p>
                    <p>
                      A variável <code className="bg-muted px-1 rounded">{'{{FirstName}}'}</code> será substituída
                      automaticamente pelo nome do lead ao enviar via outreach.
                    </p>
                  </div>
                </CardContent>
              </Card>
            </div>
          ) : (
            <Card className="h-full flex items-center justify-center min-h-[400px]">
              <CardContent className="text-center">
                <MessageCircle className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
                <p className="text-muted-foreground">
                  Descreva sua oferta e clique em "Gerar Variações" para criar 3 mensagens A/B/C
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
