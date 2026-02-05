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
import { AiModel, AiProvider } from '@/services/aiCatalog';
import { ArrowLeft, Loader2 } from 'lucide-react';
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

  useEffect(() => {
    if (!id) return;

    const fetchData = async () => {
      try {
        setLoading(true);
        const [p, m] = await Promise.all([
          adminAiProvidersService.getById(id),
          adminAiProvidersService.getModels(id)
        ]);
        setProvider(p);
        setModels(m);
      } catch (error) {
        toast.error('Failed to load provider details.');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [id]);

  const handleToggleModel = async (model: AiModel) => {
    try {
      setSavingId(model.id);
      await adminAiProvidersService.updateModel(model.id, {
        isEnabled: !model.isEnabled
      });

      setModels(models.map(m =>
        m.id === model.id ? { ...m, isEnabled: !m.isEnabled } : m
      ));
    } catch (error) {
      toast.error('Failed to update model status.');
    } finally {
        setSavingId(null);
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

      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
            <div>
                <span className="font-semibold">Key:</span> {provider.key}
            </div>
            <div>
                <span className="font-semibold">Base URL:</span> {provider.baseUrl || 'Default'}
            </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Models</CardTitle>
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
                    <Button
                      variant={model.isEnabled ? "destructive" : "default"}
                      size="sm"
                      onClick={() => handleToggleModel(model)}
                      disabled={savingId === model.id}
                    >
                      {savingId === model.id && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                      {model.isEnabled ? 'Disable' : 'Enable'}
                    </Button>
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
