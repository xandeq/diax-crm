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
import { useAuth } from '@/contexts/AuthContext';
import { getAiCatalog, type AiProvider } from '@/services/aiCatalog';
import {
  generateImage,
  imageSizeOptions,
  imageStyleOptions,
  imageQualityOptions,
  type ImageGenerationResponse,
} from '@/services/imageGeneration';
import {
  AlertCircle,
  ChevronDown,
  ChevronUp,
  Download,
  Image as ImageIcon,
  Loader2,
  Sparkles,
  Wand2,
  X,
  ZoomIn,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useRef, useState } from 'react';

export default function ImageGenerationPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  // AI config
  const [providers, setProviders] = useState<AiProvider[]>([]);
  const [selectedProvider, setSelectedProvider] = useState('');
  const [selectedModel, setSelectedModel] = useState('');
  const [loadingProviders, setLoadingProviders] = useState(true);

  // Prompt & parameters
  const [prompt, setPrompt] = useState('');
  const [negativePrompt, setNegativePrompt] = useState('');
  const [selectedSize, setSelectedSize] = useState('1024x1024');
  const [style, setStyle] = useState('vivid');
  const [quality, setQuality] = useState('standard');
  const [showAdvanced, setShowAdvanced] = useState(false);

  // Generation state
  const [isGenerating, setIsGenerating] = useState(false);
  const [result, setResult] = useState<ImageGenerationResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pageReady, setPageReady] = useState(false);

  // Lightbox
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  // Prompt textarea auto-resize
  const promptRef = useRef<HTMLTextAreaElement>(null);

  // Derived state — filter to only image-capable models
  const imageProviders = providers
    .filter(p => p.models.some(m => m.isEnabled && m.supportsImage))
    .map(p => ({ ...p, models: p.models.filter(m => m.isEnabled && m.supportsImage) }));
  const currentProvider = imageProviders.find(p => p.key === selectedProvider);
  const currentModels = currentProvider?.models || [];
  const sizeOption = imageSizeOptions.find(s => s.value === selectedSize);

  // Load providers — auto-select first image-capable provider/model
  useEffect(() => {
    async function loadProviders() {
      try {
        const catalog = await getAiCatalog();
        const enabledProviders = catalog.filter(p => p.isEnabled);
        setProviders(enabledProviders);

        if (enabledProviders.length > 0 && !selectedProvider) {
          // Find first provider that has at least one image-capable model
          const imageProvider = enabledProviders.find(p =>
            p.models.some(m => m.isEnabled && m.supportsImage)
          );
          if (imageProvider) {
            setSelectedProvider(imageProvider.key);
            const imageModel = imageProvider.models.find(m => m.isEnabled && m.supportsImage);
            if (imageModel) {
              setSelectedModel(imageModel.modelKey);
            }
          } else {
            // Fallback: select first provider even if no image models
            setSelectedProvider(enabledProviders[0].key);
          }
        }
      } catch {
        setError('Erro ao carregar provedores de IA. Recarregue a página.');
      } finally {
        setLoadingProviders(false);
      }
    }

    if (isAuthenticated) {
      loadProviders();
    }
  }, [isAuthenticated, selectedProvider]);

  // Auth guard
  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }
    setPageReady(true);
  }, [isAuthenticated, authLoading, router]);

  const handleGenerate = useCallback(async () => {
    if (!prompt.trim()) {
      setError('Digite um prompt descrevendo a imagem que deseja gerar.');
      return;
    }

    setIsGenerating(true);
    setError(null);
    setResult(null);

    try {
      const size = imageSizeOptions.find(s => s.value === selectedSize);
      const response = await generateImage({
        provider: selectedProvider,
        model: selectedModel || '',
        prompt: prompt.trim(),
        negativePrompt: negativePrompt.trim() || undefined,
        width: size?.width ?? 1024,
        height: size?.height ?? 1024,
        numberOfImages: 1,
        style,
        quality,
      });
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar imagem.');
    } finally {
      setIsGenerating(false);
    }
  }, [prompt, negativePrompt, selectedProvider, selectedModel, selectedSize, style, quality]);

  // Keyboard shortcut: Ctrl+Enter to generate
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter' && !isGenerating && prompt.trim()) {
        e.preventDefault();
        handleGenerate();
      }
    },
    [handleGenerate, isGenerating, prompt]
  );

  if (authLoading || !pageReady || loadingProviders) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <Badge variant="section">Utilitários</Badge>
          <h2 className="text-3xl font-serif tracking-tight">Geração de Imagens</h2>
          <p className="text-muted-foreground">
            Crie imagens únicas com inteligência artificial — descreva e a IA materializa sua visão
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-8 items-start">
        {/* Left Column: Config + Prompt (2/5 width) */}
        <div className="lg:col-span-2 space-y-6">
          {/* Provider & Model */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg font-serif flex items-center gap-2">
                <Wand2 className="h-4 w-4 text-accent" />
                Provedor & Modelo
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>IA (Provedor)</Label>
                <Select
                  value={selectedProvider}
                  onValueChange={(val) => {
                    setSelectedProvider(val);
                    const prov = imageProviders.find(p => p.key === val);
                    if (prov) {
                      setSelectedModel(prov.models.length > 0 ? prov.models[0].modelKey : '');
                    }
                  }}
                  disabled={imageProviders.length === 0}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={imageProviders.length === 0 ? 'Nenhum provedor com modelo de imagem' : 'Selecione o provedor'} />
                  </SelectTrigger>
                  <SelectContent>
                    {imageProviders.map(p => (
                      <SelectItem key={p.key} value={p.key}>{p.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label>Modelo</Label>
                <Select
                  value={selectedModel}
                  onValueChange={setSelectedModel}
                  disabled={!currentProvider || currentModels.length === 0}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={currentModels.length === 0 ? 'Nenhum modelo' : 'Selecione o modelo'} />
                  </SelectTrigger>
                  <SelectContent>
                    {currentModels.map(m => (
                      <SelectItem key={m.modelKey} value={m.modelKey}>{m.displayName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>

          {/* Prompt */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg font-serif flex items-center gap-2">
                <Sparkles className="h-4 w-4 text-accent" />
                Prompt
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>Descreva a imagem</Label>
                <textarea
                  ref={promptRef}
                  value={prompt}
                  onChange={e => setPrompt(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Ex: Uma paisagem de montanha ao pôr do sol com névoa suave nos vales, estilo fotorrealista, iluminação dourada..."
                  className="w-full min-h-[140px] p-4 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-y leading-relaxed placeholder:text-muted-foreground/60"
                />
                <div className="flex justify-between items-center text-xs text-muted-foreground">
                  <span>{prompt.length} caracteres</span>
                  <span className="font-mono text-[10px] opacity-60">Ctrl+Enter para gerar</span>
                </div>
              </div>

              {/* Advanced Options Toggle */}
              <button
                type="button"
                onClick={() => setShowAdvanced(!showAdvanced)}
                className="flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
              >
                {showAdvanced ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
                Opções avançadas
              </button>

              {showAdvanced && (
                <div className="space-y-4 pt-2 border-t border-border/50">
                  <div className="space-y-2">
                    <Label className="text-xs">Prompt Negativo</Label>
                    <textarea
                      value={negativePrompt}
                      onChange={e => setNegativePrompt(e.target.value)}
                      placeholder="O que evitar: baixa qualidade, desfocado, texto, marca d'água..."
                      className="w-full min-h-[80px] p-3 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-y placeholder:text-muted-foreground/60"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-2">
                      <Label className="text-xs">Dimensão</Label>
                      <Select value={selectedSize} onValueChange={setSelectedSize}>
                        <SelectTrigger className="h-10 text-xs">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {imageSizeOptions.map(s => (
                            <SelectItem key={s.value} value={s.value}>{s.label}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label className="text-xs">Estilo</Label>
                      <Select value={style} onValueChange={setStyle}>
                        <SelectTrigger className="h-10 text-xs">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {imageStyleOptions.map(s => (
                            <SelectItem key={s.value} value={s.value}>{s.label}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label className="text-xs">Qualidade</Label>
                    <Select value={quality} onValueChange={setQuality}>
                      <SelectTrigger className="h-10 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {imageQualityOptions.map(q => (
                          <SelectItem key={q.value} value={q.value}>{q.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              )}

              {/* Generate Button */}
              <Button
                className="w-full py-6 text-base font-medium"
                onClick={handleGenerate}
                disabled={isGenerating || !prompt.trim()}
              >
                {isGenerating ? (
                  <>
                    <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                    Gerando imagem...
                  </>
                ) : (
                  <>
                    <ImageIcon className="mr-2 h-5 w-5" />
                    Gerar Imagem
                  </>
                )}
              </Button>
            </CardContent>
          </Card>

          {/* Generation info */}
          {result && (
            <div className="px-4 py-3 rounded-xl bg-muted/50 border border-border text-xs text-muted-foreground space-y-1">
              <div className="flex justify-between">
                <span>Provedor</span>
                <span className="font-medium text-foreground">{result.providerUsed}</span>
              </div>
              <div className="flex justify-between">
                <span>Modelo</span>
                <span className="font-medium text-foreground">{result.modelUsed}</span>
              </div>
              <div className="flex justify-between">
                <span>Tempo</span>
                <span className="font-medium text-foreground">{(result.durationMs / 1000).toFixed(1)}s</span>
              </div>
              {sizeOption && (
                <div className="flex justify-between">
                  <span>Dimensão</span>
                  <span className="font-medium text-foreground">{sizeOption.width} x {sizeOption.height}</span>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Right Column: Result (3/5 width) */}
        <div className="lg:col-span-3">
          <Card className="min-h-[600px] overflow-hidden">
            <CardHeader className="flex flex-row items-center justify-between space-y-0">
              <CardTitle className="text-lg font-serif">Resultado</CardTitle>
              {result && result.images.length > 0 && (
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    className="h-8 text-xs"
                    onClick={() => {
                      const img = result.images[0];
                      const url = img.isBase64 ? `data:image/png;base64,${img.imageUrl}` : img.imageUrl;
                      setLightboxUrl(url);
                    }}
                  >
                    <ZoomIn className="h-3.5 w-3.5 mr-1.5" />
                    Ampliar
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    className="h-8 text-xs"
                    asChild
                  >
                    <a
                      href={result.images[0].isBase64
                        ? `data:image/png;base64,${result.images[0].imageUrl}`
                        : result.images[0].imageUrl}
                      download={`diax-image-${Date.now()}.png`}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      <Download className="h-3.5 w-3.5 mr-1.5" />
                      Download
                    </a>
                  </Button>
                </div>
              )}
            </CardHeader>
            <CardContent>
              {error ? (
                <div className="flex flex-col items-center justify-center p-8 text-center space-y-3 bg-destructive/10 rounded-xl border border-destructive/20 text-destructive">
                  <AlertCircle className="h-10 w-10" />
                  <div className="space-y-1">
                    <p className="font-semibold">Erro na geração</p>
                    <p className="text-sm opacity-90">{error}</p>
                  </div>
                  <Button variant="outline" size="sm" onClick={handleGenerate} className="mt-2">
                    Tentar Novamente
                  </Button>
                </div>
              ) : isGenerating ? (
                <div className="flex flex-col items-center justify-center min-h-[500px] space-y-6">
                  {/* Animated generation indicator */}
                  <div className="relative">
                    <div className="h-24 w-24 rounded-2xl bg-gradient-to-br from-accent/20 to-accent-secondary/20 flex items-center justify-center">
                      <Wand2 className="h-10 w-10 text-accent animate-pulse" />
                    </div>
                    <div className="absolute -inset-2 rounded-3xl border-2 border-accent/20 animate-ping" style={{ animationDuration: '2s' }} />
                  </div>
                  <div className="text-center space-y-2">
                    <p className="font-medium text-foreground">Criando sua imagem...</p>
                    <p className="text-sm text-muted-foreground">
                      Isso pode levar de 5 a 30 segundos dependendo do provedor
                    </p>
                  </div>
                  {/* Animated progress dots */}
                  <div className="flex gap-1.5">
                    {[0, 1, 2, 3, 4].map(i => (
                      <div
                        key={i}
                        className="h-1.5 w-1.5 rounded-full bg-accent"
                        style={{
                          animation: 'pulse 1.4s ease-in-out infinite',
                          animationDelay: `${i * 0.2}s`,
                        }}
                      />
                    ))}
                  </div>
                </div>
              ) : result && result.images.length > 0 ? (
                <div className="space-y-4">
                  {result.images.map((img, index) => {
                    const src = img.isBase64
                      ? `data:image/png;base64,${img.imageUrl}`
                      : img.imageUrl;

                    return (
                      <div key={index} className="relative group">
                        <div
                          className="rounded-xl overflow-hidden border border-border/50 cursor-pointer bg-muted/30"
                          onClick={() => setLightboxUrl(src)}
                        >
                          {/* eslint-disable-next-line @next/next/no-img-element */}
                          <img
                            src={src}
                            alt={img.revisedPrompt || prompt}
                            className="w-full h-auto object-contain transition-transform duration-300 group-hover:scale-[1.02]"
                            style={{ maxHeight: '70vh' }}
                          />
                        </div>

                        {/* Revised prompt display */}
                        {img.revisedPrompt && img.revisedPrompt !== prompt && (
                          <div className="mt-3 p-3 rounded-lg bg-muted/50 border border-border/50">
                            <p className="text-xs text-muted-foreground mb-1 font-medium">Prompt revisado pela IA:</p>
                            <p className="text-xs text-foreground/80 leading-relaxed">{img.revisedPrompt}</p>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              ) : (
                /* Empty state */
                <div className="flex flex-col items-center justify-center min-h-[500px] text-center p-8 space-y-6">
                  <div className="relative">
                    <div className="h-20 w-20 rounded-2xl bg-gradient-to-br from-muted to-muted/50 flex items-center justify-center">
                      <ImageIcon className="h-9 w-9 text-muted-foreground/60" />
                    </div>
                  </div>
                  <div className="space-y-2 max-w-xs">
                    <p className="font-medium text-foreground">Sua imagem aparecerá aqui</p>
                    <p className="text-sm text-muted-foreground leading-relaxed">
                      Descreva o que deseja no prompt ao lado e clique em &ldquo;Gerar Imagem&rdquo; para começar.
                    </p>
                  </div>
                  {/* Prompt suggestions */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 w-full max-w-md">
                    {[
                      'Foto profissional de retrato corporativo',
                      'Paisagem montanhosa ao pôr do sol',
                      'Design abstrato minimalista moderno',
                      'Produto em cenário lifestyle elegante',
                    ].map((suggestion) => (
                      <button
                        key={suggestion}
                        type="button"
                        onClick={() => setPrompt(suggestion)}
                        className="px-3 py-2.5 text-xs text-left text-muted-foreground rounded-lg border border-border/50 bg-muted/30 hover:bg-muted/60 hover:text-foreground hover:border-border transition-all"
                      >
                        {suggestion}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Lightbox overlay */}
      {lightboxUrl && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm"
          onClick={() => setLightboxUrl(null)}
        >
          <button
            type="button"
            className="absolute top-6 right-6 text-white/80 hover:text-white transition-colors"
            onClick={() => setLightboxUrl(null)}
          >
            <X className="h-8 w-8" />
          </button>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={lightboxUrl}
            alt="Imagem ampliada"
            className="max-w-[90vw] max-h-[90vh] object-contain rounded-xl shadow-2xl"
            onClick={e => e.stopPropagation()}
          />
        </div>
      )}
    </div>
  );
}
