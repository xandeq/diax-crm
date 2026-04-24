'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/contexts/AuthContext';
import { useAiCatalog } from '@/hooks/useAiCatalog';
import { ProviderBadge } from '@/components/ai/ProviderBadge';
import { ModelCard } from '@/components/ai/ModelCard';
import { QuotaStatusCard, type QuotaStatusDto } from '@/components/QuotaStatusCard';
import { isModelFree, requiresLicenseAcceptance } from '@/lib/aiModelClassification';
import { type AiModel } from '@/services/aiCatalog';
import { apiFetch, ApiError } from '@/services/api';
import {
  generateImage,
  generateVideo,
  imageSizeOptions,
  videoAspectRatioOptions,
  videoDurationOptions,
  type ImageGenerationResponse,
  type VideoGenerationResponse,
} from '@/services/imageGeneration';
import {
  AlertCircle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Clock,
  Download,
  Film,
  Image as ImageIcon,
  Info,
  Loader2,
  RefreshCw,
  Sparkles,
  Upload,
  Wand2,
  X,
  ZoomIn,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useRef, useState } from 'react';

// ─── Constants ─────────────────────────────────────────────────────────────────

const IMAGE_MESSAGES = [
  '✨ Pedindo para a IA criar sua obra...',
  '🎨 A IA está misturando pixels e criatividade...',
  '🖌️ Pintando cada detalhe com precisão...',
  '🔮 Materializando sua visão em pixels...',
  '⚡ Processando com inteligência artificial...',
  '🌟 Quase lá! Refinando os detalhes finais...',
];

const VIDEO_MESSAGES = [
  '🎬 Iniciando a geração do vídeo...',
  '🎞️ A IA está calculando cada frame...',
  '⚙️ Processando movimentos e transições...',
  '🌊 Animando sua visão quadro a quadro...',
  '🎥 Montando a sequência final...',
  '⏳ Vídeos levam de 30s a 5min — pode aguardar!',
  '🚀 Quase lá! Finalizando seu vídeo...',
];

const IMAGE_SUGGESTIONS = [
  'Foto corporativa profissional, fundo desfocado, iluminação de estúdio',
  'Paisagem montanhosa ao pôr do sol, céu laranja, estilo fotorrealista 8k',
  'Produto cosmético minimalista em mesa branca, estilo editorial',
  'Personagem anime guerreira, cabelo azul, cenário fantasia épica',
  'Cidade futurista cyberpunk à noite, neons roxos e azuis',
  'Logo estilo flat design, gradiente moderno, fundo escuro',
];

const VIDEO_SUGGESTIONS = [
  'Pessoa caminhando em slow motion em parque florido ao amanhecer',
  'Drone voando sobre cidade ao pôr do sol, movimento suave',
  'Produto girando 360° em fundo branco limpo, estilo comercial',
  'Oceano com ondas quebrando na praia, câmera ao nível da água',
];

// ─── Helpers ───────────────────────────────────────────────────────────────────

function formatSeconds(s: number): string {
  if (s < 60) return `${s}s`;
  return `${Math.floor(s / 60)}m ${s % 60}s`;
}

interface ErrorInfo {
  title: string;
  detail: string;
  isTransient: boolean;
}

function getErrorInfo(code: string | undefined, rawMessage: string): ErrorInfo {
  switch (code) {
    case 'QuotaExhausted':
      return { title: 'Créditos esgotados', detail: 'O saldo deste provider acabou. Recarregue os créditos para continuar.', isTransient: false };
    case 'RateLimit':
      return { title: 'Limite de requisições atingido', detail: 'Muitas chamadas em pouco tempo. Aguarde alguns minutos e tente novamente.', isTransient: true };
    case 'AuthFailed':
      return { title: 'Chave de API inválida', detail: 'A credencial do provider está incorreta ou expirada. Verifique a configuração.', isTransient: false };
    case 'ModelNotFound':
      return { title: 'Modelo indisponível', detail: 'Este modelo foi descontinuado ou não está acessível no provider.', isTransient: false };
    case 'InvalidRequest':
      return { title: 'Prompt ou parâmetros inválidos', detail: rawMessage, isTransient: false };
    case 'ProviderUnavailable':
      return { title: 'Provider fora do ar', detail: 'O serviço está temporariamente indisponível. Tente novamente em breve.', isTransient: true };
    case 'Timeout':
      return { title: 'Tempo esgotado', detail: 'O provider demorou demais para responder. Pode ser uma cold start — tente novamente em alguns segundos.', isTransient: true };
    case 'ConfigurationMissing':
      return { title: 'API key não configurada', detail: 'Configure as credenciais deste provider no painel de administração.', isTransient: false };
    case 'CapabilityMismatch':
      return { title: 'Função não suportada', detail: 'Este modelo não suporta este tipo de geração. Escolha outro modelo.', isTransient: false };
    default:
      return { title: 'Algo deu errado', detail: rawMessage || 'Erro inesperado. Tente novamente.', isTransient: false };
  }
}

// ─── Main Component ─────────────────────────────────────────────────────────────

export default function ImageGenerationPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  // Mode
  const [activeTab, setActiveTab] = useState<'image' | 'video'>('image');

  // Catalogs
  const { providers: imageProviders, selectedProvider: imageProvider, selectedModel: imageModel, setSelectedProvider: setImageProvider, setSelectedModel: setImageModel, isReady: imageReady } = useAiCatalog({ filterCapability: 'supportsImage' });
  const { providers: videoProviders, selectedProvider: videoProvider, selectedModel: videoModel, setSelectedProvider: setVideoProvider, setSelectedModel: setVideoModel, isReady: videoReady } = useAiCatalog({ filterCapability: 'supportsVideo' });

  // Selection
  const selectedProvider = activeTab === 'image' ? imageProvider : videoProvider;
  const selectedModel = activeTab === 'image' ? imageModel : videoModel;
  const setSelectedProvider = activeTab === 'image' ? setImageProvider : setVideoProvider;
  const setSelectedModel = activeTab === 'image' ? setImageModel : setVideoModel;
  const activeProviders = activeTab === 'image' ? imageProviders : videoProviders;
  const isReady = activeTab === 'image' ? imageReady : videoReady;
  const [showAllModels, setShowAllModels] = useState(false);

  // Prompt
  const [prompt, setPrompt] = useState('');
  const [negativePrompt, setNegativePrompt] = useState('');
  const [showAdvanced, setShowAdvanced] = useState(false);

  // Image params
  const [selectedSize, setSelectedSize] = useState('1024x1024');
  const [style, setStyle] = useState('vivid');
  const [quality, setQuality] = useState('standard');

  // Video params
  const [aspectRatio, setAspectRatio] = useState('16:9');
  const [duration, setDuration] = useState(5);

  // Reference image
  const [referenceImageBase64, setReferenceImageBase64] = useState<string | null>(null);
  const [referenceImagePreview, setReferenceImagePreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Generation state
  const [isGenerating, setIsGenerating] = useState(false);
  const [imageResult, setImageResult] = useState<ImageGenerationResponse | null>(null);
  const [videoResult, setVideoResult] = useState<VideoGenerationResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [errorCode, setErrorCode] = useState<string | undefined>(undefined);
  const clearError = () => { setError(null); setErrorCode(undefined); };
  const [loadingMsgIdx, setLoadingMsgIdx] = useState(0);
  const [elapsedSecs, setElapsedSecs] = useState(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const msgTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // Lightbox
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  // Drag state
  const [isDragging, setIsDragging] = useState(false);

  // Video quota
  const [quotaStatus, setQuotaStatus] = useState<QuotaStatusDto | null>(null);
  const [isLoadingQuota, setIsLoadingQuota] = useState(false);

  // ── Derived ──────────────────────────────────────────────────────────────────

  const currentProvider = activeProviders.find(p => p.key === selectedProvider);

  const currentModels = (currentProvider?.models ?? []).filter(m => m.isEnabled);

  // Free models first, then alphabetical
  const sortedModels = [...currentModels].sort((a, b) => {
    const aFree = isModelFree(selectedProvider, a.modelKey);
    const bFree = isModelFree(selectedProvider, b.modelKey);
    if (aFree && !bFree) return -1;
    if (!aFree && bFree) return 1;
    const aRequiresLicense = requiresLicenseAcceptance(selectedProvider, a.modelKey);
    const bRequiresLicense = requiresLicenseAcceptance(selectedProvider, b.modelKey);
    if (!aRequiresLicense && bRequiresLicense) return -1;
    if (aRequiresLicense && !bRequiresLicense) return 1;
    return a.displayName.localeCompare(b.displayName);
  });

  const displayedModels = showAllModels ? sortedModels : sortedModels.slice(0, 6);

  const currentLoadingMessages = activeTab === 'image' ? IMAGE_MESSAGES : VIDEO_MESSAGES;

  // ── Effects ───────────────────────────────────────────────────────────────────

  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) { router.push('/login'); return; }
  }, [isAuthenticated, authLoading, router]);

  // Reset show all models when tab changes
  useEffect(() => {
    setShowAllModels(false);
  }, [activeTab]);

  // Loading timer
  useEffect(() => {
    if (isGenerating) {
      setElapsedSecs(0);
      setLoadingMsgIdx(0);
      timerRef.current = setInterval(() => setElapsedSecs(s => s + 1), 1000);
      msgTimerRef.current = setInterval(() =>
        setLoadingMsgIdx(i => (i + 1) % currentLoadingMessages.length),
        4000
      );
    } else {
      if (timerRef.current) clearInterval(timerRef.current);
      if (msgTimerRef.current) clearInterval(msgTimerRef.current);
    }
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
      if (msgTimerRef.current) clearInterval(msgTimerRef.current);
    };
  }, [isGenerating, currentLoadingMessages.length]);

  // Fetch quota status when video provider changes
  useEffect(() => {
    if (activeTab !== 'video' || !selectedProvider) {
      setQuotaStatus(null);
      return;
    }

    const fetchQuota = async () => {
      setIsLoadingQuota(true);
      try {
        // Get provider ID from the providers list
        const provider = videoProviders.find(p => p.key === selectedProvider);
        if (!provider) {
          setQuotaStatus(null);
          return;
        }

        // Fetch quota status from API
        const quota = await apiFetch<QuotaStatusDto>(`/admin/video-providers/${provider.id}/quota`, {
          method: 'GET',
        });
        setQuotaStatus(quota);
      } catch (err) {
        console.error('Failed to fetch quota status:', err);
        setQuotaStatus(null);
      } finally {
        setIsLoadingQuota(false);
      }
    };

    fetchQuota();
  }, [activeTab, selectedProvider, videoProviders]);

  // ── Handlers ─────────────────────────────────────────────────────────────────

  const handleSelectProvider = useCallback((key: string) => {
    setSelectedProvider(key);
    setShowAllModels(false);
  }, [setSelectedProvider]);

  const handleImageUpload = useCallback((file: File) => {
    if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type)) {
      setError('Formato inválido. Use PNG, JPEG ou WEBP.');
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      setError('A imagem deve ter no máximo 10MB.');
      return;
    }
    const reader = new FileReader();
    reader.onload = e => {
      const dataUrl = e.target?.result as string;
      setReferenceImageBase64(dataUrl.split(',')[1]);
      setReferenceImagePreview(dataUrl);
      clearError();
    };
    reader.readAsDataURL(file);
  }, []);

  const handleRemoveReference = useCallback(() => {
    setReferenceImageBase64(null);
    setReferenceImagePreview(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleImageUpload(file);
  }, [handleImageUpload]);

  const handleGenerate = useCallback(async () => {
    if (!prompt.trim()) {
      setError('Descreva o que deseja gerar no campo de prompt.');
      return;
    }
    if (!selectedProvider || !selectedModel) {
      setError('Selecione um provedor e modelo antes de gerar.');
      return;
    }

    setIsGenerating(true);
    clearError();
    setImageResult(null);
    setVideoResult(null);

    try {
      if (activeTab === 'image') {
        const sizeOpt = imageSizeOptions.find(s => s.value === selectedSize);
        const response = await generateImage({
          provider: selectedProvider,
          model: selectedModel,
          prompt: prompt.trim(),
          negativePrompt: negativePrompt.trim() || undefined,
          width: sizeOpt?.width ?? 1024,
          height: sizeOpt?.height ?? 1024,
          numberOfImages: 1,
          style,
          quality,
          referenceImageBase64: referenceImageBase64 ?? undefined,
        });
        setImageResult(response);
      } else {
        const response = await generateVideo({
          provider: selectedProvider,
          model: selectedModel,
          prompt: prompt.trim(),
          negativePrompt: negativePrompt.trim() || undefined,
          durationSeconds: duration,
          aspectRatio,
          referenceImageBase64: referenceImageBase64 ?? undefined,
        });
        setVideoResult(response);
        // Update quota status if returned from API
        if (response.quotaStatus) {
          setQuotaStatus(response.quotaStatus);
        }
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro inesperado ao gerar.';
      const code = err instanceof ApiError ? err.code : undefined;
      setError(msg);
      setErrorCode(code);
    } finally {
      setIsGenerating(false);
    }
  }, [
    prompt, negativePrompt, selectedProvider, selectedModel,
    activeTab, selectedSize, style, quality,
    duration, aspectRatio, referenceImageBase64,
  ]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter' && !isGenerating && prompt.trim()) {
      e.preventDefault();
      handleGenerate();
    }
  }, [handleGenerate, isGenerating, prompt]);

  // ── Loading state ────────────────────────────────────────────────────────────

  if (!isReady) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
        <div className="h-12 w-12 rounded-2xl bg-violet-500/20 flex items-center justify-center">
          <Loader2 className="h-6 w-6 text-violet-400 animate-spin" />
        </div>
        <p className="text-sm text-white/50">Carregando provedores de IA...</p>
      </div>
    );
  }

  // ── Render ───────────────────────────────────────────────────────────────────

  const hasResult = activeTab === 'image' ? !!imageResult : !!videoResult;
  const imageSrc = imageResult?.images?.[0]?.imageUrl;
  const revisedPrompt = imageResult?.images?.[0]?.revisedPrompt;
  const generationMeta = activeTab === 'image'
    ? imageResult
      ? { provider: imageResult.providerUsed, model: imageResult.modelUsed, ms: imageResult.durationMs }
      : null
    : videoResult
      ? { provider: videoResult.providerUsed, model: videoResult.modelUsed, ms: videoResult.durationMs }
      : null;

  return (
    <div className="min-h-screen bg-[#0c0c0f] text-white">
      {/* ── Page Header ── */}
      <div className="border-b border-white/8 bg-[#0f0f13]/80 backdrop-blur-sm sticky top-0 z-10">
        <div className="max-w-screen-2xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-violet-500 to-indigo-600 flex items-center justify-center shadow-lg shadow-violet-500/20">
              <Sparkles className="h-4 w-4 text-white" />
            </div>
            <div>
              <h1 className="text-base font-semibold text-white">Gerador com IA</h1>
              <p className="text-[11px] text-white/40">Imagens &amp; Vídeos — múltiplos providers</p>
            </div>
          </div>

          {/* Tab switcher */}
          <div className="flex items-center gap-1 bg-white/5 rounded-xl p-1 border border-white/10">
            <button
              type="button"
              onClick={() => { setActiveTab('image'); clearError(); }}
              className={`flex items-center gap-1.5 px-4 py-1.5 rounded-lg text-sm font-medium transition-all duration-200 ${
                activeTab === 'image'
                  ? 'bg-violet-600 text-white shadow-lg shadow-violet-500/20'
                  : 'text-white/50 hover:text-white/80'
              }`}
            >
              <ImageIcon className="h-3.5 w-3.5" />
              Imagens
            </button>
            <button
              type="button"
              onClick={() => { setActiveTab('video'); clearError(); }}
              className={`flex items-center gap-1.5 px-4 py-1.5 rounded-lg text-sm font-medium transition-all duration-200 ${
                activeTab === 'video'
                  ? 'bg-violet-600 text-white shadow-lg shadow-violet-500/20'
                  : 'text-white/50 hover:text-white/80'
              }`}
            >
              <Film className="h-3.5 w-3.5" />
              Vídeos
            </button>
          </div>
        </div>
      </div>

      {/* ── Main Layout ── */}
      <div className="max-w-screen-2xl mx-auto px-4 py-6 flex flex-col lg:flex-row gap-5 items-start">

        {/* ─── LEFT PANEL ────────────────────────────────────────────────────────── */}
        <div className="w-full lg:w-[380px] xl:w-[420px] shrink-0 space-y-4">

          {/* Provider Selection */}
          <div className="rounded-2xl bg-white/[0.04] border border-white/10 p-5">
            <h3 className="text-xs font-semibold text-white/50 uppercase tracking-wider mb-3">
              Provedor de IA
            </h3>
            {activeProviders.length === 0 ? (
              <div className="flex items-center gap-2 text-xs text-amber-400 bg-amber-500/10 border border-amber-500/20 rounded-lg px-3 py-2.5">
                <Info className="h-3.5 w-3.5 shrink-0" />
                Nenhum provedor com modelos de {activeTab === 'image' ? 'imagem' : 'vídeo'} habilitado.
                Configure em Admin → IA.
              </div>
            ) : (
              <div className="flex flex-wrap gap-2">
                {activeProviders.map(p => (
                  <ProviderBadge
                    key={p.key}
                    provider={p}
                    selected={selectedProvider === p.key}
                    onClick={() => handleSelectProvider(p.key)}
                  />
                ))}
              </div>
            )}
          </div>

          {/* Model Selection */}
          {currentProvider && sortedModels.length > 0 && (
            <div className="rounded-2xl bg-white/[0.04] border border-white/10 p-5">
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-xs font-semibold text-white/50 uppercase tracking-wider">
                  Modelo
                </h3>
                <span className="text-[10px] text-white/30">
                  {sortedModels.filter(m => isModelFree(selectedProvider, m.modelKey)).length} grátis
                </span>
              </div>
              <div className="space-y-1.5">
                {displayedModels.map(m => (
                  <ModelCard
                    key={m.modelKey}
                    model={m}
                    selected={selectedModel === m.modelKey}
                    providerKey={selectedProvider}
                    onClick={() => setSelectedModel(m.modelKey)}
                  />
                ))}
              </div>
              {sortedModels.length > 6 && (
                <button
                  type="button"
                  onClick={() => setShowAllModels(v => !v)}
                  className="mt-2.5 w-full flex items-center justify-center gap-1.5 text-[11px] text-white/40 hover:text-white/70 transition-colors py-1"
                >
                  {showAllModels ? (
                    <><ChevronDown className="h-3 w-3" /> Mostrar menos</>
                  ) : (
                    <><ChevronRight className="h-3 w-3" /> Ver todos os {sortedModels.length} modelos</>
                  )}
                </button>
              )}
            </div>
          )}

          {/* Quota Status (Video only) */}
          {activeTab === 'video' && selectedProvider && (
            <QuotaStatusCard quota={quotaStatus} isLoading={isLoadingQuota} />
          )}

          {/* Prompt */}
          <div className="rounded-2xl bg-white/[0.04] border border-white/10 p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-xs font-semibold text-white/50 uppercase tracking-wider">
                {activeTab === 'image' ? 'Descrição da Imagem' : 'Descrição do Vídeo'}
              </h3>
              <span className="text-[10px] text-white/30">Ctrl+Enter para gerar</span>
            </div>
            <textarea
              value={prompt}
              onChange={e => setPrompt(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={
                activeTab === 'image'
                  ? 'Descreva a imagem que deseja criar em detalhes...\nEx: "Retrato profissional de mulher executiva, fundo desfocado, iluminação natural"'
                  : 'Descreva o vídeo que deseja criar...\nEx: "Câmera lenta, flores abrindo ao amanhecer, luz dourada"'
              }
              rows={4}
              className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-3 text-sm text-white placeholder:text-white/25 focus:outline-none focus:ring-1 focus:ring-violet-500/50 focus:border-violet-500/40 resize-none leading-relaxed transition-all"
            />

            {/* Prompt suggestions */}
            <div className="mt-2.5 flex flex-wrap gap-1.5">
              {(activeTab === 'image' ? IMAGE_SUGGESTIONS : VIDEO_SUGGESTIONS).slice(0, 3).map(s => (
                <button
                  key={s}
                  type="button"
                  onClick={() => setPrompt(s)}
                  className="text-[10px] text-white/35 hover:text-white/65 bg-white/5 hover:bg-white/10 border border-white/8 hover:border-white/15 rounded-lg px-2.5 py-1 transition-all text-left line-clamp-1 max-w-[130px]"
                  title={s}
                >
                  {s.split(',')[0]}...
                </button>
              ))}
            </div>
          </div>

          {/* Reference Image */}
          <div className="rounded-2xl bg-white/[0.04] border border-white/10 p-5">
            <h3 className="text-xs font-semibold text-white/50 uppercase tracking-wider mb-3">
              {activeTab === 'image' ? 'Imagem de Referência' : 'Imagem para Animar'}
              <span className="ml-1.5 text-[10px] font-normal text-white/25 normal-case">Opcional</span>
            </h3>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/png,image/jpeg,image/webp"
              className="hidden"
              onChange={e => { const f = e.target.files?.[0]; if (f) handleImageUpload(f); }}
            />
            {referenceImagePreview ? (
              <div className="relative group rounded-xl overflow-hidden border border-white/10 bg-white/5">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={referenceImagePreview}
                  alt="Referência"
                  className="w-full h-32 object-contain"
                />
                <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                  <button
                    type="button"
                    onClick={handleRemoveReference}
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-red-500/80 hover:bg-red-500 text-white text-xs font-medium transition-colors"
                  >
                    <X className="h-3.5 w-3.5" /> Remover
                  </button>
                </div>
              </div>
            ) : (
              <div
                role="button"
                tabIndex={0}
                onClick={() => fileInputRef.current?.click()}
                onKeyDown={e => e.key === 'Enter' && fileInputRef.current?.click()}
                onDrop={handleDrop}
                onDragOver={e => { e.preventDefault(); setIsDragging(true); }}
                onDragLeave={() => setIsDragging(false)}
                className={`flex flex-col items-center justify-center gap-2 h-24 rounded-xl border-2 border-dashed cursor-pointer transition-all ${
                  isDragging
                    ? 'border-violet-500/60 bg-violet-500/10 text-violet-400'
                    : 'border-white/15 hover:border-violet-500/40 hover:bg-violet-500/5 text-white/30 hover:text-white/60'
                }`}
              >
                <Upload className="h-5 w-5" />
                <span className="text-xs">Arraste ou clique para enviar</span>
                <span className="text-[10px] opacity-60">PNG, JPEG, WEBP — máx 10MB</span>
              </div>
            )}
            {activeTab === 'image' && referenceImageBase64 && (
              <p className="mt-2 text-[10px] text-white/35 leading-relaxed">
                💡 Alguns modelos usam esta imagem como base para edição (img2img).
                Modelos que não suportam ignorarão a referência.
              </p>
            )}
            {activeTab === 'video' && referenceImageBase64 && (
              <p className="mt-2 text-[10px] text-emerald-400/70 leading-relaxed">
                ✅ Imagem carregada! Ela será animada pelo modelo de vídeo.
              </p>
            )}
          </div>

          {/* Advanced Options */}
          <div className="rounded-2xl bg-white/[0.04] border border-white/10 overflow-hidden">
            <button
              type="button"
              onClick={() => setShowAdvanced(v => !v)}
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-white/5 transition-colors"
            >
              <h3 className="text-xs font-semibold text-white/50 uppercase tracking-wider">
                Opções Avançadas
              </h3>
              <ChevronDown className={`h-4 w-4 text-white/30 transition-transform ${showAdvanced ? 'rotate-180' : ''}`} />
            </button>

            {showAdvanced && (
              <div className="px-5 pb-5 space-y-4 border-t border-white/8">
                <div className="pt-4" />

                {activeTab === 'image' && (
                  <>
                    <div className="space-y-2">
                      <label className="text-[11px] text-white/40 font-medium">Dimensão</label>
                      <div className="grid grid-cols-2 gap-1.5">
                        {imageSizeOptions.map(s => (
                          <button
                            key={s.value}
                            type="button"
                            onClick={() => setSelectedSize(s.value)}
                            className={`px-3 py-2 rounded-lg text-xs border transition-all ${
                              selectedSize === s.value
                                ? 'bg-violet-500/20 border-violet-500/40 text-violet-300'
                                : 'bg-white/5 border-white/10 text-white/50 hover:bg-white/10 hover:text-white/80'
                            }`}
                          >
                            {s.label}
                          </button>
                        ))}
                      </div>
                    </div>

                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-2">
                        <label className="text-[11px] text-white/40 font-medium">Estilo</label>
                        <div className="flex gap-1.5">
                          {['vivid', 'natural'].map(v => (
                            <button
                              key={v}
                              type="button"
                              onClick={() => setStyle(v)}
                              className={`flex-1 py-2 rounded-lg text-xs border transition-all capitalize ${
                                style === v
                                  ? 'bg-violet-500/20 border-violet-500/40 text-violet-300'
                                  : 'bg-white/5 border-white/10 text-white/50 hover:bg-white/10'
                              }`}
                            >
                              {v === 'vivid' ? 'Vívido' : 'Natural'}
                            </button>
                          ))}
                        </div>
                      </div>
                      <div className="space-y-2">
                        <label className="text-[11px] text-white/40 font-medium">Qualidade</label>
                        <div className="flex gap-1.5">
                          {['standard', 'hd'].map(v => (
                            <button
                              key={v}
                              type="button"
                              onClick={() => setQuality(v)}
                              className={`flex-1 py-2 rounded-lg text-xs border transition-all uppercase ${
                                quality === v
                                  ? 'bg-violet-500/20 border-violet-500/40 text-violet-300'
                                  : 'bg-white/5 border-white/10 text-white/50 hover:bg-white/10'
                              }`}
                            >
                              {v === 'standard' ? 'Padrão' : 'HD'}
                            </button>
                          ))}
                        </div>
                      </div>
                    </div>
                  </>
                )}

                {activeTab === 'video' && (
                  <>
                    <div className="space-y-2">
                      <label className="text-[11px] text-white/40 font-medium">Proporção</label>
                      <div className="grid grid-cols-3 gap-1.5">
                        {videoAspectRatioOptions.map(opt => (
                          <button
                            key={opt.value}
                            type="button"
                            onClick={() => setAspectRatio(opt.value)}
                            className={`py-2 rounded-lg text-xs border transition-all ${
                              aspectRatio === opt.value
                                ? 'bg-violet-500/20 border-violet-500/40 text-violet-300'
                                : 'bg-white/5 border-white/10 text-white/50 hover:bg-white/10'
                            }`}
                          >
                            {opt.value}
                          </button>
                        ))}
                      </div>
                    </div>

                    <div className="space-y-2">
                      <label className="text-[11px] text-white/40 font-medium">Duração</label>
                      <div className="grid grid-cols-4 gap-1.5">
                        {videoDurationOptions.map(opt => (
                          <button
                            key={opt.value}
                            type="button"
                            onClick={() => setDuration(opt.value)}
                            className={`py-2 rounded-lg text-xs border transition-all ${
                              duration === opt.value
                                ? 'bg-violet-500/20 border-violet-500/40 text-violet-300'
                                : 'bg-white/5 border-white/10 text-white/50 hover:bg-white/10'
                            }`}
                          >
                            {opt.label}
                          </button>
                        ))}
                      </div>
                    </div>
                  </>
                )}

                <div className="space-y-2">
                  <label className="text-[11px] text-white/40 font-medium">Prompt Negativo</label>
                  <input
                    type="text"
                    value={negativePrompt}
                    onChange={e => setNegativePrompt(e.target.value)}
                    placeholder="O que evitar: baixa qualidade, desfocado, texto..."
                    className="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-2.5 text-xs text-white placeholder:text-white/25 focus:outline-none focus:ring-1 focus:ring-violet-500/50 transition-all"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Generate Button */}
          <button
            type="button"
            onClick={handleGenerate}
            disabled={isGenerating || !prompt.trim() || !selectedModel}
            className={`w-full flex items-center justify-center gap-2.5 py-4 rounded-2xl text-sm font-semibold transition-all duration-200 ${
              isGenerating || !prompt.trim() || !selectedModel
                ? 'bg-white/5 text-white/25 cursor-not-allowed'
                : 'bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-500 hover:to-indigo-500 text-white shadow-lg shadow-violet-500/25 hover:shadow-violet-500/40 active:scale-[0.98]'
            }`}
          >
            {isGenerating ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                {activeTab === 'image' ? 'Gerando imagem...' : 'Gerando vídeo...'}
              </>
            ) : (
              <>
                <Wand2 className="h-4 w-4" />
                {activeTab === 'image'
                  ? (referenceImageBase64 ? 'Processar Imagem' : 'Gerar Imagem')
                  : (referenceImageBase64 ? 'Animar Imagem' : 'Gerar Vídeo')
                }
              </>
            )}
          </button>

          {!prompt.trim() && (
            <p className="text-center text-[11px] text-white/25">
              ↑ Escreva um prompt para habilitar o botão
            </p>
          )}
        </div>

        {/* ─── RIGHT PANEL ───────────────────────────────────────────────────────── */}
        <div className="flex-1 min-w-0">
          <div className="rounded-2xl bg-white/[0.04] border border-white/10 overflow-hidden min-h-[600px] flex flex-col">

            {/* Result header */}
            {hasResult && !isGenerating && (
              <div className="flex items-center justify-between px-6 py-4 border-b border-white/8">
                <div className="flex items-center gap-2 text-xs text-emerald-400">
                  <CheckCircle2 className="h-4 w-4" />
                  Gerado com sucesso
                </div>
                <div className="flex items-center gap-2">
                  {generationMeta && (
                    <span className="flex items-center gap-1 text-[11px] text-white/30">
                      <Clock className="h-3 w-3" />
                      {(generationMeta.ms / 1000).toFixed(1)}s · {generationMeta.provider} · {generationMeta.model}
                    </span>
                  )}
                  {activeTab === 'image' && imageSrc && (
                    <div className="flex items-center gap-1.5">
                      <button
                        type="button"
                        onClick={() => setLightboxUrl(imageSrc)}
                        className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-[11px] text-white/60 hover:text-white bg-white/8 hover:bg-white/12 border border-white/10 transition-all"
                      >
                        <ZoomIn className="h-3 w-3" /> Ampliar
                      </button>
                      <a
                        href={imageSrc}
                        download={`diax-image-${Date.now()}.png`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-[11px] text-white/60 hover:text-white bg-white/8 hover:bg-white/12 border border-white/10 transition-all"
                      >
                        <Download className="h-3 w-3" /> Download
                      </a>
                    </div>
                  )}
                  {activeTab === 'video' && videoResult?.videoUrl && (
                    <a
                      href={videoResult.videoUrl}
                      download={`diax-video-${Date.now()}.mp4`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-[11px] text-white/60 hover:text-white bg-white/8 hover:bg-white/12 border border-white/10 transition-all"
                    >
                      <Download className="h-3 w-3" /> Baixar Vídeo
                    </a>
                  )}
                </div>
              </div>
            )}

            {/* Content area */}
            <div className="flex-1 flex flex-col items-center justify-center p-6">

              {/* ── Error ── */}
              {error && !isGenerating && (() => {
                const info = getErrorInfo(errorCode, error);
                return (
                <div className="w-full max-w-lg">
                  <div className="rounded-2xl bg-red-500/10 border border-red-500/20 p-6 text-center space-y-4">
                    <div className="h-12 w-12 mx-auto rounded-2xl bg-red-500/20 flex items-center justify-center">
                      {info.isTransient
                        ? <Clock className="h-6 w-6 text-red-400" />
                        : <AlertCircle className="h-6 w-6 text-red-400" />
                      }
                    </div>
                    <div>
                      <p className="font-semibold text-red-300 mb-1">{info.title}</p>
                      <p className="text-sm text-red-400/80 leading-relaxed">{info.detail}</p>
                      {errorCode && errorCode !== 'Unknown' && (
                        <p className="text-xs text-red-500/50 mt-2 font-mono">{errorCode}</p>
                      )}
                    </div>
                    <div className="flex items-center justify-center gap-3 pt-1">
                      {info.isTransient && (
                        <button
                          type="button"
                          onClick={handleGenerate}
                          disabled={!prompt.trim() || !selectedModel}
                          className="flex items-center gap-2 px-4 py-2 rounded-xl bg-red-500/20 hover:bg-red-500/30 text-red-300 text-sm font-medium border border-red-500/30 transition-all"
                        >
                          <RefreshCw className="h-3.5 w-3.5" /> Tentar Novamente
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={clearError}
                        className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-white/8 hover:bg-white/12 text-white/50 text-sm border border-white/10 transition-all"
                      >
                        <X className="h-3.5 w-3.5" /> Fechar
                      </button>
                    </div>
                  </div>
                </div>
                );
              })()}

              {/* ── Generating ── */}
              {isGenerating && (
                <div className="w-full flex flex-col items-center justify-center gap-8 py-12">
                  {/* Animated orb */}
                  <div className="relative">
                    <div className="h-28 w-28 rounded-3xl bg-gradient-to-br from-violet-500/30 to-indigo-600/30 flex items-center justify-center border border-violet-500/20">
                      <Wand2 className="h-12 w-12 text-violet-400" style={{ animation: 'pulse 2s ease-in-out infinite' }} />
                    </div>
                    <div className="absolute -inset-3 rounded-[2rem] border-2 border-violet-500/15"
                      style={{ animation: 'ping 2s cubic-bezier(0, 0, 0.2, 1) infinite' }} />
                    <div className="absolute -inset-6 rounded-[2.5rem] border border-violet-500/8"
                      style={{ animation: 'ping 2s cubic-bezier(0, 0, 0.2, 1) infinite', animationDelay: '0.5s' }} />
                  </div>

                  {/* Message */}
                  <div className="text-center space-y-2 max-w-sm">
                    <p className="text-base font-medium text-white">
                      {currentLoadingMessages[loadingMsgIdx]}
                    </p>
                    <p className="text-sm text-white/40">
                      {activeTab === 'video'
                        ? 'Vídeos podem levar de 30 segundos a 5 minutos'
                        : 'Imagens ficam prontas em 5 a 30 segundos'
                      }
                    </p>
                  </div>

                  {/* Timer */}
                  <div className="flex items-center gap-2 px-4 py-2 rounded-full bg-white/5 border border-white/10">
                    <Clock className="h-3.5 w-3.5 text-white/40" />
                    <span className="text-xs text-white/50 font-mono">{formatSeconds(elapsedSecs)}</span>
                  </div>

                  {/* Progress dots */}
                  <div className="flex gap-2">
                    {[0, 1, 2, 3, 4].map(i => (
                      <div
                        key={i}
                        className="h-1.5 w-1.5 rounded-full bg-violet-500"
                        style={{
                          animation: 'pulse 1.5s ease-in-out infinite',
                          animationDelay: `${i * 0.2}s`,
                        }}
                      />
                    ))}
                  </div>
                </div>
              )}

              {/* ── Image Result ── */}
              {!isGenerating && !error && activeTab === 'image' && imageSrc && (
                <div className="w-full space-y-4">
                  <div
                    className="rounded-2xl overflow-hidden border border-white/10 cursor-zoom-in group"
                    onClick={() => setLightboxUrl(imageSrc)}
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={imageSrc}
                      alt={revisedPrompt ?? prompt}
                      className="w-full h-auto object-contain max-h-[70vh] transition-transform duration-300 group-hover:scale-[1.01]"
                    />
                  </div>
                  {revisedPrompt && revisedPrompt !== prompt && (
                    <div className="rounded-xl bg-white/5 border border-white/10 px-4 py-3">
                      <p className="text-[11px] text-white/40 mb-1 font-medium uppercase tracking-wider">Prompt revisado pela IA</p>
                      <p className="text-xs text-white/60 leading-relaxed">{revisedPrompt}</p>
                    </div>
                  )}
                </div>
              )}

              {/* ── Video Result ── */}
              {!isGenerating && !error && activeTab === 'video' && videoResult?.videoUrl && (
                <div className="w-full space-y-4">
                  <div className="rounded-2xl overflow-hidden border border-white/10 bg-black">
                    <video
                      controls
                      src={videoResult.videoUrl}
                      poster={videoResult.thumbnailUrl}
                      className="w-full max-h-[70vh]"
                    >
                      Seu navegador não suporta reprodução de vídeo.
                    </video>
                  </div>
                  <div className="flex items-center justify-center">
                    <a
                      href={videoResult.videoUrl}
                      download={`diax-video-${Date.now()}.mp4`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-violet-600/20 hover:bg-violet-600/30 text-violet-300 text-sm font-medium border border-violet-500/30 transition-all"
                    >
                      <Download className="h-4 w-4" /> Baixar Vídeo em MP4
                    </a>
                  </div>
                </div>
              )}

              {/* ── Empty State ── */}
              {!isGenerating && !error && !hasResult && (
                <div className="w-full flex flex-col items-center justify-center gap-8 py-16 text-center">
                  {/* Icon */}
                  <div className="relative">
                    <div className="h-20 w-20 rounded-2xl bg-gradient-to-br from-violet-500/15 to-indigo-600/15 border border-violet-500/15 flex items-center justify-center">
                      {activeTab === 'image'
                        ? <ImageIcon className="h-9 w-9 text-violet-500/50" />
                        : <Film className="h-9 w-9 text-violet-500/50" />
                      }
                    </div>
                  </div>

                  <div className="space-y-2 max-w-sm">
                    <p className="text-base font-medium text-white/60">
                      {activeTab === 'image' ? 'Sua imagem aparecerá aqui' : 'Seu vídeo aparecerá aqui'}
                    </p>
                    <p className="text-sm text-white/30 leading-relaxed">
                      {activeTab === 'image'
                        ? 'Escolha um provider, selecione um modelo e descreva sua ideia no prompt ao lado.'
                        : 'Escolha um provider de vídeo, selecione um modelo e descreva o vídeo que deseja criar.'
                      }
                    </p>
                  </div>

                  {/* Quick suggestions */}
                  <div className="w-full max-w-md space-y-2">
                    <p className="text-[11px] text-white/25 font-medium uppercase tracking-wider">Sugestões rápidas</p>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                      {(activeTab === 'image' ? IMAGE_SUGGESTIONS : VIDEO_SUGGESTIONS).map(s => (
                        <button
                          key={s}
                          type="button"
                          onClick={() => setPrompt(s)}
                          className="text-xs text-left text-white/40 hover:text-white/70 bg-white/5 hover:bg-white/10 border border-white/8 hover:border-white/15 rounded-xl px-4 py-3 transition-all leading-relaxed"
                        >
                          {s}
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Provider summary */}
                  {activeProviders.length > 0 && (
                    <div className="w-full max-w-md">
                      <p className="text-[11px] text-white/25 font-medium uppercase tracking-wider mb-2">
                        {activeProviders.length} provedores disponíveis
                      </p>
                      <div className="flex flex-wrap justify-center gap-1.5">
                        {activeProviders.map(p => {
                          const freeCount = p.models.filter(m =>
                            m.isEnabled &&
                            (activeTab === 'image' ? m.supportsImage : m.supportsVideo) &&
                            isModelFree(p.key, m.modelKey)
                          ).length;
                          return (
                            <span key={p.key} className="text-[10px] text-white/30 bg-white/5 border border-white/8 rounded-lg px-2.5 py-1">
                              {p.name}
                              {freeCount > 0 && (
                                <span className="ml-1 text-emerald-500/70">·{freeCount} grátis</span>
                              )}
                            </span>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </div>
              )}

            </div>
          </div>
        </div>
      </div>

      {/* ── Lightbox ── */}
      {lightboxUrl && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 backdrop-blur-md"
          onClick={() => setLightboxUrl(null)}
        >
          <button
            type="button"
            onClick={() => setLightboxUrl(null)}
            className="absolute top-6 right-6 h-10 w-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
          <a
            href={lightboxUrl}
            download={`diax-image-${Date.now()}.png`}
            target="_blank"
            rel="noopener noreferrer"
            className="absolute top-6 right-20 h-10 w-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors"
            onClick={e => e.stopPropagation()}
          >
            <Download className="h-4 w-4" />
          </a>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={lightboxUrl}
            alt="Visualização"
            className="max-w-[92vw] max-h-[92vh] object-contain rounded-2xl shadow-2xl"
            onClick={e => e.stopPropagation()}
          />
        </div>
      )}
    </div>
  );
}
