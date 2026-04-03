'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { useAiCatalog } from '@/hooks/useAiCatalog';
import { ProviderModelSelector } from '@/components/ai/ProviderModelSelector';
import { generateSocialBatch, type SocialPostDto } from '@/services/socialMediaBatch';
import { exportToCSV } from '@/lib/export';
import {
  AlertCircle,
  Calendar,
  Clock,
  Copy,
  Download,
  Eraser,
  Hash,
  Image,
  Instagram,
  Linkedin,
  Loader2,
  Sparkles,
  X,
} from 'lucide-react';
import { useState } from 'react';

const PLATFORMS = [
  { id: 'instagram', label: 'Instagram', icon: Instagram, color: 'text-pink-600' },
  { id: 'linkedin', label: 'LinkedIn', icon: Linkedin, color: 'text-blue-700' },
  { id: 'facebook', label: 'Facebook', icon: () => <span className="text-blue-500 font-bold text-xs">f</span>, color: 'text-blue-500' },
];

const CONTENT_TYPE_LABELS: Record<string, string> = {
  carrossel: 'Carrossel',
  post_imagem: 'Post com Imagem',
  reels: 'Reels',
  stories: 'Stories',
  texto: 'Texto',
};

const PLATFORM_COLORS: Record<string, string> = {
  instagram: 'bg-pink-100 text-pink-800 border-pink-200',
  linkedin: 'bg-blue-100 text-blue-800 border-blue-200',
  facebook: 'bg-indigo-100 text-indigo-800 border-indigo-200',
};

export default function SocialBatchGeneratorPage() {
  const { providers, selectedProvider, selectedModel, setSelectedProvider, setSelectedModel, isReady } = useAiCatalog();

  const [topics, setTopics] = useState<string[]>([]);
  const [topicInput, setTopicInput] = useState('');
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>(['instagram']);
  const [postCount, setPostCount] = useState(15);
  const [month, setMonth] = useState('');
  const [brandVoice, setBrandVoice] = useState('');
  const [targetAudience, setTargetAudience] = useState('');
  const [results, setResults] = useState<(SocialPostDto & { copied?: boolean })[]>([]);
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

  const addTopic = () => {
    const trimmed = topicInput.trim();
    if (trimmed && !topics.includes(trimmed)) {
      setTopics([...topics, trimmed]);
      setTopicInput('');
    }
  };

  const removeTopic = (topic: string) => {
    setTopics(topics.filter((t) => t !== topic));
  };

  const togglePlatform = (platformId: string) => {
    setSelectedPlatforms((prev) =>
      prev.includes(platformId) ? prev.filter((p) => p !== platformId) : [...prev, platformId]
    );
  };

  const handleGenerate = async () => {
    if (topics.length === 0) {
      setError('Adicione pelo menos um tópico.');
      return;
    }
    if (selectedPlatforms.length === 0) {
      setError('Selecione pelo menos uma plataforma.');
      return;
    }

    setIsLoading(true);
    setError(null);
    setResults([]);

    try {
      const response = await generateSocialBatch({
        provider: selectedProvider,
        model: selectedModel || undefined,
        topics,
        platforms: selectedPlatforms,
        postCount,
        month: month || undefined,
        brandVoice: brandVoice || undefined,
        targetAudience: targetAudience || undefined,
        temperature: 0.8,
      });

      setResults(response.posts.map((p) => ({ ...p, copied: false })));
      setRequestId(response.requestId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar batch de posts');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async (index: number) => {
    const post = results[index];
    const text = `${post.caption}\n\n${post.hashtags.map((h) => `#${h}`).join(' ')}`;
    try {
      await navigator.clipboard.writeText(text);
      setResults((prev) => {
        const updated = [...prev];
        updated[index].copied = true;
        return updated;
      });
      setTimeout(() => {
        setResults((prev) => {
          const updated = [...prev];
          updated[index].copied = false;
          return updated;
        });
      }, 2000);
    } catch {
      setError('Erro ao copiar');
    }
  };

  const handleExportCSV = () => {
    const data = results.map((p) => ({
      numero: p.number,
      plataforma: p.platform,
      tipo: p.contentType,
      caption: p.caption,
      hashtags: p.hashtags.map((h) => `#${h}`).join(' '),
      imagemPrompt: p.imagePrompt ?? '',
      dimensao: p.imageDimension,
      melhorHorario: p.bestTimeToPost,
      topico: p.topic,
    }));
    exportToCSV(data, `social-batch-${new Date().toISOString().slice(0, 10)}`);
  };

  const handleClear = () => {
    setTopics([]);
    setTopicInput('');
    setResults([]);
    setError(null);
    setRequestId(null);
  };

  return (
    <div className="space-y-6 pb-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <Calendar className="h-8 w-8 text-pink-500" />
          Gerador de Conteúdo Social em Lote
        </h1>
        <p className="text-muted-foreground mt-2">
          Gere {postCount}+ posts para suas redes sociais de uma vez com IA
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

              {/* Topics */}
              <div className="space-y-2">
                <Label className="font-semibold">Tópicos *</Label>
                <div className="flex gap-2">
                  <Input
                    placeholder="Ex: marketing digital, IA, vendas..."
                    value={topicInput}
                    onChange={(e) => setTopicInput(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addTopic())}
                  />
                  <Button variant="outline" size="sm" onClick={addTopic} disabled={!topicInput.trim()}>
                    +
                  </Button>
                </div>
                {topics.length > 0 && (
                  <div className="flex flex-wrap gap-1.5 mt-2">
                    {topics.map((topic) => (
                      <Badge key={topic} variant="secondary" className="gap-1 pr-1">
                        {topic}
                        <button onClick={() => removeTopic(topic)} className="hover:text-destructive">
                          <X className="h-3 w-3" />
                        </button>
                      </Badge>
                    ))}
                  </div>
                )}
              </div>

              {/* Platforms */}
              <div className="space-y-2">
                <Label className="font-semibold">Plataformas *</Label>
                <div className="space-y-2">
                  {PLATFORMS.map(({ id, label, icon: Icon, color }) => (
                    <label key={id} className="flex items-center gap-2 cursor-pointer">
                      <Checkbox
                        checked={selectedPlatforms.includes(id)}
                        onCheckedChange={() => togglePlatform(id)}
                      />
                      <Icon className={`h-4 w-4 ${color}`} />
                      <span className="text-sm">{label}</span>
                    </label>
                  ))}
                </div>
              </div>

              {/* Post count */}
              <div className="space-y-2">
                <Label htmlFor="postCount" className="font-semibold">
                  Quantidade de Posts
                </Label>
                <Input
                  id="postCount"
                  type="number"
                  min={3}
                  max={30}
                  value={postCount}
                  onChange={(e) => setPostCount(Number(e.target.value))}
                />
              </div>

              {/* Optional fields */}
              <div className="space-y-2">
                <Label htmlFor="month" className="font-semibold">
                  Mês de Referência
                </Label>
                <Input
                  id="month"
                  placeholder="Ex: Abril 2026"
                  value={month}
                  onChange={(e) => setMonth(e.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="brandVoice" className="font-semibold">
                  Tom da Marca
                </Label>
                <Input
                  id="brandVoice"
                  placeholder="Ex: Profissional mas acessível, informal..."
                  value={brandVoice}
                  onChange={(e) => setBrandVoice(e.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="audience" className="font-semibold">
                  Público-alvo
                </Label>
                <Input
                  id="audience"
                  placeholder="Ex: Empreendedores, pequenos negócios..."
                  value={targetAudience}
                  onChange={(e) => setTargetAudience(e.target.value)}
                />
              </div>

              {error && (
                <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 flex gap-3 text-sm">
                  <AlertCircle className="h-4 w-4 text-destructive flex-shrink-0 mt-0.5" />
                  <p className="text-destructive">{error}</p>
                </div>
              )}

              <div className="flex gap-2 pt-2">
                <Button onClick={handleGenerate} disabled={isLoading || topics.length === 0 || selectedPlatforms.length === 0} className="flex-1">
                  {isLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Gerando...
                    </>
                  ) : (
                    <>
                      <Sparkles className="h-4 w-4 mr-2" />
                      Gerar Posts
                    </>
                  )}
                </Button>
                <Button variant="outline" onClick={handleClear} disabled={isLoading || (!topics.length && !results.length)} size="sm">
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
                <h2 className="text-xl font-semibold">{results.length} Posts Gerados</h2>
                <div className="flex items-center gap-2">
                  {requestId && <Badge variant="secondary">{requestId.slice(0, 8)}...</Badge>}
                  <Button variant="outline" size="sm" onClick={handleExportCSV}>
                    <Download className="h-4 w-4 mr-1.5" />
                    CSV
                  </Button>
                </div>
              </div>

              <div className="grid gap-4">
                {results.map((post, index) => {
                  const platColor = PLATFORM_COLORS[post.platform] || 'bg-gray-100 text-gray-800 border-gray-200';

                  return (
                    <Card key={index} className="hover:border-primary/50 transition-colors">
                      <CardHeader className="pb-2">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <Badge variant="outline" className="font-bold">
                              #{post.number}
                            </Badge>
                            <Badge className={`${platColor} border`}>{post.platform}</Badge>
                            <Badge variant="secondary">
                              {CONTENT_TYPE_LABELS[post.contentType] || post.contentType}
                            </Badge>
                          </div>
                          <div className="flex items-center gap-2 text-xs text-muted-foreground">
                            <Clock className="h-3 w-3" />
                            {post.bestTimeToPost}
                          </div>
                        </div>
                      </CardHeader>
                      <CardContent className="space-y-3">
                        {/* Caption */}
                        <div className="text-sm bg-muted p-3 rounded border whitespace-pre-wrap max-h-[200px] overflow-y-auto">
                          {post.caption}
                        </div>

                        {/* Hashtags */}
                        {post.hashtags.length > 0 && (
                          <div className="flex flex-wrap gap-1">
                            <Hash className="h-3.5 w-3.5 text-muted-foreground mt-0.5" />
                            {post.hashtags.map((tag) => (
                              <span key={tag} className="text-xs text-blue-600">
                                #{tag}
                              </span>
                            ))}
                          </div>
                        )}

                        {/* Image prompt */}
                        {post.imagePrompt && (
                          <div className="flex items-start gap-2 text-xs text-muted-foreground border-t pt-2">
                            <Image className="h-3.5 w-3.5 mt-0.5 flex-shrink-0" />
                            <div>
                              <span className="font-medium">Prompt de imagem ({post.imageDimension}):</span>{' '}
                              {post.imagePrompt}
                            </div>
                          </div>
                        )}

                        {/* Actions */}
                        <div className="flex items-center justify-between pt-1">
                          <Badge variant="outline" className="text-xs">
                            {post.topic}
                          </Badge>
                          <Button variant="ghost" size="sm" onClick={() => handleCopy(index)} className="text-xs">
                            {post.copied ? '✓ Copiado' : <><Copy className="h-3 w-3 mr-1" /> Copiar</>}
                          </Button>
                        </div>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            </div>
          ) : (
            <Card className="h-full flex items-center justify-center min-h-[400px]">
              <CardContent className="text-center">
                <Calendar className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
                <p className="text-muted-foreground">
                  Adicione tópicos, selecione plataformas e clique em "Gerar Posts"
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
