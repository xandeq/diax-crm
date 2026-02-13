'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { adminAiProvidersService, CreateAiProviderRequest } from '@/services/adminAiProviders';
import { Plus, Loader2 } from 'lucide-react';
import { toast } from 'sonner';

interface CreateProviderDialogProps {
  onCreated?: () => void;
}

export function CreateProviderDialog({ onCreated }: CreateProviderDialogProps) {
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formData, setFormData] = useState<CreateAiProviderRequest>({
    key: '',
    name: '',
    supportsListModels: false,
    baseUrl: '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validation
    if (!formData.key.trim()) {
      toast.error('Provider key is required');
      return;
    }

    if (!formData.name.trim()) {
      toast.error('Provider name is required');
      return;
    }

    // Key validation: lowercase, alphanumeric + hyphens only
    const keyRegex = /^[a-z0-9-]+$/;
    if (!keyRegex.test(formData.key)) {
      toast.error('Provider key must be lowercase, alphanumeric and hyphens only (e.g., "my-custom-ai")');
      return;
    }

    try {
      setSaving(true);

      // Prepare data - remove baseUrl if empty
      const dataToSubmit: CreateAiProviderRequest = {
        key: formData.key.trim().toLowerCase(),
        name: formData.name.trim(),
        supportsListModels: formData.supportsListModels,
      };

      // Only include baseUrl if it's not empty
      if (formData.baseUrl?.trim()) {
        dataToSubmit.baseUrl = formData.baseUrl.trim();
      }

      await adminAiProvidersService.create(dataToSubmit);

      toast.success(`Provider "${formData.name}" created successfully`);

      // Reset form
      setFormData({
        key: '',
        name: '',
        supportsListModels: false,
        baseUrl: '',
      });

      // Close dialog
      setOpen(false);

      // Notify parent
      onCreated?.();
    } catch (error: any) {
      console.error('Error creating provider:', error);

      // Check for duplicate key error
      if (error?.response?.status === 409 || error?.response?.data?.message?.includes('already exists')) {
        toast.error(`Provider with key "${formData.key}" already exists`);
      } else {
        toast.error(error?.response?.data?.message || 'Failed to create provider');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setFormData({
      key: '',
      name: '',
      supportsListModels: false,
      baseUrl: '',
    });
    setOpen(false);
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>
          <Plus className="h-4 w-4 mr-2" />
          New Provider
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[500px]">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Create New AI Provider</DialogTitle>
            <DialogDescription>
              Add a new AI provider to the system. You can configure API keys and models after creation.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="key" className="required">
                Provider Key
              </Label>
              <Input
                id="key"
                placeholder="e.g., my-custom-ai"
                value={formData.key}
                onChange={(e) => setFormData({ ...formData, key: e.target.value.toLowerCase() })}
                disabled={saving}
                required
              />
              <p className="text-xs text-muted-foreground">
                Unique identifier (lowercase, alphanumeric + hyphens). Cannot be changed later.
              </p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="name" className="required">
                Display Name
              </Label>
              <Input
                id="name"
                placeholder="e.g., My Custom AI"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                disabled={saving}
                required
              />
              <p className="text-xs text-muted-foreground">
                Human-readable name shown in the UI.
              </p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="baseUrl">
                Base URL (Optional)
              </Label>
              <Input
                id="baseUrl"
                placeholder="e.g., https://api.example.com/v1"
                value={formData.baseUrl}
                onChange={(e) => setFormData({ ...formData, baseUrl: e.target.value })}
                disabled={saving}
              />
              <p className="text-xs text-muted-foreground">
                Custom API endpoint. Leave empty to use provider's default.
              </p>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox
                id="supportsListModels"
                checked={formData.supportsListModels}
                onCheckedChange={(checked) =>
                  setFormData({ ...formData, supportsListModels: checked as boolean })
                }
                disabled={saving}
              />
              <Label
                htmlFor="supportsListModels"
                className="text-sm font-normal cursor-pointer"
              >
                Supports model discovery via API
              </Label>
            </div>
            <p className="text-xs text-muted-foreground -mt-2 ml-6">
              Enable if this provider supports listing available models through their API (like OpenAI, Gemini).
            </p>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={handleCancel}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Creating...
                </>
              ) : (
                'Create Provider'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
