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
  SelectValue,
} from '@/components/ui/select';
import { useAiCatalog } from '@/hooks/useAiCatalog';
import { ProviderModelSelector } from '@/components/ai/ProviderModelSelector';
import { humanizeText, humanizeToneOptions, type HumanizeTone } from '@/services/humanizeText';
import { AlertCircle, Check, Copy, Eraser, Loader2, Sparkles, UserCheck } from 'lucide-react';
import { useEffect, useState } from 'react';

export default function HumanizeTextPage() {
  const { providers, selectedProvider, selectedModel, setSelectedProvider, setSelectedModel, isReady } = useAiCatalog();

  const [inputText, setInputText] = useState('');
  const [outputText, setOutputText] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [tone, setTone] = useState<HumanizeTone>('humanize_text_professional');

  const selectedTone = humanizeToneOptions.find((option) => option.value === tone);

  if (!isReady) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const handleConvert = async () => {
    if (!inputText.trim()) {
      setError('Por favor, cole o texto que você deseja humanizar.');
      return;
    }

    setIsLoading(true);
    setError(null);
    setOutputText('');

    try {
      const result = await humanizeText({
        inputText,
        provider: selectedProvider,
        model: selectedModel || undefined,
        tone,
      });
      setOutputText(result.outputText);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao humanizar texto');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async () => {
    if (!outputText) return;

    try {
      await navigator.clipboard.writeText(outputText);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      setError('Erro ao copiar texto');
    }
  };

  const handleClear = () => {
    setInputText('');
    setOutputText('');
    setError(null);
    setCopied(false);
  };

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <Badge variant="section">Utilitários</Badge>
          <h2 className="text-3xl font-serif tracking-tight">Humanizar Texto</h2>
          <p className="text-muted-foreground">
            Transforme textos artificiais ou robóticos em comunicações naturais, fluidas e humanas
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
        {/* Lado Esquerdo: Configurações e Entrada */}
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-xl font-serif">Configurações</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-4">
                <ProviderModelSelector
                  providers={providers}
                  selectedProvider={selectedProvider}
                  selectedModel={selectedModel}
                  onProviderChange={setSelectedProvider}
                  onModelChange={setSelectedModel}
                />

                <div className="space-y-2">
                  <Label>Tom de Texto</Label>
                   <Select
                    value={tone}
                    onValueChange={(val) => setTone(val as HumanizeTone)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {humanizeToneOptions.map((opt) => (
                        <SelectItem key={opt.value} value={opt.value}>
                          {opt.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {selectedTone && (
                <div className="p-3 rounded-lg bg-muted/50 border border-border">
                  <p className="text-sm text-balance">
                    <strong>{selectedTone.label}:</strong> {selectedTone.description}
                  </p>
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0">
              <CardTitle className="text-xl font-serif">Texto de Entrada</CardTitle>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleClear}
                className="h-8 text-muted-foreground hover:text-foreground"
              >
                <Eraser className="h-4 w-4 mr-2" />
                Limpar
              </Button>
            </CardHeader>
            <CardContent className="space-y-4">
              <textarea
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                placeholder="Cole aqui o texto que você quer humanizar..."
                className="w-full min-h-[300px] p-4 rounded-md border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-y"
              />

              <div className="flex justify-between items-center text-xs text-muted-foreground">
                <span>{inputText.length} caracteres</span>
                <span className="italic">Dica: Textos entre 100 e 2000 caracteres funcionam melhor.</span>
              </div>

              <Button
                className="w-full py-6 text-lg font-medium"
                onClick={handleConvert}
                disabled={isLoading || !inputText.trim()}
              >
                {isLoading ? (
                  <>
                    <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                    Convertendo...
                  </>
                ) : (
                  <>
                    <UserCheck className="mr-2 h-5 w-5" />
                    Converter para Humano
                  </>
                )}
              </Button>
            </CardContent>
          </Card>
        </div>

        {/* Lado Direito: Resultado */}
        <div className="space-y-6">
          <Card className="min-h-[500px] bg-muted/30 border-dashed">
            <CardHeader className="flex flex-row items-center justify-between space-y-0">
              <CardTitle className="text-xl font-serif">Texto Humanizado</CardTitle>
              {outputText && (
                <Button variant="outline" size="sm" onClick={handleCopy} className="h-8">
                  {copied ? (
                    <>
                      <Check className="h-4 w-4 mr-2 text-green-500" />
                      Copiado!
                    </>
                  ) : (
                    <>
                      <Copy className="h-4 w-4 mr-2" />
                      Copiar
                    </>
                  )}
                </Button>
              )}
            </CardHeader>
            <CardContent>
              {error ? (
                <div className="flex flex-col items-center justify-center p-8 text-center space-y-3 bg-destructive/10 rounded-lg border border-destructive/20 text-destructive">
                  <AlertCircle className="h-10 w-10 text-destructive" />
                  <div className="space-y-1">
                    <p className="font-semibold">Erro ao processar texto</p>
                    <p className="text-sm opacity-90">{error}</p>
                  </div>
                  <Button variant="outline" size="sm" onClick={handleConvert} className="mt-2">
                    Tentar Novamente
                  </Button>
                </div>
              ) : outputText ? (
                <div className="relative group">
                  <textarea
                    readOnly
                    value={outputText}
                    className="w-full min-h-[450px] p-4 rounded-md border-none bg-transparent text-sm focus:outline-none resize-none font-sans leading-relaxed"
                  />
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center min-h-[400px] text-muted-foreground text-center p-8 space-y-4">
                  <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center">
                    <Sparkles className="h-6 w-6" />
                  </div>
                  <div className="space-y-1">
                    <p className="font-medium text-foreground">Aguardando entrada</p>
                    <p className="text-sm">
                      Insira o texto e clique em converter para ver a mágica acontecer.
                    </p>
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
