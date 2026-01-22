'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { extractUrls } from '@/services/htmlExtraction';
import { AlertCircle, Check, Copy, Link2, Sparkles } from 'lucide-react';
import { useMemo, useState } from 'react';

export default function HtmlUrlExtractorPage() {
  const [htmlInput, setHtmlInput] = useState('');
  const [extractedUrls, setExtractedUrls] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const urlsText = useMemo(() => extractedUrls.join('\n'), [extractedUrls]);

  const handleExtract = async () => {
    if (!htmlInput.trim()) {
      setError('Por favor, cole o HTML antes de extrair');
      return;
    }

    setIsLoading(true);
    setError(null);
    setExtractedUrls([]);

    try {
      const urls = await extractUrls(htmlInput);
      setExtractedUrls(urls);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao extrair URLs do HTML');
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
    setHtmlInput('');
    setExtractedUrls([]);
    setError(null);
    setCopied(false);
  };

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <Badge variant="section">Ferramentas</Badge>
          <h2 className="text-3xl font-serif tracking-tight">Extrator de URLs do HTML</h2>
          <p className="text-muted-foreground">
            Cole HTML bruto e extraia URLs de atributos <span className="font-mono">href</span> e{' '}
            <span className="font-mono">src</span>
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
            <CardTitle className="flex items-center gap-2">
              <Link2 className="h-5 w-5" />
              HTML de Entrada
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <textarea
              value={htmlInput}
              onChange={(e) => setHtmlInput(e.target.value)}
              placeholder="Cole seu HTML aqui...&#10;&#10;Exemplo:&#10;&lt;a href=\"https://example.com\"&gt;link&lt;/a&gt;&#10;&lt;img src=\"/assets/img.png\" /&gt;"
              className="w-full min-h-[400px] p-4 font-mono text-sm rounded-lg border border-input bg-background resize-y focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
              disabled={isLoading}
            />
            <div className="flex gap-2">
              <Button
                onClick={handleExtract}
                disabled={isLoading || !htmlInput.trim()}
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

      <Card className="bg-muted/30">
        <CardContent className="pt-6">
          <h3 className="font-semibold mb-3">O que esta ferramenta faz?</h3>
          <ul className="space-y-2 text-sm text-muted-foreground">
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
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">•</span>
              <span>Limite máximo: 5MB de HTML</span>
            </li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
