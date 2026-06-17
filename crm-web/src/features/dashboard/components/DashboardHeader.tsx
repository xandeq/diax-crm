'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { RefreshCw, Plus, UserPlus, Mail, DollarSign } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import Link from 'next/link';

interface DashboardHeaderProps {
  lastSync: Date;
  isSyncing: boolean;
  onSync: () => void;
}

export function DashboardHeader({ lastSync, isSyncing, onSync }: DashboardHeaderProps) {
  const [timeStr, setTimeStr] = useState<string>('');

  useEffect(() => {
    setTimeStr(new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'short' }));
  }, []);

  return (
    <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-2 border-b border-zinc-800/40">
      <div>
        <h1 className="text-3xl font-extrabold tracking-tight text-zinc-50 bg-gradient-to-r from-zinc-50 via-zinc-200 to-zinc-400 bg-clip-text text-transparent">
          Painel de Controle
        </h1>
        <div className="flex items-center gap-2 mt-1.5">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
          </span>
          <span className="text-xs font-semibold text-zinc-400 uppercase tracking-wider">
            Live
          </span>
          <span className="text-zinc-600">•</span>
          <span className="text-xs text-zinc-500 font-medium">
            {timeStr}
          </span>
          <span className="text-zinc-600">•</span>
          <span className="text-xs text-zinc-500 font-medium">
            Sincronizado há: {lastSync.toLocaleTimeString('pt-BR')}
          </span>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <Button
          variant="outline"
          size="sm"
          onClick={onSync}
          disabled={isSyncing}
          className="h-9 border-zinc-800/80 hover:bg-zinc-900/60 text-zinc-300 font-semibold gap-2 transition-all"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isSyncing ? 'animate-spin' : ''}`} />
          Sincronizar
        </Button>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              size="sm"
              className="h-9 bg-teal-500 hover:bg-teal-600 text-zinc-950 font-bold gap-1.5 transition-all shadow-[0_4px_20px_rgba(20,184,166,0.25)] hover:shadow-[0_4px_25px_rgba(20,184,166,0.4)] border-none"
            >
              <Plus className="h-4 w-4 stroke-[3px]" />
              Novo Registro
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56 bg-zinc-950/95 border-zinc-800 text-zinc-200 shadow-2xl backdrop-blur-xl">
            <DropdownMenuItem asChild className="focus:bg-zinc-900 focus:text-zinc-50 cursor-pointer">
              <Link href="/leads/import" className="flex items-center gap-2 py-2">
                <UserPlus className="h-4 w-4 text-teal-400" />
                <span>Importar Lead</span>
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem asChild className="focus:bg-zinc-900 focus:text-zinc-50 cursor-pointer">
              <Link href="/email-marketing" className="flex items-center gap-2 py-2">
                <Mail className="h-4 w-4 text-blue-400" />
                <span>Nova Campanha</span>
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem asChild className="focus:bg-zinc-900 focus:text-zinc-50 cursor-pointer">
              <Link href="/finance" className="flex items-center gap-2 py-2">
                <DollarSign className="h-4 w-4 text-emerald-400" />
                <span>Nova Transação</span>
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  );
}
