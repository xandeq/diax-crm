'use client';

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { adminAiProvidersService, CredentialStatusDto, TestConnectionResultDto } from '@/services/adminAiProviders';
import { Eye, EyeOff, Key, Loader2, CheckCircle2, XCircle, AlertCircle } from 'lucide-react';
import { toast } from 'sonner';

interface ApiKeyConfigFormProps {
  providerId: string;
  providerName: string;
  onSaved?: () => void;
}

export function ApiKeyConfigForm({ providerId, providerName, onSaved }: ApiKeyConfigFormProps) {
  const [apiKey, setApiKey] = useState('');
  const [status, setStatus] = useState<CredentialStatusDto | null>(null);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<TestConnectionResultDto | null>(null);
  const [showApiKey, setShowApiKey] = useState(false);
  const [loading, setLoading] = useState(true);

  // Load credential status on mount
  useEffect(() => {
    loadCredentialStatus();
  }, [providerId]);

  const loadCredentialStatus = async () => {
    try {
      setLoading(true);
      const credentialStatus = await adminAiProvidersService.getCredentialStatus(providerId);
      setStatus(credentialStatus);
    } catch (error) {
      console.error('Error loading credential status:', error);
      toast.error('Failed to load API key status');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveApiKey = async () => {
    // Validation
    if (!apiKey.trim()) {
      toast.error('API key cannot be empty');
      return;
    }

    if (apiKey.trim().length < 10) {
      toast.error('API key must be at least 10 characters long');
      return;
    }

    // Confirm if overwriting existing key
    if (status?.isConfigured) {
      const confirmed = window.confirm(
        `This will overwrite the existing API key for ${providerName}. Continue?`
      );
      if (!confirmed) return;
    }

    try {
      setSaving(true);
      setTestResult(null); // Clear previous test result

      await adminAiProvidersService.saveApiKey(providerId, apiKey);

      toast.success('API key saved successfully');

      // Reload status to show updated last 4 digits
      await loadCredentialStatus();

      // Clear input
      setApiKey('');
      setShowApiKey(false);

      // Notify parent
      onSaved?.();
    } catch (error: any) {
      console.error('Error saving API key:', error);
      toast.error(error?.response?.data?.message || 'Failed to save API key');
    } finally {
      setSaving(false);
    }
  };

  const handleTestConnection = async () => {
    if (!status?.isConfigured) {
      toast.error('Please configure an API key first');
      return;
    }

    try {
      setTesting(true);
      const result = await adminAiProvidersService.testConnection(providerId);
      setTestResult(result);

      if (result.success) {
        toast.success('Connection test successful');
      } else {
        toast.error('Connection test failed');
      }
    } catch (error: any) {
      console.error('Error testing connection:', error);
      const errorResult: TestConnectionResultDto = {
        success: false,
        message: 'Connection test failed',
        errorDetails: error?.response?.data?.message || error?.message || 'Unknown error'
      };
      setTestResult(errorResult);
      toast.error('Connection test failed');
    } finally {
      setTesting(false);
    }
  };

  const handleClear = () => {
    setApiKey('');
    setShowApiKey(false);
    setTestResult(null);
  };

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span>Loading API key status...</span>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Status Badge */}
      <div className="flex items-center gap-2">
        <Label>Status:</Label>
        {status?.isConfigured ? (
          <Badge variant="default" className="bg-green-600">
            <CheckCircle2 className="h-3 w-3 mr-1" />
            Configured {status.lastFourDigits && `(••••${status.lastFourDigits})`}
          </Badge>
        ) : (
          <Badge variant="secondary" className="bg-yellow-600">
            <AlertCircle className="h-3 w-3 mr-1" />
            Not Configured
          </Badge>
        )}
      </div>

      {/* API Key Input */}
      <div className="space-y-2">
        <Label htmlFor="apiKey">
          API Key for {providerName}
        </Label>
        <div className="relative">
          <Input
            id="apiKey"
            type={showApiKey ? 'text' : 'password'}
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder={`Enter API Key for ${providerName}`}
            className="pr-10"
            disabled={saving}
          />
          <button
            type="button"
            onClick={() => setShowApiKey(!showApiKey)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            disabled={saving}
          >
            {showApiKey ? (
              <EyeOff className="h-4 w-4" />
            ) : (
              <Eye className="h-4 w-4" />
            )}
          </button>
        </div>
        <p className="text-xs text-muted-foreground">
          API keys are encrypted and stored securely. Minimum 10 characters required.
        </p>
      </div>

      {/* Action Buttons */}
      <div className="flex gap-2">
        <Button
          onClick={handleSaveApiKey}
          disabled={!apiKey.trim() || saving || testing}
        >
          {saving ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Saving...
            </>
          ) : (
            <>
              <Key className="h-4 w-4 mr-2" />
              Save API Key
            </>
          )}
        </Button>

        <Button
          variant="secondary"
          onClick={handleTestConnection}
          disabled={!status?.isConfigured || saving || testing}
        >
          {testing ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Testing...
            </>
          ) : (
            'Test Connection'
          )}
        </Button>

        {apiKey.trim() && (
          <Button
            variant="ghost"
            onClick={handleClear}
            disabled={saving || testing}
          >
            Clear
          </Button>
        )}
      </div>

      {/* Test Result */}
      {testResult && (
        <Alert variant={testResult.success ? 'default' : 'destructive'}>
          <div className="flex items-start gap-2">
            {testResult.success ? (
              <CheckCircle2 className="h-4 w-4 mt-0.5 text-green-600" />
            ) : (
              <XCircle className="h-4 w-4 mt-0.5" />
            )}
            <div className="flex-1">
              <AlertDescription>
                <p className="font-medium">{testResult.message}</p>
                {testResult.errorDetails && (
                  <p className="text-sm mt-1 opacity-90">{testResult.errorDetails}</p>
                )}
              </AlertDescription>
            </div>
          </div>
        </Alert>
      )}
    </div>
  );
}
