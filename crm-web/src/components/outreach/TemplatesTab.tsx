'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { Textarea } from '@/components/ui/textarea';
import { Flame, Loader2, MessageSquare, Snowflake, Thermometer } from 'lucide-react';
import { useEffect, useState } from 'react';

import { OutreachConfigResponse, UpdateOutreachConfigRequest } from '@/services/outreach';

// ─── Templates Tab ────────────────────────────────────────────────────────────

export interface TemplatesTabProps {
  config: OutreachConfigResponse | null;
  loading: boolean;
  onSave: (data: UpdateOutreachConfigRequest) => void;
  saving: boolean;
}

const PLACEHOLDERS = ['{{nome}}', '{{empresa}}', '{{email}}', '{{website}}'];

interface TemplateEditorProps {
  title: string;
  colorClass: string;
  icon: React.ReactNode;
  subject: string;
  body: string;
  onSubjectChange: (v: string) => void;
  onBodyChange: (v: string) => void;
}

function TemplateEditor({
  title,
  colorClass,
  icon,
  subject,
  body,
  onSubjectChange,
  onBodyChange,
}: TemplateEditorProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className={`text-base flex items-center gap-2 ${colorClass}`}>
          {icon}
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label>Assunto</Label>
          <Input
            value={subject}
            onChange={(e) => onSubjectChange(e.target.value)}
            placeholder={`Assunto do email para lead ${title.toLowerCase()}...`}
          />
        </div>
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label>Corpo do Email (HTML)</Label>
            <div className="flex flex-wrap gap-1">
              {PLACEHOLDERS.map((p) => (
                <code
                  key={p}
                  className="text-xs bg-slate-100 text-slate-600 px-1.5 py-0.5 rounded font-mono cursor-help"
                  title="Variável disponível"
                >
                  {p}
                </code>
              ))}
            </div>
          </div>
          <Textarea
            value={body}
            onChange={(e) => onBodyChange(e.target.value)}
            placeholder="<p>Olá {{nome}}, ...</p>"
            className="min-h-[180px] font-mono text-sm"
          />
        </div>
      </CardContent>
    </Card>
  );
}

interface WhatsAppTemplateEditorProps {
  title: string;
  colorClass: string;
  icon: React.ReactNode;
  body: string;
  onBodyChange: (v: string) => void;
}

function WhatsAppTemplateEditor({
  title,
  colorClass,
  icon,
  body,
  onBodyChange,
}: WhatsAppTemplateEditorProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className={`text-base flex items-center gap-2 ${colorClass}`}>
          {icon}
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label>Mensagem WhatsApp</Label>
            <div className="flex flex-wrap gap-1">
              {PLACEHOLDERS.map((p) => (
                <code
                  key={p}
                  className="text-xs bg-slate-100 text-slate-600 px-1.5 py-0.5 rounded font-mono cursor-help"
                  title="Variável disponível"
                >
                  {p}
                </code>
              ))}
            </div>
          </div>
          <Textarea
            value={body}
            onChange={(e) => onBodyChange(e.target.value)}
            placeholder="Olá {{nome}}, ..."
            className="min-h-[150px] text-sm"
          />
        </div>
      </CardContent>
    </Card>
  );
}

export function TemplatesTab({ config, loading, onSave, saving }: TemplatesTabProps) {
  const [hotSubject, setHotSubject] = useState('');
  const [hotBody, setHotBody] = useState('');
  const [warmSubject, setWarmSubject] = useState('');
  const [warmBody, setWarmBody] = useState('');
  const [coldSubject, setColdSubject] = useState('');
  const [coldBody, setColdBody] = useState('');
  const [whatsAppHotTemplate, setWhatsAppHotTemplate] = useState('');
  const [whatsAppWarmTemplate, setWhatsAppWarmTemplate] = useState('');
  const [whatsAppColdTemplate, setWhatsAppColdTemplate] = useState('');
  const [whatsAppFollowUpTemplate, setWhatsAppFollowUpTemplate] = useState('');

  useEffect(() => {
    if (!config) return;
    setHotSubject(config.hotTemplateSubject ?? '');
    setHotBody(config.hotTemplateBody ?? '');
    setWarmSubject(config.warmTemplateSubject ?? '');
    setWarmBody(config.warmTemplateBody ?? '');
    setColdSubject(config.coldTemplateSubject ?? '');
    setColdBody(config.coldTemplateBody ?? '');
    setWhatsAppHotTemplate(config.whatsAppHotTemplate ?? '');
    setWhatsAppWarmTemplate(config.whatsAppWarmTemplate ?? '');
    setWhatsAppColdTemplate(config.whatsAppColdTemplate ?? '');
    setWhatsAppFollowUpTemplate(config.whatsAppFollowUpTemplate ?? '');
  }, [config]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave({
      hotTemplateSubject: hotSubject || undefined,
      hotTemplateBody: hotBody || undefined,
      warmTemplateSubject: warmSubject || undefined,
      warmTemplateBody: warmBody || undefined,
      coldTemplateSubject: coldSubject || undefined,
      coldTemplateBody: coldBody || undefined,
      whatsAppHotTemplate: whatsAppHotTemplate || undefined,
      whatsAppWarmTemplate: whatsAppWarmTemplate || undefined,
      whatsAppColdTemplate: whatsAppColdTemplate || undefined,
      whatsAppFollowUpTemplate: whatsAppFollowUpTemplate || undefined,
    });
  };

  if (loading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-64 rounded-xl" />
        ))}
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Email Templates */}
      <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide">Templates de Email</h2>
      <TemplateEditor
        title="Quente"
        colorClass="text-red-600"
        icon={<Flame className="h-4 w-4" />}
        subject={hotSubject}
        body={hotBody}
        onSubjectChange={setHotSubject}
        onBodyChange={setHotBody}
      />
      <TemplateEditor
        title="Morno"
        colorClass="text-orange-600"
        icon={<Thermometer className="h-4 w-4" />}
        subject={warmSubject}
        body={warmBody}
        onSubjectChange={setWarmSubject}
        onBodyChange={setWarmBody}
      />
      <TemplateEditor
        title="Frio"
        colorClass="text-blue-600"
        icon={<Snowflake className="h-4 w-4" />}
        subject={coldSubject}
        body={coldBody}
        onSubjectChange={setColdSubject}
        onBodyChange={setColdBody}
      />

      {/* WhatsApp Templates */}
      <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide pt-4">Templates de WhatsApp</h2>
      <WhatsAppTemplateEditor
        title="WhatsApp Quente"
        colorClass="text-red-600"
        icon={<Flame className="h-4 w-4" />}
        body={whatsAppHotTemplate}
        onBodyChange={setWhatsAppHotTemplate}
      />
      <WhatsAppTemplateEditor
        title="WhatsApp Morno"
        colorClass="text-orange-600"
        icon={<Thermometer className="h-4 w-4" />}
        body={whatsAppWarmTemplate}
        onBodyChange={setWhatsAppWarmTemplate}
      />
      <WhatsAppTemplateEditor
        title="WhatsApp Frio"
        colorClass="text-blue-600"
        icon={<Snowflake className="h-4 w-4" />}
        body={whatsAppColdTemplate}
        onBodyChange={setWhatsAppColdTemplate}
      />
      <WhatsAppTemplateEditor
        title="WhatsApp Follow-Up"
        colorClass="text-purple-600"
        icon={<MessageSquare className="h-4 w-4" />}
        body={whatsAppFollowUpTemplate}
        onBodyChange={setWhatsAppFollowUpTemplate}
      />

      <div className="flex justify-end">
        <Button type="submit" disabled={saving}>
          {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {saving ? 'Salvando...' : 'Salvar Templates'}
        </Button>
      </div>
    </form>
  );
}
