'use client';

import {
    Accordion,
    AccordionContent,
    AccordionItem,
    AccordionTrigger,
} from '@/components/ui/accordion';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
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
    type ImageGenerationResponse
} from '@/services/imageGeneration';
import {
    AlertCircle,
    Download,
    Image as ImageIcon,
    Loader2,
    Sparkles,
    Upload,
    Wand2,
    X,
    ZoomIn
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

  // Prompt Builder State
  const [subject, setSubject] = useState('');
  const [setting, setSetting] = useState('');
  const [action, setAction] = useState('');
  const [expression, setExpression] = useState('');
  const [secondaryElements, setSecondaryElements] = useState('');
  const [artStyle, setArtStyle] = useState('');
  const [photoStyle, setPhotoStyle] = useState('');
  const [theme, setTheme] = useState('');
  const [cameraAngle, setCameraAngle] = useState('');
  const [focus, setFocus] = useState('');
  const [detailLevel, setDetailLevel] = useState('');
  const [editIntent, setEditIntent] = useState('');
  const [isAutoUpdating, setIsAutoUpdating] = useState(true);

  // Generation state
  const [isGenerating, setIsGenerating] = useState(false);
  const [result, setResult] = useState<ImageGenerationResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pageReady, setPageReady] = useState(false);

  // Lightbox
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  // Reference image (img2img)
  const [referenceImageBase64, setReferenceImageBase64] = useState<string | null>(null);
  const [referenceImagePreview, setReferenceImagePreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Prompt textarea auto-resize
  const promptRef = useRef<HTMLTextAreaElement>(null);

  // Derived state — filter to only image-capable models
  const imageProviders = providers
    .filter(p => p.models.some(m => m.isEnabled && m.supportsImage))
    .map(p => ({ ...p, models: p.models.filter(m => m.isEnabled && m.supportsImage) }));
  const currentProvider = imageProviders.find(p => p.key === selectedProvider);
  const currentModels = currentProvider?.models || [];
  const sizeOption = imageSizeOptions.find(s => s.value === selectedSize);

  // Load providers once on mount — auto-select first image-capable provider/model.
  // NOTE: selectedProvider is intentionally NOT in the dependency array.
  // Including it would re-run this effect on every provider change, causing
  // duplicate API calls and potential race conditions.
  useEffect(() => {
    async function loadProviders() {
      try {
        const catalog = await getAiCatalog();
        const enabledProviders = catalog.filter(p => p.isEnabled);
        setProviders(enabledProviders);

        if (enabledProviders.length > 0) {
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
            // Fallback: select first provider even if no image models found
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
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated]);

  // Auth guard
  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }
    setPageReady(true);
  }, [isAuthenticated, authLoading, router]);

  // Auto-update prompt logic
  useEffect(() => {
    if (!isAutoUpdating) return;

    const parts = [];

    if (referenceImageBase64 && editIntent) {
      parts.push(`[Intenção: ${editIntent}]`);
    }

    const elements = [];
    if (subject) elements.push(subject);
    if (action) elements.push(action);
    if (expression) elements.push(`com expressão ${expression}`);
    if (secondaryElements) elements.push(`incluindo ${secondaryElements}`);

    if (elements.length > 0) {
      parts.push(elements.join(', '));
    }

    if (setting) {
      parts.push(`Cenário: ${setting}`);
    }

    const styles = [];
    if (artStyle) styles.push(artStyle);
    if (photoStyle) styles.push(photoStyle);
    if (theme) styles.push(theme);

    if (styles.length > 0) {
      parts.push(`Estilo: ${styles.join(', ')}`);
    }

    const composition = [];
    if (cameraAngle) composition.push(`Ângulo: ${cameraAngle}`);
    if (focus) composition.push(`Foco: ${focus}`);

    if (composition.length > 0) {
      parts.push(`Composição: ${composition.join(', ')}`);
    }

    if (detailLevel) {
      parts.push(`Qualidade: ${detailLevel}`);
    }

    const generatedPrompt = parts.join('. ');
    // React bails out of re-render when state value is unchanged (Object.is comparison),
    // so calling setPrompt with the same string is safe and avoids the stale-closure
    // problem that came from comparing against a captured `prompt` ref.
    setPrompt(generatedPrompt);
  }, [
    subject, setting, action, expression, secondaryElements,
    artStyle, photoStyle, theme,
    cameraAngle, focus,
    detailLevel,
    editIntent, referenceImageBase64,
    isAutoUpdating,
  ]);

  const handleImageUpload = useCallback((file: File) => {
    const allowedTypes = ['image/png', 'image/jpeg', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      setError('Formato inválido. Use PNG, JPEG ou WEBP.');
      return;
    }
    const maxBytes = 10 * 1024 * 1024; // 10MB
    if (file.size > maxBytes) {
      setError('A imagem deve ter no máximo 10MB.');
      return;
    }
    const reader = new FileReader();
    reader.onload = (e) => {
      const dataUrl = e.target?.result as string;
      // dataUrl = "data:image/png;base64,XXXX"
      const base64 = dataUrl.split(',')[1];
      setReferenceImageBase64(base64);
      setReferenceImagePreview(dataUrl);
      setError(null);
    };
    reader.readAsDataURL(file);
  }, []);

  const handleRemoveImage = useCallback(() => {
    setReferenceImageBase64(null);
    setReferenceImagePreview(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  }, []);

  const handleDrop = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const file = e.dataTransfer.files[0];
    if (file) handleImageUpload(file);
  }, [handleImageUpload]);

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
        referenceImageBase64: referenceImageBase64 ?? undefined,
      });
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar imagem.');
    } finally {
      setIsGenerating(false);
    }
  }, [prompt, negativePrompt, selectedProvider, selectedModel, selectedSize, style, quality, referenceImageBase64]);

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

          {/* Prompt Builder */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg font-serif flex items-center gap-2">
                <Sparkles className="h-4 w-4 text-accent" />
                Construtor de Prompt
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <Accordion type="single" collapsible defaultValue="step-1" className="w-full">

                {/* Step 1: Base Image */}
                <AccordionItem value="step-1">
                  <AccordionTrigger className="text-sm font-medium">1. Imagem Base (Opcional)</AccordionTrigger>
                  <AccordionContent className="space-y-4 pt-2">
                    <div className="space-y-2">
                      <Label className="text-xs">Imagem de Referência</Label>
                      <input
                        ref={fileInputRef}
                        type="file"
                        accept="image/png,image/jpeg,image/webp"
                        className="hidden"
                        onChange={e => {
                          const file = e.target.files?.[0];
                          if (file) handleImageUpload(file);
                        }}
                      />
                      {referenceImagePreview ? (
                        <div className="relative group w-full rounded-xl border border-border overflow-hidden bg-muted/30">
                          {/* eslint-disable-next-line @next/next/no-img-element */}
                          <img
                            src={referenceImagePreview}
                            alt="Imagem de referência"
                            className="w-full h-40 object-contain"
                          />
                          <button
                            type="button"
                            onClick={handleRemoveImage}
                            className="absolute top-2 right-2 h-7 w-7 rounded-full bg-black/60 hover:bg-black/80 text-white flex items-center justify-center transition-colors"
                            title="Remover imagem"
                          >
                            <X className="h-4 w-4" />
                          </button>
                        </div>
                      ) : (
                        <div
                          role="button"
                          tabIndex={0}
                          onClick={() => fileInputRef.current?.click()}
                          onKeyDown={e => e.key === 'Enter' && fileInputRef.current?.click()}
                          onDrop={handleDrop}
                          onDragOver={e => e.preventDefault()}
                          className="flex flex-col items-center justify-center gap-2 w-full h-28 rounded-xl border-2 border-dashed border-border hover:border-accent hover:bg-accent/5 cursor-pointer transition-all text-muted-foreground hover:text-accent"
                        >
                          <Upload className="h-6 w-6" />
                          <span className="text-xs">Clique ou arraste uma imagem aqui</span>
                          <span className="text-[10px] opacity-60">PNG, JPEG, WEBP — máx. 10MB</span>
                        </div>
                      )}
                    </div>

                    {referenceImageBase64 && (
                      <div className="space-y-2">
                        <Label className="text-xs">Intenção de Edição</Label>
                        <Select value={editIntent} onValueChange={setEditIntent}>
                          <SelectTrigger className="h-9 text-xs">
                            <SelectValue placeholder="O que deseja fazer com a imagem?" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Manter a estrutura e alterar o estilo">Alterar Estilo</SelectItem>
                            <SelectItem value="Alterar o fundo da imagem">Alterar Fundo</SelectItem>
                            <SelectItem value="Criar uma variação semelhante">Criar Variação</SelectItem>
                            <SelectItem value="Transformar em personagem/avatar">Transformar em Avatar</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    )}
                  </AccordionContent>
                </AccordionItem>

                {/* Step 2: Elements */}
                <AccordionItem value="step-2">
                  <AccordionTrigger className="text-sm font-medium">2. Elementos da Imagem</AccordionTrigger>
                  <AccordionContent className="space-y-3 pt-2">
                    <div className="space-y-1.5">
                      <Label className="text-xs">Sujeito Principal</Label>
                      <Input
                        placeholder="Ex: Um cachorro golden retriever, Uma mulher jovem..."
                        value={subject} onChange={e => setSubject(e.target.value)}
                        className="h-9 text-xs"
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label className="text-xs">Ação / Atividade</Label>
                      <Input
                        placeholder="Ex: correndo no parque, tomando café..."
                        value={action} onChange={e => setAction(e.target.value)}
                        className="h-9 text-xs"
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label className="text-xs">Cenário / Fundo</Label>
                      <Input
                        placeholder="Ex: em uma floresta mágica, estúdio com fundo branco..."
                        value={setting} onChange={e => setSetting(e.target.value)}
                        className="h-9 text-xs"
                      />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1.5">
                        <Label className="text-xs">Expressão / Pose</Label>
                        <Input
                          placeholder="Ex: sorrindo, de perfil..."
                          value={expression} onChange={e => setExpression(e.target.value)}
                          className="h-9 text-xs"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <Label className="text-xs">Elementos Secundários</Label>
                        <Input
                          placeholder="Ex: usando óculos, pássaros voando..."
                          value={secondaryElements} onChange={e => setSecondaryElements(e.target.value)}
                          className="h-9 text-xs"
                        />
                      </div>
                    </div>
                  </AccordionContent>
                </AccordionItem>

                {/* Step 3: Style */}
                <AccordionItem value="step-3">
                  <AccordionTrigger className="text-sm font-medium">3. Estilo e Visual</AccordionTrigger>
                  <AccordionContent className="space-y-3 pt-2">
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1.5">
                        <Label className="text-xs">Estilo Artístico</Label>
                        <Select value={artStyle} onValueChange={setArtStyle}>
                          <SelectTrigger className="h-9 text-xs"><SelectValue placeholder="Nenhum" /></SelectTrigger>
                          <SelectContent>
                            <SelectItem value="">Nenhum</SelectItem>
                            <SelectItem value="Pintura a óleo">Pintura a óleo</SelectItem>
                            <SelectItem value="Aquarela">Aquarela</SelectItem>
                            <SelectItem value="Pixel Art">Pixel Art</SelectItem>
                            <SelectItem value="Ilustração 2D">Ilustração 2D</SelectItem>
                            <SelectItem value="Anime / Mangá">Anime / Mangá</SelectItem>
                            <SelectItem value="3D Render (Unreal Engine)">3D Render</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-1.5">
                        <Label className="text-xs">Estilo Fotográfico</Label>
                        <Select value={photoStyle} onValueChange={setPhotoStyle}>
                          <SelectTrigger className="h-9 text-xs"><SelectValue placeholder="Nenhum" /></SelectTrigger>
                          <SelectContent>
                            <SelectItem value="">Nenhum</SelectItem>
                            <SelectItem value="Retrato de estúdio">Retrato de estúdio</SelectItem>
                            <SelectItem value="Foto Polaroid">Foto Polaroid</SelectItem>
                            <SelectItem value="Macro (Close-up extremo)">Macro</SelectItem>
                            <SelectItem value="Fotografia de rua">Fotografia de rua</SelectItem>
                            <SelectItem value="Cinematic (Estilo cinema)">Cinematic</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                    <div className="space-y-1.5">
                      <Label className="text-xs">Temática Visual</Label>
                      <Select value={theme} onValueChange={setTheme}>
                        <SelectTrigger className="h-9 text-xs"><SelectValue placeholder="Nenhuma" /></SelectTrigger>
                        <SelectContent>
                          <SelectItem value="">Nenhuma</SelectItem>
                          <SelectItem value="Cyberpunk">Cyberpunk</SelectItem>
                          <SelectItem value="Steampunk">Steampunk</SelectItem>
                          <SelectItem value="Retrô Anos 90">Retrô Anos 90</SelectItem>
                          <SelectItem value="Sci-Fi Futurista">Sci-Fi Futurista</SelectItem>
                          <SelectItem value="Fantasia Épica">Fantasia Épica</SelectItem>
                          <SelectItem value="Minimalista">Minimalista</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </AccordionContent>
                </AccordionItem>

                {/* Step 4: Composition */}
                <AccordionItem value="step-4">
                  <AccordionTrigger className="text-sm font-medium">4. Composição e Qualidade</AccordionTrigger>
                  <AccordionContent className="space-y-3 pt-2">
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1.5">
                        <Label className="text-xs">Ângulo da Câmera</Label>
                        <Select value={cameraAngle} onValueChange={setCameraAngle}>
                          <SelectTrigger className="h-9 text-xs"><SelectValue placeholder="Padrão" /></SelectTrigger>
                          <SelectContent>
                            <SelectItem value="">Padrão</SelectItem>
                            <SelectItem value="Visão frontal">Frontal</SelectItem>
                            <SelectItem value="Visão de cima (Top-down)">De cima</SelectItem>
                            <SelectItem value="Visão de baixo (Low angle)">De baixo</SelectItem>
                            <SelectItem value="Plano geral (Wide shot)">Plano geral</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-1.5">
                        <Label className="text-xs">Foco e Profundidade</Label>
                        <Select value={focus} onValueChange={setFocus}>
                          <SelectTrigger className="h-9 text-xs"><SelectValue placeholder="Padrão" /></SelectTrigger>
                          <SelectContent>
                            <SelectItem value="">Padrão</SelectItem>
                            <SelectItem value="Fundo desfocado (Bokeh)">Fundo desfocado</SelectItem>
                            <SelectItem value="Foco nítido em toda a cena">Foco total</SelectItem>
                            <SelectItem value="Foco no rosto">Foco no rosto</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1.5">
                        <Label className="text-xs">Nível de Detalhe</Label>
                        <Select value={detailLevel} onValueChange={setDetailLevel}>
                          <SelectTrigger className="h-9 text-xs"><SelectValue placeholder="Padrão" /></SelectTrigger>
                          <SelectContent>
                            <SelectItem value="">Padrão</SelectItem>
                            <SelectItem value="Fotorrealista, alta definição">Fotorrealista</SelectItem>
                            <SelectItem value="Altamente detalhado, 8k">Altamente detalhado</SelectItem>
                            <SelectItem value="Traços simples, rascunho">Traços simples</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-1.5">
                        <Label className="text-xs">Dimensão (Proporção)</Label>
                        <Select value={selectedSize} onValueChange={setSelectedSize}>
                          <SelectTrigger className="h-9 text-xs"><SelectValue /></SelectTrigger>
                          <SelectContent>
                            {imageSizeOptions.map(s => (
                              <SelectItem key={s.value} value={s.value}>{s.label}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                  </AccordionContent>
                </AccordionItem>
              </Accordion>
            </CardContent>
          </Card>

          {/* Final Prompt Area */}
          <Card className="border-accent/20 bg-accent/5">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-sm font-serif flex items-center gap-2">
                  Prompt Final
                </CardTitle>
                <div className="flex items-center gap-2">
                  <Label htmlFor="auto-update" className="text-[10px] text-muted-foreground cursor-pointer">Auto-atualizar</Label>
                  <input
                    id="auto-update"
                    type="checkbox"
                    checked={isAutoUpdating}
                    onChange={(e) => setIsAutoUpdating(e.target.checked)}
                    className="rounded border-input"
                  />
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <textarea
                ref={promptRef}
                value={prompt}
                onChange={e => {
                  setPrompt(e.target.value);
                  if (isAutoUpdating) setIsAutoUpdating(false); // Disable auto-update if user manually edits
                }}
                onKeyDown={handleKeyDown}
                placeholder="O prompt gerado aparecerá aqui. Você pode editá-livremente..."
                className="w-full min-h-[120px] p-3 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-accent resize-y leading-relaxed"
              />

              <div className="space-y-2">
                <Label className="text-xs text-muted-foreground">Prompt Negativo (Opcional)</Label>
                <Input
                  value={negativePrompt}
                  onChange={e => setNegativePrompt(e.target.value)}
                  placeholder="O que evitar: baixa qualidade, desfocado, texto..."
                  className="h-9 text-xs bg-background"
                />
              </div>

              {/* Generate Button */}
              <Button
                className="w-full py-6 text-base font-medium mt-2"
                onClick={handleGenerate}
                disabled={isGenerating || !prompt.trim()}
              >
                {isGenerating ? (
                  <>
                    <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                    {referenceImageBase64 ? 'Processando imagem...' : 'Gerando imagem...'}
                  </>
                ) : referenceImageBase64 ? (
                  <>
                    <Wand2 className="mr-2 h-5 w-5" />
                    Processar Imagem
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
                      setLightboxUrl(result.images[0].imageUrl);
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
                      href={result.images[0].imageUrl}
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
                    // imageUrl from backend is always a complete URL or full data-URL
                    // (e.g. "https://..." for OpenAI/fal.ai, "data:image/png;base64,..." for Gemini/Imagen)
                    const src = img.imageUrl;

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
                        onClick={() => {
                          setPrompt(suggestion);
                          setIsAutoUpdating(false); // Prevent builder fields from overwriting suggestion
                        }}
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
