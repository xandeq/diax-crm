'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { adminAiProvidersService } from '@/services/adminAiProviders';
import { adminGroupsService, GroupAiAccessDto, UserGroup } from '@/services/adminGroups';
import { AiModel, AiProvider } from '@/services/aiCatalog';
import { ArrowLeft, Bot, Loader2, Save } from 'lucide-react';
import Link from 'next/link';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';
import { useSearchParams } from 'next/navigation';

function EditGroupContent() {
  const searchParams = useSearchParams();
  const id = searchParams.get('id');

  const [group, setGroup] = useState<UserGroup | null>(null);
  const [providers, setProviders] = useState<AiProvider[]>([]);
  const [modelsByProvider, setModelsByProvider] = useState<Record<string, AiModel[]>>({});

  const [access, setAccess] = useState<GroupAiAccessDto>({ 
      groupId: id || '', 
      allowedProviderIds: [], 
      allowedModelIds: [] 
  });
  
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!id) return;
    
    const fetchData = async () => {
      try {
        setLoading(true);
        const [g, p, a] = await Promise.all([
          adminGroupsService.getAll().then(list => list.find(x => x.id === id)), // Temporary until we have getById
          adminAiProvidersService.getAll(),
          adminGroupsService.getAiAccess(id)
        ]);

        setGroup(g || null);
        setProviders(p);
        setAccess(a);

        // Fetch models for all providers (in parallel could be heavy, but okay for Admin)
        const modelsMap: Record<string, AiModel[]> = {};
        await Promise.all(p.map(async (prov) => {
            if (prov.isEnabled) { // Only fetch enabled providers' models
                 modelsMap[prov.id] = await adminAiProvidersService.getModels(prov.id);
            }
        }));
        setModelsByProvider(modelsMap);

      } catch (error) {
        toast.error('Failed to load group details.');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [id]);

  const handleSave = async () => {
    if (!id) return;
    try {
        setSaving(true);
        await adminGroupsService.updateAiAccess(id, {
            allowedProviderIds: access.allowedProviderIds,
            allowedModelIds: access.allowedModelIds
        });
        toast.success('Permissions updated.');
    } catch (error) {
        toast.error('Failed to update permissions.');
    } finally {
        setSaving(false);
    }
  };

  const toggleProvider = (providerId: string, checked: boolean) => {
    let newProviders = [...access.allowedProviderIds];
    let newModels = [...access.allowedModelIds];
    const providerModels = modelsByProvider[providerId] || [];

    if (checked) {
        newProviders.push(providerId);
        // Auto-select all models for convenience? Or let user choose?
        // Let's auto-select all to ensure functionality "Works" by default
        const modelIds = providerModels.map(m => m.id);
        modelIds.forEach(id => {
            if (!newModels.includes(id)) newModels.push(id);
        });
    } else {
        newProviders = newProviders.filter(id => id !== providerId);
        // Remove all models of this provider
        const modelIds = providerModels.map(m => m.id);
        newModels = newModels.filter(id => !modelIds.includes(id));
    }

    setAccess({ ...access, allowedProviderIds: newProviders, allowedModelIds: newModels });
  };

  const toggleModel = (modelId: string, checked: boolean) => {
      let newModels = [...access.allowedModelIds];
      if (checked) {
          newModels.push(modelId);
      } else {
          newModels = newModels.filter(id => id !== modelId);
      }
      setAccess({ ...access, allowedModelIds: newModels });
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!group || !id) return <div>Group not found</div>;

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex justify-between items-center">
        <div className="flex items-center gap-4">
             <Button variant="ghost" asChild>
                <Link href="/admin/groups">
                    <ArrowLeft className="h-4 w-4 mr-2" /> Back
                </Link>
            </Button>
            <div>
            <h1 className="text-3xl font-bold tracking-tight">{group.name} Access</h1>
            <p className="text-muted-foreground">Configure AI access for this group.</p>
            </div>
        </div>
        <Button onClick={handleSave} disabled={saving}>
          {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          <Save className="mr-2 h-4 w-4" /> Save Changes
        </Button>
      </div>

      <div className="grid gap-6">
        <Card>
            <CardHeader><CardTitle className="flex items-center gap-2"><Bot className="h-5 w-5"/> AI Permissions</CardTitle></CardHeader>
            <CardContent>
                <div className="space-y-6">
                    {providers.map(provider => (
                        <div key={provider.id} className="border rounded-lg p-4">
                            <div className="flex items-center space-x-2 mb-4">
                                <Checkbox
                                    id={`prov-${provider.id}`}
                                    checked={access.allowedProviderIds.includes(provider.id)}
                                    onCheckedChange={(c) => toggleProvider(provider.id, c === true)}
                                />
                                <Label htmlFor={`prov-${provider.id}`} className="text-lg font-medium cursor-pointer">
                                    {provider.name}
                                </Label>
                                {!provider.isEnabled && <Badge variant="secondary">Disabled Globally</Badge>}
                            </div>

                            {access.allowedProviderIds.includes(provider.id) && (
                                <div className="pl-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                                    {(modelsByProvider[provider.id] || []).map(model => (
                                        <div key={model.id} className="flex items-center space-x-2">
                                            <Checkbox
                                                id={`mod-${model.id}`}
                                                checked={access.allowedModelIds.includes(model.id)}
                                                onCheckedChange={(c) => toggleModel(model.id, c === true)}
                                            />
                                             <Label htmlFor={`mod-${model.id}`} className="text-sm cursor-pointer">
                                                {model.displayName} <span className="text-xs text-muted-foreground">({model.modelKey})</span>
                                            </Label>
                                        </div>
                                    ))}
                                    {(modelsByProvider[provider.id] || []).length === 0 && (
                                        <p className="text-sm text-muted-foreground italic">No models found for {provider.name}</p>
                                    )}
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            </CardContent>
        </Card>
      </div>
    </div>
  );
}

export default function UserGroupDetailsPage() {
    return (
        <Suspense fallback={<div className="flex justify-center p-10"><Loader2 className="animate-spin text-primary" /></div>}>
            <EditGroupContent />
        </Suspense>
    );
}
