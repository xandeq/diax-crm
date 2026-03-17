'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { useAiCatalog } from '@/hooks/useAiCatalog';
import { ProviderModelSelector } from '@/components/ai/ProviderModelSelector';
import { generatePersonas, type GeneratePersonasResponse } from '@/services/leadPersonaGenerator';
import { AlertCircle, Loader2, Sparkles, TrendingUp, Users } from 'lucide-react';
import { useEffect, useState } from 'react';

interface PersonaDisplay {
  id: number;
  name: string;
  title: string;
  companyType: string;
  industry: string;
  painPoints: string[];
  goals: string[];
  budgetRange: string;
  decisionProcess: string;
  effectiveChannels: string[];
  outreachMessages: string[];
  leadExamples: string[];
  percentageOfLeads: number;
}

export default function LeadPersonaGeneratorPage() {
  const { providers, selectedProvider, selectedModel, setSelectedProvider, setSelectedModel, isReady } = useAiCatalog();

  const [personas, setPersonas] = useState<PersonaDisplay[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [requestId, setRequestId] = useState<string | null>(null);
  const [leadsAnalyzed, setLeadsAnalyzed] = useState(0);
  const [focusSegment, setFocusSegment] = useState('');
  const [personaCount, setPersonaCount] = useState(5);

  if (!isReady) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const handleGenerate = async () => {
    setIsLoading(true);
    setError(null);
    setPersonas([]);

    try {
      const response = await generatePersonas({
        provider: selectedProvider,
        model: selectedModel || undefined,
        count: personaCount,
        focusSegment: focusSegment || undefined,
        includeOutreachTips: true,
        temperature: 0.7,
      });

      setPersonas(response.personas);
      setRequestId(response.requestId);
      setLeadsAnalyzed(response.leadsAnalyzed);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar personas');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-6 pb-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <Users className="h-8 w-8 text-purple-500" />
          Gerador de Personas de Leads
        </h1>
        <p className="text-muted-foreground mt-2">
          Analisa seus leads e gera personas detalhadas para outreach mais relevante
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
                <Label htmlFor="personaCount" className="font-semibold">
                  Número de Personas (3-10)
                </Label>
                <Input
                  id="personaCount"
                  type="number"
                  min="3"
                  max="10"
                  value={personaCount}
                  onChange={(e) => setPersonaCount(Math.min(10, Math.max(3, parseInt(e.target.value) || 5)))}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="focusSegment" className="font-semibold">
                  Foco (Opcional)
                </Label>
                <Input
                  id="focusSegment"
                  placeholder="Ex: Hot, Warm, Tech, SaaS..."
                  value={focusSegment}
                  onChange={(e) => setFocusSegment(e.target.value)}
                />
              </div>

              {error && (
                <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 flex gap-3 text-sm">
                  <AlertCircle className="h-4 w-4 text-destructive flex-shrink-0 mt-0.5" />
                  <p className="text-destructive">{error}</p>
                </div>
              )}

              <Button onClick={handleGenerate} disabled={isLoading} className="w-full">
                {isLoading ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Gerando...
                  </>
                ) : (
                  <>
                    <Sparkles className="h-4 w-4 mr-2" />
                    Gerar Personas
                  </>
                )}
              </Button>
            </CardContent>
          </Card>
        </div>

        {/* Results Section */}
        <div className="lg:col-span-2">
          {personas.length > 0 ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-semibold">Personas Geradas</h2>
                {requestId && <Badge variant="secondary">{requestId.slice(0, 8)}...</Badge>}
              </div>

              {leadsAnalyzed > 0 && (
                <p className="text-sm text-muted-foreground">
                  Analisados {leadsAnalyzed} leads da sua base
                </p>
              )}

              <div className="grid gap-4">
                {personas.map((persona) => (
                  <Card key={persona.id} className="hover:border-primary/50 transition-colors">
                    <CardContent className="pt-6">
                      <div className="space-y-4">
                        {/* Header */}
                        <div>
                          <h3 className="text-lg font-semibold">{persona.name}</h3>
                          <p className="text-sm text-muted-foreground">{persona.title}</p>
                        </div>

                        {/* Overview */}
                        <div className="grid grid-cols-2 gap-4 text-sm">
                          <div>
                            <span className="font-medium">Tipo de Empresa:</span> {persona.companyType}
                          </div>
                          <div>
                            <span className="font-medium">Indústria:</span> {persona.industry}
                          </div>
                          <div>
                            <span className="font-medium">Orçamento:</span> {persona.budgetRange}
                          </div>
                          <div className="flex items-center gap-1">
                            <TrendingUp className="h-4 w-4 text-green-600" />
                            <span className="font-medium">{persona.percentageOfLeads}% dos leads</span>
                          </div>
                        </div>

                        {/* Pain Points */}
                        {persona.painPoints.length > 0 && (
                          <div>
                            <h4 className="font-semibold text-sm mb-2">Dores principais</h4>
                            <ul className="space-y-1 text-sm">
                              {persona.painPoints.map((point, idx) => (
                                <li key={idx} className="flex gap-2">
                                  <span className="text-red-500">•</span>
                                  {point}
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}

                        {/* Goals */}
                        {persona.goals.length > 0 && (
                          <div>
                            <h4 className="font-semibold text-sm mb-2">Objetivos</h4>
                            <ul className="space-y-1 text-sm">
                              {persona.goals.map((goal, idx) => (
                                <li key={idx} className="flex gap-2">
                                  <span className="text-green-500">✓</span>
                                  {goal}
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}

                        {/* Outreach Messages */}
                        {persona.outreachMessages.length > 0 && (
                          <div>
                            <h4 className="font-semibold text-sm mb-2">Ângulos de Contato</h4>
                            <div className="space-y-2">
                              {persona.outreachMessages.map((msg, idx) => (
                                <div key={idx} className="p-2 bg-muted rounded text-sm italic">
                                  {msg}
                                </div>
                              ))}
                            </div>
                          </div>
                        )}

                        {/* Channels */}
                        {persona.effectiveChannels.length > 0 && (
                          <div>
                            <h4 className="font-semibold text-sm mb-2">Canais Efetivos</h4>
                            <div className="flex flex-wrap gap-2">
                              {persona.effectiveChannels.map((channel, idx) => (
                                <Badge key={idx} variant="outline" className="text-xs">
                                  {channel}
                                </Badge>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          ) : (
            <Card className="h-full flex items-center justify-center min-h-[400px]">
              <CardContent className="text-center">
                <Users className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
                <p className="text-muted-foreground">
                  Clique em "Gerar Personas" para analisar seus leads
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
