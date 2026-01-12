'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { extractText } from '@/services/htmlExtraction';
import { AlertCircle, Check, Copy, FileText, Sparkles } from 'lucide-react';
import { useState } from 'react';

export default function HtmlExtractorPage() {
  const [htmlInput, setHtmlInput] = useState('');
  const [extractedText, setExtractedText] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const handleExtract = async () => {
    if (!htmlInput.trim()) {
      setError('Por favor, cole o HTML antes de extrair');
      return;
    }

    setIsLoading(true);
    setError(null);
    setExtractedText('');

    try {
      const text = await extractText(htmlInput);
      setExtractedText(text);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao extrair texto do HTML');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async () => {
    if (!extractedText) return;

    try {
      await navigator.clipboard.writeText(extractedText);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      setError('Erro ao copiar texto');
    }
  };

  const handleClear = () => {
    setHtmlInput('');
    setExtractedText('');
    setError(null);
    setCopied(false);
  };

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <Badge variant="section">Ferramentas</Badge>
          <h2 className="text-3xl font-serif tracking-tight">Extrator de Texto HTML</h2>
          <p className="text-muted-foreground">
            Cole HTML bruto e extraia apenas o texto visível, sem scripts, estilos ou tags
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
        {/* Input Card */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5" />
              HTML de Entrada
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <textarea
              value={htmlInput}
              onChange={(e) => setHtmlInput(e.target.value)}
              placeholder="Cole seu HTML aqui...&#10;&#10;Exemplo:&#10;<html>&#10;  <head>&#10;    <script>alert('test')</script>&#10;  </head>&#10;  <body>&#10;    <h1>Título</h1>&#10;    <p>Conteúdo visível</p>&#10;  </body>&#10;</html>"
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
                    Extrair Texto
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
          </CardContent>
        </Card>

        {/* Output Card */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Texto Extraído
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="relative">
              <textarea
                value={extractedText}
                readOnly
                placeholder="O texto extraído aparecerá aqui..."
                className="w-full min-h-[400px] p-4 font-mono text-sm rounded-lg border border-input bg-muted/30 resize-y focus:outline-none"
              />
              {extractedText && (
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
            {extractedText && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Badge variant="outline" className="font-mono">
                  {extractedText.length.toLocaleString()} caracteres
                </Badge>
                <span>•</span>
                <span>{extractedText.split('\n').length} linhas</span>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Info Section */}
      <Card className="bg-muted/30">
        <CardContent className="pt-6">
          <h3 className="font-semibold mb-3">O que esta ferramenta faz?</h3>
          <ul className="space-y-2 text-sm text-muted-foreground">
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">•</span>
              <span>Remove completamente tags <code className="text-xs bg-background px-1 py-0.5 rounded">&lt;script&gt;</code>, <code className="text-xs bg-background px-1 py-0.5 rounded">&lt;style&gt;</code> e <code className="text-xs bg-background px-1 py-0.5 rounded">&lt;head&gt;</code></span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">•</span>
              <span>Extrai apenas texto visível ao usuário final</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">•</span>
              <span>Remove comentários HTML e tags de marcação</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">•</span>
              <span>Limpa espaços em branco excessivos e formata o texto</span>
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
