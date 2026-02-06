'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { adminAiProvidersService, DiscoveredModel } from '@/services/adminAiProviders';
import { AiProvider } from '@/services/aiCatalog';
import { Eye, Layers, Loader2, RefreshCw } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

export default function AiAdminPage() {
  const [providers, setProviders] = useState<AiProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState<string | null>(null);

  // Dialog state for viewing models
  const [showModelsDialog, setShowModelsDialog] = useState(false);
  const [discoveredModels, setDiscoveredModels] = useState<DiscoveredModel[]>([]);
  const [loadingModels, setLoadingModels] = useState(false);
  const [selectedProvider, setSelectedProvider] = useState<string>('');
  const [selectedModel, setSelectedModel] = useState<string>('');

  const loadProviders = async () => {
    try {
      setLoading(true);
      const data = await adminAiProvidersService.getAll();
      setProviders(data);
    } catch (error) {
      console.error(error);
      toast.error('Failed to load AI Providers.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProviders();
  }, []);

  const handleToggleProvider = async (provider: AiProvider) => {
    try {
      await adminAiProvidersService.update(provider.id, {
        name: provider.name,
        supportsListModels: provider.supportsListModels,
        baseUrl: provider.baseUrl,
        isEnabled: !provider.isEnabled,
      });

      // Update local state
      setProviders(providers.map(p =>
        p.id === provider.id ? { ...p, isEnabled: !p.isEnabled } : p
      ));

      toast.success(`Provider ${provider.name} ${!provider.isEnabled ? 'enabled' : 'disabled'}.`);
    } catch (error) {
      toast.error('Failed to update provider status.');
    }
  };

  const handleSyncModels = async (provider: AiProvider) => {
    if (!provider.supportsListModels) return;

    try {
      setSyncing(provider.id);
      const result = await adminAiProvidersService.syncModels(provider.id);

      toast.success(`Sync Completed: Discovered: ${result.discoveredCount}, New: ${result.newModels}, Updated: ${result.existingModelsUpdated}`);

      // Optionally reload providers or just the models for this provider if we were showing them detailed
    } catch (error) {
      toast.error('Failed to synchronize models.');
    } finally {
      setSyncing(null);
    }
  };

  const handleViewModels = async (providerKey: string) => {
    try {
      setLoadingModels(true);
      setSelectedProvider(providerKey);
      setShowModelsDialog(true);
      setDiscoveredModels([]);
      setSelectedModel('');

      const response = await adminAiProvidersService.discoverModels(providerKey);

      if (response.success && response.data) {
        setDiscoveredModels(response.data);
        toast.success(`${response.totalCount} modelos carregados com sucesso`);
      } else {
        toast.error(response.error || 'Erro ao carregar modelos');
        setShowModelsDialog(false);
      }
    } catch (error: any) {
      console.error('Error discovering models:', error);
      toast.error(error?.message || 'Falha ao carregar modelos disponíveis');
      setShowModelsDialog(false);
    } finally {
      setLoadingModels(false);
    }
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">AI Providers Administration</h1>
          <p className="text-muted-foreground">Manage AI providers, models, and access controls.</p>
        </div>
        <Button onClick={loadProviders} variant="outline">
          <RefreshCw className="mr-2 h-4 w-4" /> Refresh
        </Button>
      </div>

      <div className="grid gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-xl flex items-center gap-2">
              <Layers className="h-5 w-5" /> Registered Providers
            </CardTitle>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="flex justify-center p-8">
                <Loader2 className="h-8 w-8 animate-spin text-primary" />
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Status</TableHead>
                    <TableHead>Name</TableHead>
                    <TableHead>Key</TableHead>
                    <TableHead>Base URL</TableHead>
                    <TableHead>Features</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {providers.map((provider) => (
                    <TableRow key={provider.id}>
                      <TableCell>
                        {provider.isEnabled ? (
                          <Badge variant="default" className="bg-green-600 hover:bg-green-700">Active</Badge>
                        ) : (
                          <Badge variant="secondary">Disabled</Badge>
                        )}
                      </TableCell>
                      <TableCell className="font-medium">{provider.name}</TableCell>
                      <TableCell className="font-mono text-sm">{provider.key}</TableCell>
                      <TableCell className="text-sm text-muted-foreground truncate max-w-[200px]">
                        {provider.baseUrl || '-'}
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-2">
                            {provider.supportsListModels && (
                                <Badge variant="outline">Auto-Sync</Badge>
                            )}
                        </div>
                      </TableCell>
                      <TableCell className="text-right space-x-2">
                        {provider.supportsListModels && (
                          <>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleSyncModels(provider)}
                              disabled={syncing === provider.id || !provider.isEnabled}
                            >
                              {syncing === provider.id ? (
                                  <Loader2 className="h-4 w-4 animate-spin" />
                              ) : (
                                  <RefreshCw className="h-4 w-4 mr-1" />
                              )}
                              Sync
                            </Button>

                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleViewModels(provider.key)}
                            >
                              <Eye className="h-4 w-4 mr-1" />
                              Ver Modelos
                            </Button>
                          </>
                        )}

                        <Button
                          variant={provider.isEnabled ? "destructive" : "default"}
                          size="sm"
                          onClick={() => handleToggleProvider(provider)}
                        >
                          {provider.isEnabled ? 'Disable' : 'Enable'}
                        </Button>

                        <Button variant="outline" size="sm" asChild>
                            <a href={`/admin/ai/edit?id=${provider.id}`}>Manage Models</a>
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Dialog for viewing available models */}
      <Dialog open={showModelsDialog} onOpenChange={setShowModelsDialog}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Modelos Disponíveis - {selectedProvider.toUpperCase()}</DialogTitle>
            <DialogDescription>
              Visualize todos os modelos disponíveis no provedor {selectedProvider}
            </DialogDescription>
          </DialogHeader>

          {loadingModels ? (
            <div className="flex justify-center items-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
              <span className="ml-3 text-muted-foreground">Carregando modelos...</span>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">Selecione um modelo:</label>
                <Select value={selectedModel} onValueChange={setSelectedModel}>
                  <SelectTrigger>
                    <SelectValue placeholder="Escolha um modelo..." />
                  </SelectTrigger>
                  <SelectContent>
                    {discoveredModels.map((model) => (
                      <SelectItem key={model.id} value={model.id}>
                        {model.name} ({model.id})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {selectedModel && (
                <div className="mt-4 p-4 bg-muted rounded-lg">
                  <h4 className="font-semibold mb-2">Detalhes do Modelo:</h4>
                  {(() => {
                    const model = discoveredModels.find(m => m.id === selectedModel);
                    return model ? (
                      <div className="space-y-1 text-sm">
                        <p><span className="font-medium">ID:</span> {model.id}</p>
                        <p><span className="font-medium">Nome:</span> {model.name}</p>
                        <p><span className="font-medium">Provider:</span> {model.provider}</p>
                        {model.contextLength && (
                          <p><span className="font-medium">Context Length:</span> {model.contextLength.toLocaleString()} tokens</p>
                        )}
                        {model.inputCostHint && (
                          <p><span className="font-medium">Input Cost:</span> ${model.inputCostHint}</p>
                        )}
                        {model.outputCostHint && (
                          <p><span className="font-medium">Output Cost:</span> ${model.outputCostHint}</p>
                        )}
                      </div>
                    ) : null;
                  })()}
                </div>
              )}

              <div className="flex justify-between items-center pt-4 border-t">
                <p className="text-sm text-muted-foreground">
                  {discoveredModels.length} modelos disponíveis
                </p>
                <Button variant="outline" onClick={() => setShowModelsDialog(false)}>
                  Fechar
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
