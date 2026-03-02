'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { extractUrls as extractUrlsFromHtml } from '@/services/htmlExtraction';
import { AlertCircle, Check, Copy, FileCode2, FileJson, Link2, Sparkles } from 'lucide-react';
import { useMemo, useState } from 'react';

function extractUrlsFromJson(jsonText: string): string[] {
  try {
    const data = JSON.parse(jsonText);
    const urls = new Set<string>();

    function traverse(obj: any) {
      if (typeof obj === 'string') {
        const trimmed = obj.trim();
        // Extrai todas as URLs do valor string
        const matches = trimmed.match(/https?:\/\/[^\s"']+/g);
        if (matches) {
          matches.forEach(m => urls.add(m));
        }
      } else if (Array.isArray(obj)) {
        obj.forEach(traverse);
      } else if (obj !== null && typeof obj === 'object') {
        Object.values(obj).forEach(traverse);
      }
    }

    traverse(data);
    return Array.from(urls);
  } catch (error) {
    throw new Error('O JSON fornecido é inválido. Verifique a formatação.');
  }
}

export default function HtmlUrlExtractorPage() {
  const [mode, setMode] = useState<'html' | 'json'>('html');
  const [inputText, setInputText] = useState('');
  const [extractedUrls, setExtractedUrls] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const urlsText = useMemo(() => extractedUrls.join('\n'), [extractedUrls]);

  const htmlPlaceholder = `Cole seu HTML aqui...

Exemplo:
<a href="https://example.com">link</a>
<img src="/assets/img.png" />`;

  const jsonPlaceholder = `Cole seu JSON aqui...

Exemplo:
[
  { "website": "https://www.marisa.com.br/" },
  { "website": "https://www.cea.com.br/" }
]`;

  const handleExtract = async () => {
    if (!inputText.trim()) {
      setError(`Por favor, cole o ${mode.toUpperCase()} antes de extrair`);
      return;
    }

    setIsLoading(true);
    setError(null);
    setExtractedUrls([]);

    try {
      if (mode === 'html') {
        const urls = await extractUrlsFromHtml(inputText);
        setExtractedUrls(urls);
      } else {
        const urls = extractUrlsFromJson(inputText);
        setExtractedUrls(urls);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao extrair URLs do ' + mode.toUpperCase());
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async () => {
    if (!urlsText) return;

    try {
      await navigator.clipboard.writeText(urlsText);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      setError('Erro ao copiar URLs');
    }
  };

  const handleClear = () => {
    setInputText('');
    setExtractedUrls([]);
    setError(null);
    setCopied(false);
  };

  const handleModeChange = (newMode: string) => {
    setMode(newMode as 'html' | 'json');
    handleClear();
  };

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <Badge variant="section">Ferramentas</Badge>
          <h2 className="text-3xl font-serif tracking-tight">Extrator de URLs</h2>
          <p className="text-muted-foreground">
            Cole conteúdo HTML ou JSON e extraia automaticamente URLs válidas.
          </p>
        </div>
      </div>

      <Tabs value={mode} onValueChange={handleModeChange} className="w-full">
        <TabsList className="mb-4">
          <TabsTrigger value="html" className="flex items-center gap-2">
            <FileCode2 className="w-4 h-4" />
            HTML
          </TabsTrigger>
          <TabsTrigger value="json" className="flex items-center gap-2">
            <FileJson className="w-4 h-4" />
            JSON
          </TabsTrigger>
        </TabsList>

        {error && (
          <div className="flex items-center gap-3 p-4 mb-6 rounded-xl bg-destructive/10 text-destructive border border-destructive/20">
            <AlertCircle className="h-5 w-5 flex-shrink-0" />
            <p className="text-sm font-medium">{error}</p>
          </div>
        )}

        <div className="grid gap-6 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {mode === 'html' ? <FileCode2 className="h-5 w-5" /> : <FileJson className="h-5 w-5" />}
                Entrada ({mode.toUpperCase()})
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <textarea
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                placeholder={mode === 'html' ? htmlPlaceholder : jsonPlaceholder}
                className="w-full min-h-[400px] p-4 font-mono text-sm rounded-lg border border-input bg-background resize-y focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
                disabled={isLoading}
              />
              <div className="flex gap-2">
                <Button
                  onClick={handleExtract}
                  disabled={isLoading || !inputText.trim()}
                  className="flex-1"
                >
                  {isLoading ? (
                    <>
                      <Sparkles className="mr-2 h-4 w-4 animate-spin" />
                      Extraindo...
                    </>
                  ) : (
                    <>
                      <Sparkles className="mr-2 h-4 w-4" />
                      Extrair URLs
                    </>
                  )}
                </Button>
                <Button onClick={handleClear} variant="outline" disabled={isLoading}>
                  Limpar
                </Button>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Link2 className="h-5 w-5" />
                URLs Extraídas
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="relative">
                <textarea
                  value={urlsText}
                  readOnly
                  placeholder="As URLs extraídas aparecerão aqui..."
                  className="w-full min-h-[400px] p-4 font-mono text-sm rounded-lg border border-input bg-muted/30 resize-y focus:outline-none"
                />
                {urlsText && (
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
              {extractedUrls.length > 0 && (
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Badge variant="outline" className="font-mono">
                    {extractedUrls.length.toLocaleString()} URLs
                  </Badge>
                  <span>•</span>
                  <span>{urlsText.length.toLocaleString()} caracteres</span>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </Tabs>

      <Card className="bg-muted/30">
        <CardContent className="pt-6">
          <h3 className="font-semibold mb-3">O que esta ferramenta faz?</h3>
          <ul className="space-y-2 text-sm text-muted-foreground">
            {mode === 'html' ? (
              <>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Extrai URLs de atributos <span className="font-mono">href</span> e <span className="font-mono">src</span></span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Remove duplicadas e mantém a ordem de aparição</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Não executa scripts nem renderiza o HTML</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Ignora URLs perigosas como <span className="font-mono">javascript:</span> e <span className="font-mono">data:</span></span>
                </li>
              </>
            ) : (
              <>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Varre todos os valores dentro do objeto JSON</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Extrai textos que começam com <span className="font-mono">http://</span> ou <span className="font-mono">https://</span></span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Suporta Arrays, Objetos aninhados e Strings cruas</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">•</span>
                  <span>Remove URLs duplicadas e formata linha a linha</span>
                </li>
              </>
            )}
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">•</span>
              <span>Limite recomendado: até 5MB de texto colado</span>
            </li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
