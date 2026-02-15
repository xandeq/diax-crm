'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { apiFetch } from '@/services/api';
import { ArrowLeft, Loader2, Upload } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

interface ImportCustomerRow {
  name: string;
  email: string;
  phone?: string;
  whatsApp?: string;
  companyName?: string;
  notes?: string;
  tags?: string;
}

interface ImportError {
  rowNumber: number;
  email: string;
  errorMessage: string;
}

interface BulkImportResponse {
  success: boolean;
  totalRecords: number;
  successCount: number;
  failedCount: number;
  errors: ImportError[];
}

export default function LeadsImportPage() {
  const router = useRouter();
  const [importMode, setImportMode] = useState<'csv' | 'text'>('csv');
  const [file, setFile] = useState<File | null>(null);
  const [textData, setTextData] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<BulkImportResponse | null>(null);

  const parseCSV = (text: string): ImportCustomerRow[] => {
    const lines = text.split('\n').filter(l => l.trim());

    // Remove header if exists
    const hasHeader = lines[0]?.toLowerCase().includes('nome') || lines[0]?.toLowerCase().includes('name') || lines[0]?.toLowerCase().includes('email');
    const dataLines = hasHeader ? lines.slice(1) : lines;

    return dataLines.map(line => {
      // Handle both comma and semicolon separators
      const separator = line.includes(';') ? ';' : ',';
      const parts = line.split(separator).map(p => p.trim().replace(/^["']|["']$/g, ''));

      const [name = '', email = '', phone = '', whatsApp = '', companyName = '', notes = '', tags = ''] = parts;

      return {
        name,
        email,
        phone: phone || undefined,
        whatsApp: whatsApp || phone || undefined,
        companyName: companyName || undefined,
        notes: notes || undefined,
        tags: tags || undefined,
      };
    });
  };

  const parseText = (text: string): ImportCustomerRow[] => {
    const lines = text.split('\n').filter(l => l.trim());

    return lines.map(line => {
      // Try to extract email
      const emailMatch = line.match(/[\w.+-]+@[\w.-]+\.\w+/);

      // Try to extract phone (Brazilian format or international)
      const phoneMatch = line.match(/\+?5?5?\s?\(?[0-9]{2}\)?\s?[0-9]{4,5}-?[0-9]{4}|\+?[0-9]{10,15}/);

      // Name is whatever comes before the email or the whole line
      let name = line;
      if (emailMatch) {
        name = line.substring(0, line.indexOf(emailMatch[0])).trim();
        // Remove common separators
        name = name.replace(/[<>,;-]\s*$/, '').trim();
      }

      return {
        name: name || 'Nome não especificado',
        email: emailMatch?.[0] || '',
        phone: phoneMatch?.[0],
        whatsApp: phoneMatch?.[0],
      };
    });
  };

  const handleImport = async () => {
    setLoading(true);
    setResult(null);

    try {
      let customers: ImportCustomerRow[] = [];

      if (importMode === 'csv' && file) {
        const text = await file.text();
        customers = parseCSV(text);
      } else if (importMode === 'text' && textData) {
        customers = parseText(textData);
      }

      if (customers.length === 0) {
        alert('Nenhum contato válido encontrado para importar.');
        return;
      }

      const response = await apiFetch<BulkImportResponse>('/customers/import', {
        method: 'POST',
        body: JSON.stringify({ customers }),
      });

      setResult(response);

      if (response.successCount > 0) {
        setTimeout(() => {
          router.push('/leads');
        }, 3000);
      }
    } catch (error: any) {
      alert(`Erro ao importar: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mx-auto py-6 max-w-4xl">
      <div className="mb-6">
        <Link href="/leads">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Voltar para Leads
          </Button>
        </Link>
      </div>

      <div className="mb-6">
        <h1 className="text-3xl font-bold tracking-tight">Importar Leads</h1>
        <p className="text-muted-foreground mt-2">
          Importe múltiplos leads de uma vez usando arquivo CSV ou texto.
        </p>
      </div>

      <div className="grid gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Selecione o método de importação</CardTitle>
            <CardDescription>
              Escolha entre fazer upload de um arquivo CSV ou colar contatos em formato de texto.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex gap-4">
              <Button
                variant={importMode === 'csv' ? 'default' : 'outline'}
                onClick={() => setImportMode('csv')}
                className="flex-1"
              >
                <Upload className="h-4 w-4 mr-2" />
                Arquivo CSV
              </Button>
              <Button
                variant={importMode === 'text' ? 'default' : 'outline'}
                onClick={() => setImportMode('text')}
                className="flex-1"
              >
                Colar Texto
              </Button>
            </div>

            {importMode === 'csv' && (
              <div className="space-y-2">
                <Label htmlFor="file">Arquivo CSV</Label>
                <Input
                  id="file"
                  type="file"
                  accept=".csv,.txt"
                  onChange={(e) => setFile(e.target.files?.[0] || null)}
                />
                <p className="text-sm text-muted-foreground">
                  Formato esperado: Nome,Email,Telefone,WhatsApp,Empresa,Observações,Tags
                </p>
                <p className="text-sm text-muted-foreground">
                  Exemplo: João Silva,joao@email.com,11999999999,11999999999,Empresa XYZ,Cliente em potencial,vip;premium
                </p>
              </div>
            )}

            {importMode === 'text' && (
              <div className="space-y-2">
                <Label htmlFor="text">Cole os contatos</Label>
                <Textarea
                  id="text"
                  placeholder="João Silva <joao@email.com> (11) 99999-9999&#10;Maria Santos maria@email.com +5511988888888&#10;Pedro Oliveira, pedro@email.com, 11977777777"
                  rows={12}
                  value={textData}
                  onChange={(e) => setTextData(e.target.value)}
                  className="font-mono text-sm"
                />
                <p className="text-sm text-muted-foreground">
                  Cole uma lista de contatos. O sistema detectará automaticamente nome, email e telefone.
                </p>
                <p className="text-sm text-muted-foreground">
                  Formatos aceitos: "Nome &lt;email&gt; telefone", "Nome, email, telefone", etc.
                </p>
              </div>
            )}

            <Button
              onClick={handleImport}
              disabled={loading || (importMode === 'csv' ? !file : !textData)}
              className="w-full"
              size="lg"
            >
              {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {loading ? 'Importando...' : 'Importar Leads'}
            </Button>
          </CardContent>
        </Card>

        {result && (
          <Card className={result.failedCount === 0 ? 'border-green-200 bg-green-50' : 'border-yellow-200 bg-yellow-50'}>
            <CardHeader>
              <CardTitle>Resultado da Importação</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-3 gap-4 text-center">
                <div>
                  <div className="text-2xl font-bold">{result.totalRecords}</div>
                  <div className="text-sm text-muted-foreground">Total</div>
                </div>
                <div>
                  <div className="text-2xl font-bold text-green-600">{result.successCount}</div>
                  <div className="text-sm text-muted-foreground">Sucesso</div>
                </div>
                <div>
                  <div className="text-2xl font-bold text-red-600">{result.failedCount}</div>
                  <div className="text-sm text-muted-foreground">Erros</div>
                </div>
              </div>

              {result.failedCount > 0 && (
                <div className="space-y-2">
                  <h4 className="font-semibold text-sm">Erros encontrados:</h4>
                  <div className="max-h-60 overflow-y-auto space-y-1">
                    {result.errors.map((error, index) => (
                      <div key={index} className="text-sm bg-white p-2 rounded border">
                        <span className="font-medium">Linha {error.rowNumber}:</span>{' '}
                        <span className="text-muted-foreground">{error.email || 'sem email'}</span> -{' '}
                        <span className="text-red-600">{error.errorMessage}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {result.successCount > 0 && (
                <p className="text-sm text-green-700 font-medium">
                  ✅ {result.successCount} lead(s) importado(s) com sucesso! Redirecionando...
                </p>
              )}
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
