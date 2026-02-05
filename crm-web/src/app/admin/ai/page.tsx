'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { adminAiProvidersService } from '@/services/adminAiProviders';
import { AiProvider } from '@/services/aiCatalog';
import { Layers, Loader2, RefreshCw } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

export default function AiAdminPage() {
  const [providers, setProviders] = useState<AiProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState<string | null>(null);

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
    </div>
  );
}
