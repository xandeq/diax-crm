'use client';

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { adminAiProvidersService, UpdateAiProviderRequest } from '@/services/adminAiProviders';
import { AiProvider } from '@/services/aiCatalog';
import { Save, Loader2 } from 'lucide-react';
import { toast } from 'sonner';

interface EditProviderSettingsFormProps {
  provider: AiProvider;
  onUpdated?: () => void;
}

export function EditProviderSettingsForm({ provider, onUpdated }: EditProviderSettingsFormProps) {
  const [saving, setSaving] = useState(false);
  const [formData, setFormData] = useState<UpdateAiProviderRequest>({
    name: provider.name,
    supportsListModels: provider.supportsListModels,
    baseUrl: provider.baseUrl || '',
    isEnabled: provider.isEnabled,
  });

  // Update form when provider prop changes
  useEffect(() => {
    setFormData({
      name: provider.name,
      supportsListModels: provider.supportsListModels,
      baseUrl: provider.baseUrl || '',
      isEnabled: provider.isEnabled,
    });
  }, [provider]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validation
    if (!formData.name.trim()) {
      toast.error('Provider name is required');
      return;
    }

    try {
      setSaving(true);

      // Prepare data - convert empty baseUrl to undefined
      const dataToSubmit: UpdateAiProviderRequest = {
        name: formData.name.trim(),
        supportsListModels: formData.supportsListModels,
        baseUrl: formData.baseUrl?.trim() || undefined,
        isEnabled: formData.isEnabled,
      };

      await adminAiProvidersService.update(provider.id, dataToSubmit);

      toast.success('Provider settings updated successfully');

      // Notify parent
      onUpdated?.();
    } catch (error: any) {
      console.error('Error updating provider:', error);
      toast.error(error?.response?.data?.message || 'Failed to update provider settings');
    } finally {
      setSaving(false);
    }
  };

  const hasChanges =
    formData.name !== provider.name ||
    formData.supportsListModels !== provider.supportsListModels ||
    (formData.baseUrl || '') !== (provider.baseUrl || '') ||
    formData.isEnabled !== provider.isEnabled;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid gap-4">
        <div className="grid gap-2">
          <Label htmlFor="providerKey">
            Provider Key
          </Label>
          <Input
            id="providerKey"
            value={provider.key}
            disabled
            className="bg-muted"
          />
          <p className="text-xs text-muted-foreground">
            Provider key cannot be changed after creation.
          </p>
        </div>

        <div className="grid gap-2">
          <Label htmlFor="providerName" className="required">
            Display Name
          </Label>
          <Input
            id="providerName"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            disabled={saving}
            required
          />
        </div>

        <div className="grid gap-2">
          <Label htmlFor="providerBaseUrl">
            Base URL (Optional)
          </Label>
          <Input
            id="providerBaseUrl"
            placeholder="e.g., https://api.example.com/v1"
            value={formData.baseUrl}
            onChange={(e) => setFormData({ ...formData, baseUrl: e.target.value })}
            disabled={saving}
          />
          <p className="text-xs text-muted-foreground">
            Custom API endpoint. Leave empty to use provider's default.
          </p>
        </div>

        <div className="space-y-3">
          <div className="flex items-center space-x-2">
            <Checkbox
              id="providerSupportsListModels"
              checked={formData.supportsListModels}
              onCheckedChange={(checked) =>
                setFormData({ ...formData, supportsListModels: checked as boolean })
              }
              disabled={saving}
            />
            <Label
              htmlFor="providerSupportsListModels"
              className="text-sm font-normal cursor-pointer"
            >
              Supports model discovery via API
            </Label>
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox
              id="providerIsEnabled"
              checked={formData.isEnabled}
              onCheckedChange={(checked) =>
                setFormData({ ...formData, isEnabled: checked as boolean })
              }
              disabled={saving}
            />
            <Label
              htmlFor="providerIsEnabled"
              className="text-sm font-normal cursor-pointer"
            >
              Provider is enabled
            </Label>
          </div>
          <p className="text-xs text-muted-foreground ml-6">
            Disabled providers won't be available to users, even if they have permissions.
          </p>
        </div>
      </div>

      <div className="flex justify-end">
        <Button
          type="submit"
          disabled={!hasChanges || saving}
        >
          {saving ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Saving...
            </>
          ) : (
            <>
              <Save className="h-4 w-4 mr-2" />
              Save Changes
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
