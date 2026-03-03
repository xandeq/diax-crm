'use client';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { agendaService } from '@/services/agenda';
import { CreateAppointmentDto } from '@/types/agenda';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { AlertCircle, CheckCircle2, Loader2, Wand2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

interface ImportTextDialogProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

const TYPE_LABELS: Record<string, string> = {
  Medical: 'Médico',
  HomeService: 'Serviço',
  Payment: 'Pagamento',
  Other: 'Outro'
};

export function ImportTextDialog({ isOpen, onOpenChange, onSuccess }: ImportTextDialogProps) {
  const [text, setText] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [parsedItems, setParsedItems] = useState<CreateAppointmentDto[] | null>(null);

  const handleParse = async () => {
    if (!text.trim()) return;

    setIsLoading(true);
    setParsedItems(null);
    try {
      const result = await agendaService.importFromText(text);
      setParsedItems(result);
      toast.success(`${result.length} compromissos encontrados!`);
    } catch (error) {
      console.error('Failed to parse text:', error);
      toast.error('Erro ao interpretar o texto. Verifique se há algum problema.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaveAll = async () => {
    if (!parsedItems || parsedItems.length === 0) return;

    setIsSaving(true);
    try {
      let successCount = 0;
      for (const item of parsedItems) {
        try {
          await agendaService.create(item);
          successCount++;
        } catch (e) {
          console.error('Error saving item:', item, e);
        }
      }

      toast.success(`${successCount} compromissos salvos com sucesso!`);
      setText('');
      setParsedItems(null);
      onOpenChange(false);
      onSuccess();
    } catch (error) {
      console.error('Failed to save some appointments:', error);
      toast.error('Ocorreu um erro ao salvar alguns compromissos.');
    } finally {
      setIsSaving(false);
    }
  };

  const resetState = () => {
    setText('');
    setParsedItems(null);
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => {
      onOpenChange(open);
      if (!open) resetState();
    }}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Wand2 className="w-5 h-5 text-purple-600" />
            Importação Mágica via IA
          </DialogTitle>
          <DialogDescription>
            Cole o texto (ex: mensagens do WhatsApp) contendo as datas, horários e títulos dos compromissos. A inteligência artificial irá extrair e estruturar tudo automaticamente.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 pt-4">
          {!parsedItems ? (
            <>
              <Textarea
                placeholder="Exemplo:&#10;Dia 02/3 (segunda-feira) comprar óculos&#10;Dia 03/03 10h : Aromas essência&#10;Diq 04/03 10h : aplicação ferrosa (R$ 160)"
                value={text}
                onChange={(e) => setText(e.target.value)}
                className="min-h-[200px] resize-none"
              />
              <div className="flex justify-end">
                <Button
                  onClick={handleParse}
                  disabled={isLoading || !text.trim()}
                  className="bg-purple-600 hover:bg-purple-700 w-full sm:w-auto"
                >
                  {isLoading ? (
                    <><Loader2 className="w-4 h-4 mr-2 animate-spin" /> Analisando texto com IA...</>
                  ) : (
                    <><Wand2 className="w-4 h-4 mr-2" /> Extrair Compromissos</>
                  )}
                </Button>
              </div>
            </>
          ) : (
            <div className="space-y-4">
              <div className="bg-slate-50 border border-slate-200 rounded-lg p-4">
                <h3 className="font-medium text-slate-800 mb-3 flex items-center gap-2">
                  <CheckCircle2 className="w-5 h-5 text-green-600" />
                  {parsedItems.length} compromissos reconhecidos
                </h3>

                <div className="space-y-3 max-h-[300px] overflow-y-auto pr-2">
                  {parsedItems.map((item, idx) => (
                    <div key={idx} className="bg-white p-3 border border-slate-200 rounded-md shadow-sm">
                      <div className="flex justify-between items-start mb-1">
                        <span className="font-semibold text-sm text-slate-800">{item.title}</span>
                        <span className="text-xs bg-slate-100 px-2 py-0.5 rounded-full border">{TYPE_LABELS[item.type] || item.type}</span>
                      </div>
                      <div className="text-xs text-slate-600 flex items-center gap-2">
                        <span className="font-medium">Data:</span>
                        {item.date ? format(parseISO(item.date), "dd/MM/yyyy 'às' HH:mm", { locale: ptBR }) : 'Data Inválida'}
                      </div>
                      {item.description && (
                        <div className="text-xs text-slate-500 mt-1 italic">
                          Obs: {item.description}
                        </div>
                      )}
                    </div>
                  ))}

                  {parsedItems.length === 0 && (
                    <div className="text-sm text-slate-500 flex items-center gap-2 py-4 justify-center">
                      <AlertCircle className="w-5 h-5 text-orange-500" />
                      Não foi possível encontrar nenhum compromisso claro.
                    </div>
                  )}
                </div>
              </div>

              <div className="flex flex-col sm:flex-row gap-3 justify-end pt-2">
                <Button variant="outline" onClick={() => setParsedItems(null)} disabled={isSaving}>
                  Tentar Novamente
                </Button>
                {parsedItems.length > 0 && (
                  <Button onClick={handleSaveAll} disabled={isSaving} className="bg-green-600 hover:bg-green-700">
                    {isSaving ? (
                      <><Loader2 className="w-4 h-4 mr-2 animate-spin" /> Salvando na Agenda...</>
                    ) : (
                      'Salvar Todos na Agenda'
                    )}
                  </Button>
                )}
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
