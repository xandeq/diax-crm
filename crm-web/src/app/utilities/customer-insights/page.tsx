'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { useAiCatalog } from '@/hooks/useAiCatalog';
import { ProviderModelSelector } from '@/components/ai/ProviderModelSelector';
import { generateCustomerInsights, type GenerateInsightsResponse } from '@/services/customerInsights';
import {
  AlertCircle,
  ArrowRight,
  BarChart3,
  CheckCircle,
  Eraser,
  Lightbulb,
  Loader2,
  Sparkles,
  TrendingUp,
  Users,
} from 'lucide-react';
import { useState } from 'react';

const DATE_RANGES = [
  { value: 'last_7_days', label: '7 dias' },
  { value: 'last_30_days', label: '30 dias' },
  { value: 'last_90_days', label: '90 dias' },
  { value: 'last_year', label: '1 ano' },
];

const IMPACT_COLORS: Record<string, string> = {
  alto: 'bg-red-100 text-red-800 border-red-200',
  médio: 'bg-yellow-100 text-yellow-800 border-yellow-200',
  baixo: 'bg-green-100 text-green-800 border-green-200',
};

const CATEGORY_ICONS: Record<string, string> = {
  conversão: '🎯',
  segmentação: '📊',
  engajamento: '💬',
  qualidade: '✅',
  oportunidade: '💡',
};

export default function CustomerInsightsPage() {
  const { providers, selectedProvider, selectedModel, setSelectedProvider, setSelectedModel, isReady } = useAiCatalog();

  const [dateRange, setDateRange] = useState('last_30_days');
  const [focusArea, setFocusArea] = useState('');
  const [result, setResult] = useState<GenerateInsightsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    setResult(null);

    try {
      const response = await generateCustomerInsights({
        provider: selectedProvider,
        model: selectedModel || undefined,
        dateRange,
        focusArea: focusArea || undefined,
        temperature: 0.5,
      });
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar insights');
    } finally {
      setIsLoading(false);
    }
  };

  const handleClear = () => {
    setResult(null);
    setError(null);
    setFocusArea('');
  };

  return (
    <div className="space-y-6 pb-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <BarChart3 className="h-8 w-8 text-emerald-500" />
          Relatório de Insights de Clientes
        </h1>
        <p className="text-muted-foreground mt-2">
          Analise padrões nos seus leads e clientes com IA para tomar decisões data-driven
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Input */}
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
                <Label className="font-semibold">Período</Label>
                <div className="flex flex-wrap gap-2">
                  {DATE_RANGES.map((dr) => (
                    <Button
                      key={dr.value}
                      variant={dateRange === dr.value ? 'default' : 'outline'}
                      size="sm"
                      onClick={() => setDateRange(dr.value)}
                    >
                      {dr.label}
                    </Button>
                  ))}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="focusArea" className="font-semibold">
                  Foco da Análise (Opcional)
                </Label>
                <Input
                  id="focusArea"
                  placeholder="Ex: taxa de conversão, qualidade de leads, segmentação..."
                  value={focusArea}
                  onChange={(e) => setFocusArea(e.target.value)}
                />
              </div>

              {error && (
                <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 flex gap-3 text-sm">
                  <AlertCircle className="h-4 w-4 text-destructive flex-shrink-0 mt-0.5" />
                  <p className="text-destructive">{error}</p>
                </div>
              )}

              <div className="flex gap-2 pt-2">
                <Button onClick={handleGenerate} disabled={isLoading} className="flex-1">
                  {isLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Analisando...
                    </>
                  ) : (
                    <>
                      <Sparkles className="h-4 w-4 mr-2" />
                      Gerar Relatório
                    </>
                  )}
                </Button>
                <Button variant="outline" onClick={handleClear} disabled={isLoading || !result} size="sm">
                  <Eraser className="h-4 w-4" />
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Results */}
        <div className="lg:col-span-2">
          {result ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-semibold">{result.title}</h2>
                <Badge variant="secondary">{result.requestId.slice(0, 8)}...</Badge>
              </div>

              {/* Summary Cards */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <Card>
                  <CardContent className="pt-4 pb-3 text-center">
                    <Users className="h-5 w-5 mx-auto text-blue-500 mb-1" />
                    <p className="text-2xl font-bold">{result.summary.totalLeads}</p>
                    <p className="text-xs text-muted-foreground">Leads</p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4 pb-3 text-center">
                    <CheckCircle className="h-5 w-5 mx-auto text-green-500 mb-1" />
                    <p className="text-2xl font-bold">{result.summary.totalCustomers}</p>
                    <p className="text-xs text-muted-foreground">Clientes</p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4 pb-3 text-center">
                    <TrendingUp className="h-5 w-5 mx-auto text-emerald-500 mb-1" />
                    <p className="text-2xl font-bold">{result.summary.conversionRate}%</p>
                    <p className="text-xs text-muted-foreground">Conversão</p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4 pb-3 text-center">
                    <Sparkles className="h-5 w-5 mx-auto text-amber-500 mb-1" />
                    <p className="text-2xl font-bold">{result.summary.newLeadsInPeriod}</p>
                    <p className="text-xs text-muted-foreground">Novos no período</p>
                  </CardContent>
                </Card>
              </div>

              {/* Patterns */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg flex items-center gap-2">
                    <Lightbulb className="h-5 w-5 text-amber-500" />
                    Padrões Identificados
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {result.patterns.map((pattern, index) => {
                    const impactColor = IMPACT_COLORS[pattern.impact] || IMPACT_COLORS['médio'];
                    const catIcon = CATEGORY_ICONS[pattern.category] || '📋';

                    return (
                      <div key={index} className="p-3 rounded-lg border bg-muted/30">
                        <div className="flex items-center justify-between mb-1">
                          <h4 className="font-medium flex items-center gap-1.5">
                            <span>{catIcon}</span> {pattern.title}
                          </h4>
                          <Badge className={`${impactColor} border text-xs`}>
                            {pattern.impact}
                          </Badge>
                        </div>
                        <p className="text-sm text-muted-foreground">{pattern.description}</p>
                      </div>
                    );
                  })}
                </CardContent>
              </Card>

              {/* Recommendations */}
              {result.recommendations.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg flex items-center gap-2">
                      <ArrowRight className="h-5 w-5 text-blue-500" />
                      Recomendações
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="space-y-2">
                      {result.recommendations.map((rec, index) => (
                        <li key={index} className="flex items-start gap-2 text-sm">
                          <CheckCircle className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                          <span>{rec}</span>
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>
              )}
            </div>
          ) : (
            <Card className="h-full flex items-center justify-center min-h-[400px]">
              <CardContent className="text-center">
                <BarChart3 className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
                <p className="text-muted-foreground">
                  Selecione o período e clique em "Gerar Relatório" para analisar seus dados
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
