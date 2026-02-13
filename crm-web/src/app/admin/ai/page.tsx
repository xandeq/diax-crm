'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
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
import { Eye, Layers, Loader2, RefreshCw, Save, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { CreateProviderDialog } from '@/components/admin/ai/CreateProviderDialog';

export default function AiAdminPage() {
  const [providers, setProviders] = useState<AiProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState<string | null>(null);
  const [savingBatch, setSavingBatch] = useState(false);
  const [deleting, setDeleting] = useState<string | null>(null);

  // Dialog state for viewing models
  const [showModelsDialog, setShowModelsDialog] = useState(false);
  const [discoveredModels, setDiscoveredModels] = useState<DiscoveredModel[]>([]);
  const [loadingModels, setLoadingModels] = useState(false);
  const [selectedProviderId, setSelectedProviderId] = useState<string>('');
  const [selectedProviderKey, setSelectedProviderKey] = useState<string>('');
  const [selectedModelKeys, setSelectedModelKeys] = useState<string[]>([]);
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

  const handleViewModels = async (provider: AiProvider) => {
    try {
      setLoadingModels(true);
      setSelectedProviderId(provider.id);
      setSelectedProviderKey(provider.key);
      setShowModelsDialog(true);
      setDiscoveredModels([]);
      setSelectedModel('');
      setSelectedModelKeys([]);

      const response = await adminAiProvidersService.discoverModels(provider.key);

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

  const handleToggleModelSelection = (key: string) => {
    setSelectedModelKeys(prev =>
      prev.includes(key) ? prev.filter(k => k !== key) : [...prev, key]
    );
  };

  const handleSaveBatch = async () => {
    if (selectedModelKeys.length === 0) return;

    try {
      setSavingBatch(true);
      const modelsToSave = discoveredModels.filter(m => selectedModelKeys.includes(m.id));
      const result = await adminAiProvidersService.addBatchModels(selectedProviderId, modelsToSave);

      if (result.success) {
        toast.success(result.message);
        setShowModelsDialog(false);
      } else {
        toast.error('Erro ao salvar modelos');
      }
    } catch (error: any) {
      toast.error(error?.message || 'Erro ao salvar modelos');
    } finally {
      setSavingBatch(false);
    }
  };

  const handleDeleteProvider = async (provider: AiProvider) => {
    const confirmMessage = `Are you sure you want to delete "${provider.name}"?\n\nThis will also delete:\n- All associated models\n- API key credentials\n- Group access permissions\n\nThis action cannot be undone.`;

    if (!confirm(confirmMessage)) return;

    try {
      setDeleting(provider.id);
      await adminAiProvidersService.delete(provider.id);

      toast.success(`Provider "${provider.name}" deleted successfully`);

      // Remove from local state
      setProviders(providers.filter(p => p.id !== provider.id));
    } catch (error: any) {
      console.error('Error deleting provider:', error);
      toast.error(error?.response?.data?.message || 'Failed to delete provider');
    } finally {
      setDeleting(null);
    }
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">AI Providers Administration</h1>
          <p className="text-muted-foreground">Manage AI providers, models, and access controls.</p>
        </div>
        <div className="flex gap-2">
          <CreateProviderDialog onCreated={loadProviders} />
          <Button onClick={loadProviders} variant="outline">
            <RefreshCw className="mr-2 h-4 w-4" /> Refresh
          </Button>
        </div>
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
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          {provider.supportsListModels && (
                            <>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleSyncModels(provider)}
                                disabled={syncing === provider.id || !provider.isEnabled || deleting === provider.id}
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
                                onClick={() => handleViewModels(provider)}
                                disabled={deleting === provider.id}
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
                            disabled={deleting === provider.id}
                          >
                            {provider.isEnabled ? 'Disable' : 'Enable'}
                          </Button>

                          <Button
                            variant="outline"
                            size="sm"
                            asChild
                            disabled={deleting === provider.id}
                          >
                              <a href={`/admin/ai/edit?id=${provider.id}`}>Manage</a>
                          </Button>

                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDeleteProvider(provider)}
                            disabled={deleting === provider.id}
                            className="text-destructive hover:text-destructive hover:bg-destructive/10"
                          >
                            {deleting === provider.id ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Trash2 className="h-4 w-4" />
                            )}
                          </Button>
                        </div>
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
        <DialogContent className="max-w-4xl max-h-[90vh] flex flex-col">
          <DialogHeader>
            <DialogTitle>Modelos Disponíveis - {selectedProviderKey.toUpperCase()}</DialogTitle>
            <DialogDescription>
              Selecione os modelos que deseja habilitar no sistema para o provedor {selectedProviderKey}.
            </DialogDescription>
          </DialogHeader>

          {loadingModels ? (
            <div className="flex flex-col justify-center items-center py-12">
              <Loader2 className="h-10 w-10 animate-spin text-primary" />
              <span className="mt-4 text-muted-foreground font-medium">Carregando modelos da API...</span>
            </div>
          ) : (
            <>
              <div className="flex-1 overflow-y-auto pr-2">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-[50px]">Select</TableHead>
                      <TableHead>Model ID</TableHead>
                      <TableHead>Name</TableHead>
                      <TableHead>Details</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {discoveredModels.map((model) => (
                      <TableRow
                        key={model.id}
                        className="cursor-pointer hover:bg-muted/50"
                        onClick={() => handleToggleModelSelection(model.id)}
                      >
                        <TableCell onClick={(e) => e.stopPropagation()}>
                          <Checkbox
                            checked={selectedModelKeys.includes(model.id)}
                            onCheckedChange={() => handleToggleModelSelection(model.id)}
                          />
                        </TableCell>
                        <TableCell className="font-mono text-xs">{model.id}</TableCell>
                        <TableCell className="font-medium text-sm">{model.name}</TableCell>
                        <TableCell>
                          <div className="text-xs text-muted-foreground space-y-0.5">
                            {model.contextLength && <span>Ctx: {model.contextLength.toLocaleString()}</span>}
                            {model.inputCostHint && (
                              <div className="flex gap-2">
                                <span>In: ${model.inputCostHint}</span>
                                <span>Out: ${model.outputCostHint}</span>
                              </div>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>

              <div className="flex justify-between items-center pt-6 border-t mt-4 bg-background">
                <div className="text-sm">
                  <span className="font-semibold text-primary">{selectedModelKeys.length}</span> modelos selecionados de <span className="font-semibold">{discoveredModels.length}</span>
                </div>
                <div className="flex gap-3">
                  <Button variant="outline" onClick={() => setShowModelsDialog(false)}>
                    Cancelar
                  </Button>
                  <Button
                    onClick={handleSaveBatch}
                    disabled={selectedModelKeys.length === 0 || savingBatch}
                    className="min-w-[180px]"
                  >
                    {savingBatch ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Salvando...
                      </>
                    ) : (
                      <>
                        <Save className="mr-2 h-4 w-4" />
                        Adicionar Selecionados
                      </>
                    )}
                  </Button>
                </div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
