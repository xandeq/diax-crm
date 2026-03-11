"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import {
  deleteTaxDocument,
  formatFileSize,
  getTaxDocumentDownloadUrl,
  getTaxDocumentFiscalYears,
  getTaxDocuments,
  TAX_DOCUMENT_TYPE_LABELS,
  TaxDocumentDto,
  uploadTaxDocument,
  updateTaxDocument,
} from "@/services/taxDocuments";
import { Download, Edit2, FileText, Loader2, Plus, RefreshCw, Trash2, Upload } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";

const INSTITUTION_TYPES = Object.entries(TAX_DOCUMENT_TYPE_LABELS).map(([value, label]) => ({
  value: Number(value),
  label,
}));

const CURRENT_YEAR = new Date().getFullYear();

// ─── Upload Dialog ─────────────────────────────────────────────────────────────

interface UploadDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

function UploadDialog({ open, onClose, onSuccess }: UploadDialogProps) {
  const [fiscalYear, setFiscalYear] = useState(String(CURRENT_YEAR - 1));
  const [institutionName, setInstitutionName] = useState("");
  const [institutionType, setInstitutionType] = useState("0");
  const [notes, setNotes] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const reset = () => {
    setFiscalYear(String(CURRENT_YEAR - 1));
    setInstitutionName("");
    setInstitutionType("0");
    setNotes("");
    setFile(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      toast.error("Selecione um arquivo para continuar.");
      return;
    }
    if (!institutionName.trim()) {
      toast.error("Informe o nome da instituição.");
      return;
    }

    const yearNum = parseInt(fiscalYear);
    if (isNaN(yearNum) || yearNum < 2000 || yearNum > CURRENT_YEAR) {
      toast.error(`Ano fiscal deve estar entre 2000 e ${CURRENT_YEAR}.`);
      return;
    }

    setIsLoading(true);
    try {
      const formData = new FormData();
      formData.append("file", file);
      formData.append("fiscalYear", String(yearNum));
      formData.append("institutionName", institutionName.trim());
      formData.append("institutionType", institutionType);
      if (notes.trim()) formData.append("notes", notes.trim());

      await uploadTaxDocument(formData);
      toast.success("Documento adicionado com sucesso.");
      reset();
      onSuccess();
    } catch (err: any) {
      toast.error(err.message || "Erro ao fazer upload do documento.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && handleClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Adicionar Documento</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="fiscal-year">Ano Fiscal</Label>
              <Input
                id="fiscal-year"
                type="number"
                min={2000}
                max={CURRENT_YEAR}
                value={fiscalYear}
                onChange={(e) => setFiscalYear(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="inst-type">Tipo de Instituição</Label>
              <Select value={institutionType} onValueChange={setInstitutionType}>
                <SelectTrigger id="inst-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {INSTITUTION_TYPES.map((t) => (
                    <SelectItem key={t.value} value={String(t.value)}>
                      {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="inst-name">Nome da Instituição</Label>
            <Input
              id="inst-name"
              placeholder="Ex: Banco do Brasil, Nubank, XP Investimentos…"
              value={institutionName}
              onChange={(e) => setInstitutionName(e.target.value)}
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="doc-file">
              Arquivo <span className="text-muted-foreground text-xs">(PDF, JPG, PNG, DOC — máx 20 MB)</span>
            </Label>
            <Input
              id="doc-file"
              ref={fileInputRef}
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,.doc,.docx"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="cursor-pointer"
              required
            />
            {file && (
              <p className="text-xs text-muted-foreground">
                {file.name} · {formatFileSize(file.size)}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="doc-notes">
              Observações <span className="text-muted-foreground text-xs">(opcional)</span>
            </Label>
            <Textarea
              id="doc-notes"
              placeholder="Informações adicionais sobre este documento…"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={2}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose} disabled={isLoading}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Enviando…
                </>
              ) : (
                <>
                  <Upload className="mr-2 h-4 w-4" />
                  Enviar
                </>
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Edit Dialog ───────────────────────────────────────────────────────────────

interface EditDialogProps {
  doc: TaxDocumentDto | null;
  onClose: () => void;
  onSuccess: () => void;
}

function EditDialog({ doc, onClose, onSuccess }: EditDialogProps) {
  const [fiscalYear, setFiscalYear] = useState("");
  const [institutionName, setInstitutionName] = useState("");
  const [institutionType, setInstitutionType] = useState("0");
  const [notes, setNotes] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (doc) {
      setFiscalYear(String(doc.fiscalYear));
      setInstitutionName(doc.institutionName);
      setInstitutionType(String(doc.institutionType));
      setNotes(doc.notes ?? "");
    }
  }, [doc]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!doc) return;

    const yearNum = parseInt(fiscalYear);
    if (isNaN(yearNum) || yearNum < 2000 || yearNum > CURRENT_YEAR) {
      toast.error(`Ano fiscal deve estar entre 2000 e ${CURRENT_YEAR}.`);
      return;
    }

    setIsLoading(true);
    try {
      await updateTaxDocument(doc.id, {
        institutionName: institutionName.trim(),
        institutionType: parseInt(institutionType),
        fiscalYear: yearNum,
        notes: notes.trim() || undefined,
      });
      toast.success("Documento atualizado.");
      onSuccess();
    } catch (err: any) {
      toast.error(err.message || "Erro ao atualizar documento.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={!!doc} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Editar Documento</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="edit-fiscal-year">Ano Fiscal</Label>
              <Input
                id="edit-fiscal-year"
                type="number"
                min={2000}
                max={CURRENT_YEAR}
                value={fiscalYear}
                onChange={(e) => setFiscalYear(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-inst-type">Tipo</Label>
              <Select value={institutionType} onValueChange={setInstitutionType}>
                <SelectTrigger id="edit-inst-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {INSTITUTION_TYPES.map((t) => (
                    <SelectItem key={t.value} value={String(t.value)}>
                      {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-inst-name">Nome da Instituição</Label>
            <Input
              id="edit-inst-name"
              value={institutionName}
              onChange={(e) => setInstitutionName(e.target.value)}
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-notes">Observações</Label>
            <Textarea
              id="edit-notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={2}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={isLoading}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              Salvar
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Main Page ─────────────────────────────────────────────────────────────────

export default function TaxDocumentsPage() {
  const [docs, setDocs] = useState<TaxDocumentDto[]>([]);
  const [fiscalYears, setFiscalYears] = useState<number[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Filters
  const [filterYear, setFilterYear] = useState<string>("all");
  const [filterType, setFilterType] = useState<string>("all");
  const [search, setSearch] = useState("");

  // Dialogs
  const [uploadOpen, setUploadOpen] = useState(false);
  const [editDoc, setEditDoc] = useState<TaxDocumentDto | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const [docsData, yearsData] = await Promise.all([
        getTaxDocuments({
          fiscalYear: filterYear !== "all" ? parseInt(filterYear) : undefined,
          institutionType: filterType !== "all" ? parseInt(filterType) : undefined,
          search: search.trim() || undefined,
        }),
        getTaxDocumentFiscalYears(),
      ]);
      setDocs(docsData);
      setFiscalYears(yearsData.sort((a, b) => b - a));
    } catch (err: any) {
      toast.error(err.message || "Erro ao carregar documentos.");
    } finally {
      setIsLoading(false);
    }
  }, [filterYear, filterType, search]);

  useEffect(() => {
    load();
  }, [load]);

  const handleDelete = async (doc: TaxDocumentDto) => {
    if (!confirm(`Excluir "${doc.fileName}" (${doc.institutionName} — ${doc.fiscalYear})?`)) return;
    setDeletingId(doc.id);
    try {
      await deleteTaxDocument(doc.id);
      toast.success("Documento excluído.");
      load();
    } catch (err: any) {
      toast.error(err.message || "Erro ao excluir documento.");
    } finally {
      setDeletingId(null);
    }
  };

  // Group docs by fiscal year for display
  const grouped = docs.reduce<Record<number, TaxDocumentDto[]>>((acc, d) => {
    (acc[d.fiscalYear] ??= []).push(d);
    return acc;
  }, {});
  const sortedYears = Object.keys(grouped)
    .map(Number)
    .sort((a, b) => b - a);

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" });

  return (
    <div className="container mx-auto py-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Imposto de Renda</h1>
          <p className="text-muted-foreground">
            Centralize informes de rendimentos e documentos fiscais por ano-calendário.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="icon" onClick={load} disabled={isLoading} title="Atualizar">
            <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
          </Button>
          <Button onClick={() => setUploadOpen(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Adicionar Documento
          </Button>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-4">
          <div className="flex flex-col sm:flex-row gap-3">
            <Select value={filterYear} onValueChange={(v) => setFilterYear(v)}>
              <SelectTrigger className="w-full sm:w-44">
                <SelectValue placeholder="Todos os anos" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos os anos</SelectItem>
                {fiscalYears.map((y) => (
                  <SelectItem key={y} value={String(y)}>
                    {y}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select value={filterType} onValueChange={(v) => setFilterType(v)}>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Todos os tipos" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos os tipos</SelectItem>
                {INSTITUTION_TYPES.map((t) => (
                  <SelectItem key={t.value} value={String(t.value)}>
                    {t.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Input
              placeholder="Buscar instituição…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1"
            />
          </div>
        </CardContent>
      </Card>

      {/* Content */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : docs.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-16 text-center">
            <FileText className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="text-lg font-medium text-muted-foreground">Nenhum documento encontrado</p>
            <p className="text-sm text-muted-foreground mt-1">
              Clique em "Adicionar Documento" para começar.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-6">
          {sortedYears.map((year) => (
            <Card key={year}>
              <CardHeader className="pb-3">
                <CardTitle className="text-lg">Ano-Calendário {year}</CardTitle>
                <CardDescription>
                  {grouped[year].length} documento{grouped[year].length !== 1 ? "s" : ""}
                </CardDescription>
              </CardHeader>
              <CardContent className="p-0">
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="text-left px-4 py-3 font-medium text-muted-foreground">Instituição</th>
                        <th className="text-left px-4 py-3 font-medium text-muted-foreground">Tipo</th>
                        <th className="text-left px-4 py-3 font-medium text-muted-foreground">Arquivo</th>
                        <th className="text-left px-4 py-3 font-medium text-muted-foreground">Tamanho</th>
                        <th className="text-left px-4 py-3 font-medium text-muted-foreground">Enviado em</th>
                        <th className="text-right px-4 py-3 font-medium text-muted-foreground">Ações</th>
                      </tr>
                    </thead>
                    <tbody>
                      {grouped[year].map((doc) => (
                        <tr key={doc.id} className="border-b last:border-0 hover:bg-muted/30 transition-colors">
                          <td className="px-4 py-3 font-medium">
                            {doc.institutionName}
                            {doc.notes && (
                              <p className="text-xs text-muted-foreground font-normal truncate max-w-[200px]">
                                {doc.notes}
                              </p>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            <Badge variant="secondary">{doc.institutionTypeName}</Badge>
                          </td>
                          <td className="px-4 py-3 max-w-[200px]">
                            <span className="truncate block" title={doc.fileName}>
                              {doc.fileName}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                            {formatFileSize(doc.fileSize)}
                          </td>
                          <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                            {formatDate(doc.uploadedAt)}
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex items-center justify-end gap-1">
                              <a
                                href={getTaxDocumentDownloadUrl(doc.id)}
                                download={doc.fileName}
                                title="Baixar"
                              >
                                <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                                  <span>
                                    <Download className="h-4 w-4" />
                                  </span>
                                </Button>
                              </a>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8"
                                title="Editar"
                                onClick={() => setEditDoc(doc)}
                              >
                                <Edit2 className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-destructive hover:text-destructive"
                                title="Excluir"
                                disabled={deletingId === doc.id}
                                onClick={() => handleDelete(doc)}
                              >
                                {deletingId === doc.id ? (
                                  <Loader2 className="h-4 w-4 animate-spin" />
                                ) : (
                                  <Trash2 className="h-4 w-4" />
                                )}
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <UploadDialog
        open={uploadOpen}
        onClose={() => setUploadOpen(false)}
        onSuccess={() => {
          setUploadOpen(false);
          load();
        }}
      />

      <EditDialog
        doc={editDoc}
        onClose={() => setEditDoc(null)}
        onSuccess={() => {
          setEditDoc(null);
          load();
        }}
      />
    </div>
  );
}
