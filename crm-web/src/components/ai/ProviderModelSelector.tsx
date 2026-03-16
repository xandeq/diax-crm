'use client';

import { AiModel, AiProvider } from '@/services/aiCatalog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Label } from '@/components/ui/label';

interface ProviderModelSelectorProps {
  providers: AiProvider[];
  selectedProvider: string;
  selectedModel: string;
  onProviderChange: (key: string) => void;
  onModelChange: (key: string) => void;
  disabled?: boolean;
  className?: string;
  hideLabels?: boolean;
}

/**
 * Reusable component for selecting an AI provider and model.
 * Handles auto-population of available models based on selected provider.
 */
export function ProviderModelSelector({
  providers,
  selectedProvider,
  selectedModel,
  onProviderChange,
  onModelChange,
  disabled = false,
  className = '',
  hideLabels = false,
}: ProviderModelSelectorProps) {
  const currentProvider = providers.find(p => p.key === selectedProvider);
  const availableModels = currentProvider?.models?.filter(m => m.isEnabled) ?? [];

  return (
    <div className={`flex gap-4 ${className}`}>
      {/* Provider Selection */}
      <div className="flex-1 space-y-2">
        {!hideLabels && <Label htmlFor="provider-select">Provider</Label>}
        <Select value={selectedProvider} onValueChange={onProviderChange} disabled={disabled}>
          <SelectTrigger id="provider-select">
            <SelectValue placeholder="Select a provider..." />
          </SelectTrigger>
          <SelectContent>
            {providers.map(provider => (
              <SelectItem key={provider.id} value={provider.key}>
                {provider.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Model Selection */}
      <div className="flex-1 space-y-2">
        {!hideLabels && <Label htmlFor="model-select">Model</Label>}
        <Select value={selectedModel} onValueChange={onModelChange} disabled={disabled || availableModels.length === 0}>
          <SelectTrigger id="model-select">
            <SelectValue placeholder={availableModels.length === 0 ? 'No models available' : 'Select a model...'} />
          </SelectTrigger>
          <SelectContent>
            {availableModels.map(model => (
              <SelectItem key={model.id} value={model.modelKey}>
                {model.displayName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </div>
  );
}
