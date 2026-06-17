'use client';

import { FinanceNav } from '@/components/finance/FinanceNav';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import { CardBrand, CardKind, financeService } from '@/services/finance';
import { ArrowLeft, Loader2, CreditCard as CreditCardIcon } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { motion } from 'framer-motion';
import { toast } from 'sonner';

export default function NewCreditCardPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    lastFourDigits: '',
    closingDay: 1,
    dueDay: 10,
    limit: 0,
    brand: CardBrand.Unknown,
    cardKind: CardKind.Physical
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    // Validation
    if (!/^\d{4}$/.test(formData.lastFourDigits)) {
      setError('Os últimos 4 dígitos devem conter exatamente 4 números.');
      setLoading(false);
      return;
    }

    try {
      await financeService.createCreditCard({
        ...formData,
        limit: Number(formData.limit),
        closingDay: Number(formData.closingDay),
        dueDay: Number(formData.dueDay),
        brand: Number(formData.brand),
        cardKind: Number(formData.cardKind)
      });
      toast.success('Cartão de crédito criado com sucesso');
      router.push('/finance/credit-cards');
    } catch (err) {
      setError('Erro ao criar cartão de crédito. Por favor, tente novamente.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mx-auto px-4 py-6 max-w-7xl">
      <FinanceNav />

      <div className="max-w-xl mx-auto space-y-6">
        <div className="flex items-center gap-3">
          <Button 
            variant="ghost" 
            size="icon" 
            onClick={() => router.back()} 
            className="rounded-full text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50"
          >
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
              <CreditCardIcon className="h-5 w-5 text-violet-400" />
              Novo Cartão de Crédito
            </h1>
            <p className="text-xs text-zinc-400">Cadastre seus cartões de crédito para gerenciar faturas</p>
          </div>
        </div>

        <motion.div 
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="rounded-2xl p-6 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-violet-500/5 to-transparent pointer-events-none" />

          {error && (
            <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-3 rounded-xl mb-4 text-xs font-semibold">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4 relative z-10">
            <div className="space-y-1.5">
              <Label htmlFor="name" className="text-zinc-300 font-medium text-xs">Nome do Cartão</Label>
              <Input
                id="name"
                required
                placeholder="Ex: Nubank Mastercard, Itaú Personalité, Visa Infinite..."
                className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm"
                value={formData.name}
                onChange={e => setFormData({...formData, name: e.target.value})}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="brand" className="text-zinc-300 font-medium text-xs">Bandeira</Label>
                <Select
                  onValueChange={(value) => setFormData({...formData, brand: Number(value)})}
                  value={String(formData.brand)}
                >
                  <SelectTrigger className="bg-zinc-950/80 border-zinc-800 text-zinc-100 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm">
                    <SelectValue placeholder="Selecione..." />
                  </SelectTrigger>
                  <SelectContent className="bg-zinc-900 border-zinc-800 text-zinc-100 rounded-xl">
                    <SelectItem value={String(CardBrand.Unknown)}>Outra / Desconhecida</SelectItem>
                    <SelectItem value={String(CardBrand.Visa)}>Visa</SelectItem>
                    <SelectItem value={String(CardBrand.Mastercard)}>Mastercard</SelectItem>
                    <SelectItem value={String(CardBrand.Elo)}>Elo</SelectItem>
                    <SelectItem value={String(CardBrand.Amex)}>American Express</SelectItem>
                    <SelectItem value={String(CardBrand.Hipercard)}>Hipercard</SelectItem>
                    <SelectItem value={String(CardBrand.Diners)}>Diners Club</SelectItem>
                    <SelectItem value={String(CardBrand.Discover)}>Discover</SelectItem>
                    <SelectItem value={String(CardBrand.JCB)}>JCB</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="cardKind" className="text-zinc-300 font-medium text-xs">Tipo de Cartão</Label>
                <Select
                  onValueChange={(value) => setFormData({...formData, cardKind: Number(value)})}
                  value={String(formData.cardKind)}
                >
                  <SelectTrigger className="bg-zinc-950/80 border-zinc-800 text-zinc-100 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm">
                    <SelectValue placeholder="Selecione..." />
                  </SelectTrigger>
                  <SelectContent className="bg-zinc-900 border-zinc-800 text-zinc-100 rounded-xl">
                    <SelectItem value={String(CardKind.Physical)}>Físico</SelectItem>
                    <SelectItem value={String(CardKind.Virtual)}>Virtual</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="lastFourDigits" className="text-zinc-300 font-medium text-xs">Últimos 4 dígitos</Label>
                <Input
                  id="lastFourDigits"
                  required
                  maxLength={4}
                  pattern="\d{4}"
                  placeholder="Ex: 1234"
                  className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm font-mono tracking-widest text-center"
                  value={formData.lastFourDigits}
                  onChange={e => setFormData({...formData, lastFourDigits: e.target.value.replace(/\D/g, '')})}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="limit" className="text-zinc-300 font-medium text-xs">Limite Total</Label>
                <div className="relative">
                  <span className="absolute left-3.5 top-2.5 text-xs text-zinc-500 font-bold">R$</span>
                  <Input
                    id="limit"
                    type="number"
                    step="0.01"
                    required
                    placeholder="0,00"
                    className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 pl-9 text-sm"
                    value={formData.limit || ''}
                    onChange={e => setFormData({...formData, limit: Number(e.target.value)})}
                  />
                </div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="closingDay" className="text-zinc-300 font-medium text-xs">Dia de Fechamento</Label>
                <Input
                  id="closingDay"
                  type="number"
                  min="1"
                  max="31"
                  required
                  className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm"
                  value={formData.closingDay}
                  onChange={e => setFormData({...formData, closingDay: Math.min(31, Math.max(1, Number(e.target.value)))})}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="dueDay" className="text-zinc-300 font-medium text-xs">Dia de Vencimento</Label>
                <Input
                  id="dueDay"
                  type="number"
                  min="1"
                  max="31"
                  required
                  className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm"
                  value={formData.dueDay}
                  onChange={e => setFormData({...formData, dueDay: Math.min(31, Math.max(1, Number(e.target.value)))})}
                />
              </div>
            </div>

            <div className="flex justify-end pt-4 gap-3">
              <Button
                type="button"
                onClick={() => router.back()}
                className="border-zinc-800 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50 rounded-xl"
                variant="outline"
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={loading}
                className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold transition-all duration-300 rounded-xl"
              >
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Salvar Cartão
              </Button>
            </div>
          </form>
        </motion.div>
      </div>
    </div>
  );
}
