'use client';

import { ApiKeyConfigForm } from '@/components/admin/ai/ApiKeyConfigForm';
import { EditProviderSettingsForm } from '@/components/admin/ai/EditProviderSettingsForm';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { adminAiProvidersService, DiscoveredModel } from '@/services/adminAiProviders';
import { AiModel, AiProvider } from '@/services/aiCatalog';
import { ArrowLeft, Eye, Key, Loader2, Save, Search, Settings, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

function EditAiProviderContent() {
  const searchParams = useSearchParams();
  const id = searchParams.get('id');

  const [provider, setProvider] = useState<AiProvider | null>(null);
  const [models, setModels] = useState<AiModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [savingId, setSavingId] = useState<string | null>(null);

  // Discovery logic
  const [showModelsDialog, setShowModelsDialog] = useState(false);
  const [discoveredModels, setDiscoveredModels] = useState<DiscoveredModel[]>([]);
  const [loadingDiscovery, setLoadingDiscovery] = useState(false);
  const [selectedModelKeys, setSelectedModelKeys] = useState<string[]>([]);
  const [savingBatch, setSavingBatch] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  // Bulk actions state
  const [bulkProcessing, setBulkProcessing] = useState(false);
  const [bulkProgress, setBulkProgress] = useState('');
  // Computes which models should be shown based on search term
  const visibleModels = discoveredModels.filter(m =>
    !searchTerm ||
    m.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    m.id.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // Check if all CURRENTLY VISIBLE models are selected
  // We only check if visibleModels length > 0 to avoid checking empty lists
  const areAllVisibleSelected = visibleModels.length > 0 && visibleModels.every(m => selectedModelKeys.includes(m.id));

  const handleSelectAllVisible = () => {
    if (areAllVisibleSelected) {
      // Deselect all visible models
      const visibleIds = visibleModels.map(m => m.id);
      setSelectedModelKeys(prev => prev.filter(key => !visibleIds.includes(key)));
    } else {
      // Select all visible models
      const visibleIds = visibleModels.map(m => m.id);
      // Combine current selection with visible IDs, removing duplicates with Set
      setSelectedModelKeys(prev => [...new Set([...prev, ...visibleIds])]);
    }
  };

  const fetchData = async (silent = false) => {
    if (!id) return;
    try {
      if (!silent) setLoading(true);
      const [p, m] = await Promise.all([
        adminAiProvidersService.getById(id),
        adminAiProvidersService.getModels(id)
      ]);
      setProvider(p);
      setModels(m);
    } catch (error) {
      toast.error('Failed to load provider details.');
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [id]);

  const handleToggleModel = async (model: AiModel) => {
    try {
      setSavingId(model.id);
      await adminAiProvidersService.updateModel(model.id, {
        ...model,
        isEnabled: !model.isEnabled
      });

      // Local update for immediate feedback
      setModels(models.map(m =>
        m.id === model.id ? { ...m, isEnabled: !m.isEnabled } : m
      ));
      toast.success(`Modelo ${model.isEnabled ? 'desabilitado' : 'habilitado'} com sucesso`);
    } catch (error) {
      toast.error('Failed to update model status.');
      // Revert local state by re-fetching
      await fetchData(true);
    } finally {
        setSavingId(null);
    }
  };

  const handleDeleteModel = async (modelId: string) => {
    if (!confirm('Are you sure you want to delete this model?')) return;

    try {
      setSavingId(modelId);
      await adminAiProvidersService.deleteModel(modelId);
      toast.success('Modelo removido com sucesso');
      await fetchData(true);
    } catch (error) {
      toast.error('Erro ao remover modelo');
    } finally {
      setSavingId(null);
    }
  };

  const processBatch = async (
    items: any[],
    action: (item: any) => Promise<any>,
    actionName: string
  ) => {
    if (items.length === 0) return;

    setBulkProcessing(true);
    let successCount = 0;
    let errorCount = 0;

    try {
      for (let i = 0; i < items.length; i++) {
        setBulkProgress(`${actionName}: ${i + 1}/${items.length}`);
        try {
          await action(items[i]);
          successCount++;
        } catch (e) {
          console.error(e);
          errorCount++;
        }
      }

      toast.success(`${actionName} completed. Success: ${successCount}, Errors: ${errorCount}`);
      // Refresh data
      await fetchData(true);
    } catch (error) {
      toast.error(`Error during ${actionName}`);
    } finally {
      setBulkProcessing(false);
      setBulkProgress('');
    }
  };

  const handleEnableAll = async () => {
    const toEnable = models.filter(m => !m.isEnabled);
    if (toEnable.length === 0) {
      toast.info('All models are already enabled.');
      return;
    }

    await processBatch(
      toEnable,
      (m) => adminAiProvidersService.updateModel(m.id, { ...m, isEnabled: true }),
      'Enabling models'
    );
  };

  const handleDisableAll = async () => {
    const toDisable = models.filter(m => m.isEnabled);
    if (toDisable.length === 0) {
      toast.info('All models are already disabled.');
      return;
    }

    await processBatch(
      toDisable,
      (m) => adminAiProvidersService.updateModel(m.id, { ...m, isEnabled: false }),
      'Disabling models'
    );
  };

  const handleDeleteAll = async () => {
    if (models.length === 0) return;

    const confirmMessage = `CAUTION: You are about to DELETE ALL ${models.length} models for this provider.\n\nThis action cannot be undone.\n\nType "DELETE" to confirm.`;
    const userConfirmation = prompt(confirmMessage);

    if (userConfirmation !== 'DELETE') return;

    await processBatch(
      models,
      (m) => adminAiProvidersService.deleteModel(m.id),
      'Deleting models'
    );
  };

  const handleViewModels = async () => {
    if (!provider) return;

    try {
      setLoadingDiscovery(true);
      setShowModelsDialog(true);
      setDiscoveredModels([]);
      setSelectedModelKeys([]);
      setSearchTerm('');

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
      setLoadingDiscovery(false);
    }
  };

  const handleToggleModelSelection = (key: string) => {
    setSelectedModelKeys(prev =>
      prev.includes(key) ? prev.filter(k => k !== key) : [...prev, key]
    );
  };

  const handleSaveBatch = async () => {
    if (selectedModelKeys.length === 0 || !id) return;

    try {
      setSavingBatch(true);
      const modelsToSave = discoveredModels.filter(m => selectedModelKeys.includes(m.id));
      const result = await adminAiProvidersService.addBatchModels(id, modelsToSave);

      if (result.success) {
        toast.success(result.message);
        setShowModelsDialog(false);
        await fetchData(true);
      } else {
        toast.error('Erro ao salvar modelos');
      }
    } catch (error: any) {
      toast.error(error?.message || 'Erro ao salvar modelos');
    } finally {
      setSavingBatch(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!provider || !id) return <div>Provider not found</div>;

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" asChild>
            <Link href="/admin/ai">
              <ArrowLeft className="h-4 w-4 mr-2" /> Back
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">{provider.name} Models</h1>
            <p className="text-muted-foreground">Manage available models for this provider.</p>
          </div>
        </div>
        {provider.supportsListModels && (
          <Button onClick={handleViewModels} variant="outline">
            <Eye className="h-4 w-4 mr-2" /> Ver Modelos (API)
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            Provider Settings
          </CardTitle>
          <CardDescription>
            Configure provider metadata and features.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <EditProviderSettingsForm
            provider={provider}
            onUpdated={() => fetchData(true)}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Key className="h-5 w-5" />
            API Key Configuration
          </CardTitle>
          <CardDescription>
            Configure the API key for this provider. Keys are encrypted and stored securely.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <ApiKeyConfigForm
            providerId={id}
            providerName={provider.name}
            onSaved={() => toast.success('API key saved successfully')}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between flex-wrap gap-4">
            <CardTitle>Models</CardTitle>
            <div className="flex items-center gap-2">
                {bulkProcessing && (
                    <span className="text-sm text-muted-foreground mr-2 flex items-center gap-2">
                        <Loader2 className="h-3 w-3 animate-spin"/> {bulkProgress}
                    </span>
                )}
                <Button
                    variant="outline"
                    size="sm"
                    onClick={handleEnableAll}
                    disabled={models.length === 0 || bulkProcessing}
                >
                    Enable All
                </Button>
                <Button
                    variant="outline"
                    size="sm"
                    onClick={handleDisableAll}
                    disabled={models.length === 0 || bulkProcessing}
                >
                    Disable All
                </Button>
                <Button
                    variant="destructive"
                    size="sm"
                    onClick={handleDeleteAll}
                    disabled={models.length === 0 || bulkProcessing}
                >
                    Delete All
                </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Status</TableHead>
                <TableHead>Key</TableHead>
                <TableHead>Display Name</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {models.map((model) => (
                <TableRow key={model.id}>
                  <TableCell>
                    {model.isEnabled ? (
                      <Badge variant="default" className="bg-green-600">Active</Badge>
                    ) : (
                      <Badge variant="secondary">Disabled</Badge>
                    )}
                  </TableCell>
                  <TableCell className="font-mono text-sm">{model.modelKey}</TableCell>
                  <TableCell>{model.displayName}</TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant={model.isEnabled ? "destructive" : "default"}
                        size="sm"
                        onClick={() => handleToggleModel(model)}
                        disabled={savingId === model.id}
                        className="w-[100px]"
                      >
                        {savingId === model.id ? <Loader2 className="h-4 w-4 animate-spin" /> : (model.isEnabled ? 'Disable' : 'Enable')}
                      </Button>

                      <Button
                        variant="outline"
                        size="icon"
                        className="h-8 w-8 text-destructive hover:bg-destructive/10"
                        onClick={() => handleDeleteModel(model.id)}
                        disabled={savingId === model.id}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
              {models.length === 0 && (
                <TableRow>
                    <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                        No models found. Try syncing models from the main page.
                    </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Dialog for viewing available models from API */}
      <Dialog open={showModelsDialog} onOpenChange={setShowModelsDialog}>
        <DialogContent className="max-w-4xl max-h-[90vh] flex flex-col overflow-hidden">
          <DialogHeader>
            <DialogTitle>Modelos Disponíveis (API) - {provider.name}</DialogTitle>
            <DialogDescription>
              Selecione os modelos que deseja habilitar no sistema para o provedor {provider.key}.
            </DialogDescription>
          </DialogHeader>

          {loadingDiscovery ? (
            <div className="flex flex-col justify-center items-center py-12">
              <Loader2 className="h-10 w-10 animate-spin text-primary" />
              <span className="mt-4 text-muted-foreground font-medium">Carregando modelos da API...</span>
            </div>
          ) : (
            <>
              <div className="relative mb-4 px-1">
                <Search className="absolute left-3.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  type="search"
                  placeholder="Filtrar modelos por nome ou ID..."
                  className="pl-9"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>
              <div className="flex-1 overflow-y-auto pr-2 min-h-0">
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
                    {visibleModels.map((model) => {
                      const isAlreadyInSystem = models.some(m => m.modelKey === model.id);
                      return (
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
                          <TableCell className="font-mono text-xs">
                            <div className="flex items-center gap-2">
                                {model.id}
                                {isAlreadyInSystem && <Badge variant="secondary" className="text-[10px]">In System</Badge>}
                            </div>
                          </TableCell>
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
                      );
                    })}
                    {searchTerm && visibleModels.length === 0 && (
                        <TableRow>
                            <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                                Nenhum modelo encontrado para "{searchTerm}"
                            </TableCell>
                        </TableRow>
                    )}
                  </TableBody>
                </Table>
              </div>

              <div className="flex flex-col gap-4 pt-6 border-t mt-4 bg-background">
                <div className="flex flex-wrap items-center justify-between gap-4">
                  <div className="text-sm">
                    <span className="font-semibold text-primary">{selectedModelKeys.length}</span> modelos selecionados
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setSelectedModelKeys(discoveredModels.map(m => m.id))}
                      disabled={discoveredModels.length === 0}
                    >
                      Selecionar Todos ({discoveredModels.length})
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setSelectedModelKeys([])}
                      disabled={selectedModelKeys.length === 0}
                    >
                      Deselecionar Todos
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleSelectAllVisible}
                      disabled={visibleModels.length === 0}
                    >
                      {areAllVisibleSelected ? 'Deselecionar Filtrados' : `Selecionar Filtrados (${visibleModels.length})`}
                    </Button>
                  </div>
                </div>

                <div className="flex justify-end gap-3">
                  <Button variant="ghost" onClick={() => setShowModelsDialog(false)}>
                    Cancelar
                  </Button>
                  <Button
                    onClick={handleSaveBatch}
                    disabled={selectedModelKeys.length === 0 || savingBatch}
                    className="min-w-[150px]"
                  >
                    {savingBatch ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Salvando...
                      </>
                    ) : (
                      <>
                        <Save className="mr-2 h-4 w-4" />
                        Salvar Selecionados
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

export default function AiProviderDetailsPage() {
    return (
        <Suspense fallback={<div className="flex justify-center p-10"><Loader2 className="animate-spin text-primary" /></div>}>
            <EditAiProviderContent />
        </Suspense>
    );
}
