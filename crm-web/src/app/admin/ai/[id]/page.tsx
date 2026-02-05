'use client';

import { useEffect, useState } from 'react';
import { adminAiProvidersService } from '@/services/adminAiProviders';
import { AiProvider, AiModel } from '@/services/aiCatalog';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Table,
  TableHeader,
  TableRow,
  TableHead,
  TableBody,
  TableCell,
} from '@/components/ui/table';
import { Loader2, ArrowLeft, Save } from 'lucide-react';
import { useToast } from '@/components/ui/use-toast'; 
import Link from 'next/link';

export default function AiProviderDetailsPage({ params }: { params: { id: string } }) {
  const [provider, setProvider] = useState<AiProvider | null>(null);
  const [models, setModels] = useState<AiModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [savingId, setSavingId] = useState<string | null>(null);
  const { toast } = useToast();

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [p, m] = await Promise.all([
          adminAiProvidersService.getById(params.id),
          adminAiProvidersService.getModels(params.id)
        ]);
        setProvider(p);
        setModels(m);
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to load provider details.',
          variant: 'destructive',
        });
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [params.id]);

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
      toast({
        title: 'Error',
        description: 'Failed to update model status.',
        variant: 'destructive',
      });
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

  if (!provider) return <div>Provider not found</div>;

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
