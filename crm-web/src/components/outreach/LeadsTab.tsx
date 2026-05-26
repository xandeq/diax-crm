'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { ChevronLeft, ChevronRight, RefreshCw, Search, Users, X } from 'lucide-react';
import { useState } from 'react';
import { useRouter } from 'next/navigation';

import { ReadyLeadResponse } from '@/services/outreach';
import { getSegmentBadge } from '@/components/outreach/OutreachShared';
import { formatDateShort } from '@/lib/date-utils';

// ─── Ready Leads Tab ──────────────────────────────────────────────────────────

export interface LeadsTabProps {
  leads: ReadyLeadResponse[];
  loading: boolean;
  onRefresh: () => void;
}

type LeadSortKey = 'name' | 'email' | 'segment' | 'leadScore' | 'companyName' | 'lastEmailSentAt';
const SEGMENT_ORDER: Record<string, number> = { hot: 3, warm: 2, cold: 1 };

export function LeadsTab({ leads, loading, onRefresh }: LeadsTabProps) {
  const router = useRouter();
  const [search, setSearch]       = useState('');
  const [segFilter, setSegFilter] = useState<string>('');
  const [sortKey, setSortKey]     = useState<LeadSortKey>('leadScore');
  const [sortDir, setSortDir]     = useState<'asc' | 'desc'>('desc');
  const [page, setPage]           = useState(1);
  const PAGE_SIZE = 25;

  const toggleSort = (key: LeadSortKey) => {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    else { setSortKey(key); setSortDir('desc'); }
  };

  const filtered = leads
    .filter(l => {
      const q = search.toLowerCase();
      const matchSearch = !q || l.name.toLowerCase().includes(q) || l.email.toLowerCase().includes(q) || (l.companyName ?? '').toLowerCase().includes(q);
      const matchSeg = !segFilter || l.segment.toLowerCase() === segFilter;
      return matchSearch && matchSeg;
    })
    .sort((a, b) => {
      let va: string | number = '';
      let vb: string | number = '';
      if (sortKey === 'name')           { va = a.name; vb = b.name; }
      else if (sortKey === 'email')     { va = a.email; vb = b.email; }
      else if (sortKey === 'segment')   { va = SEGMENT_ORDER[a.segment.toLowerCase()] ?? 0; vb = SEGMENT_ORDER[b.segment.toLowerCase()] ?? 0; }
      else if (sortKey === 'leadScore') { va = a.leadScore; vb = b.leadScore; }
      else if (sortKey === 'companyName') { va = a.companyName ?? ''; vb = b.companyName ?? ''; }
      else if (sortKey === 'lastEmailSentAt') { va = a.lastEmailSentAt ?? ''; vb = b.lastEmailSentAt ?? ''; }
      if (va < vb) return sortDir === 'asc' ? -1 : 1;
      if (va > vb) return sortDir === 'asc' ? 1 : -1;
      return 0;
    });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const firstItem  = filtered.length > 0 ? (page - 1) * PAGE_SIZE + 1 : 0;
  const lastItem   = Math.min(page * PAGE_SIZE, filtered.length);

  const LeadColHead = ({ col, label, className = '' }: { col: LeadSortKey; label: string; className?: string }) => (
    <TableHead
      className={`cursor-pointer select-none whitespace-nowrap hover:bg-slate-50 transition-colors ${className}`}
      onClick={() => { toggleSort(col); setPage(1); }}
    >
      <span className="flex items-center gap-0.5">
        {label}
        {sortKey === col
          ? <span className="ml-1 text-[10px] text-slate-700">{sortDir === 'asc' ? '↑' : '↓'}</span>
          : <span className="ml-1 opacity-20 text-[10px]">↕</span>}
      </span>
    </TableHead>
  );

  if (loading) {
    return (
      <Card>
        <CardContent className="p-4 space-y-2">
          {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-10 w-full rounded" />)}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      {/* ── Header ── */}
      <CardHeader className="pb-2">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <CardTitle className="text-base flex items-center gap-2">
              <Users className="h-4 w-4 text-slate-500" />
              Leads Prontos
            </CardTitle>
            <Badge variant="secondary" className="font-normal tabular-nums">
              {filtered.length !== leads.length
                ? `${filtered.length} de ${leads.length}`
                : leads.length}
            </Badge>
          </div>
          <div className="flex items-center gap-2">
            <div className="relative">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Nome, email ou empresa…"
                className="h-8 pl-8 w-48 text-sm"
                value={search}
                onChange={e => { setSearch(e.target.value); setPage(1); }}
              />
              {search && (
                <button className="absolute right-2 top-2 text-muted-foreground hover:text-foreground" onClick={() => { setSearch(''); setPage(1); }}>
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
            <Button variant="outline" size="sm" className="h-8" onClick={onRefresh}>
              <RefreshCw className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
        {/* Segment filter chips */}
        <div className="flex gap-1.5 pt-1 flex-wrap">
          {[
            { value: '',     label: 'Todos'   },
            { value: 'hot',  label: '🔥 Hot'  },
            { value: 'warm', label: '☀️ Warm' },
            { value: 'cold', label: '🧊 Cold' },
          ].map(opt => (
            <button
              key={opt.value}
              onClick={() => { setSegFilter(opt.value); setPage(1); }}
              className={`rounded-full px-3 py-0.5 text-xs font-medium border transition-colors ${
                segFilter === opt.value
                  ? 'bg-slate-900 text-white border-slate-900'
                  : 'bg-background text-slate-500 border-slate-200 hover:border-slate-400 hover:text-slate-700'
              }`}
            >
              {opt.label}
            </button>
          ))}
          {(search || segFilter) && (
            <button
              onClick={() => { setSearch(''); setSegFilter(''); setPage(1); }}
              className="flex items-center gap-1 rounded-full px-3 py-0.5 text-xs border border-border text-muted-foreground hover:bg-muted ml-1"
            >
              <X className="h-3 w-3" /> Limpar
            </button>
          )}
        </div>
      </CardHeader>

      <CardContent className="p-0">
        {leads.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-slate-400">
            <Users className="h-10 w-10 mb-3 opacity-20" />
            <p className="font-medium text-slate-500 text-sm">Nenhum lead disponível</p>
            <p className="text-xs text-slate-400 mt-1">Execute a segmentação para classificar os leads.</p>
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-slate-400">
            <Search className="h-8 w-8 mb-2 opacity-20" />
            <p className="text-sm">Nenhum resultado para os filtros aplicados.</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow className="bg-slate-50/60">
                    <LeadColHead col="name"            label="Nome"         className="pl-5 w-[180px]" />
                    <LeadColHead col="email"           label="Email"        className="w-[200px]" />
                    <LeadColHead col="segment"         label="Segmento"     className="w-[110px]" />
                    <LeadColHead col="leadScore"       label="Score"        className="w-[80px] text-right" />
                    <LeadColHead col="companyName"     label="Empresa"      className="w-[160px]" />
                    <LeadColHead col="lastEmailSentAt" label="Último Envio" className="w-[150px]" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {paginated.map(lead => (
                    <TableRow
                      key={lead.id}
                      className="cursor-pointer hover:bg-slate-50/60 transition-colors"
                      onClick={() => router.push(`/leads?search=${encodeURIComponent(lead.name)}`)}
                    >
                      <TableCell className="pl-5 font-medium text-blue-700 hover:underline text-sm truncate max-w-[180px]" title={lead.name}>
                        {lead.name}
                      </TableCell>
                      <TableCell className="text-slate-500 text-xs truncate max-w-[200px]" title={lead.email}>
                        {lead.email}
                      </TableCell>
                      <TableCell>{getSegmentBadge(lead.segment)}</TableCell>
                      <TableCell className="text-right pr-5">
                        <span className={`font-bold tabular-nums text-sm ${lead.leadScore >= 70 ? 'text-green-600' : lead.leadScore >= 40 ? 'text-amber-600' : 'text-slate-500'}`}>
                          {lead.leadScore}
                        </span>
                      </TableCell>
                      <TableCell className="text-slate-500 text-xs truncate max-w-[160px]" title={lead.companyName ?? ''}>
                        {lead.companyName ?? <span className="text-slate-300">—</span>}
                      </TableCell>
                      <TableCell className="text-slate-400 text-xs whitespace-nowrap">
                        {lead.lastEmailSentAt ? formatDateShort(lead.lastEmailSentAt) : <span className="text-slate-300">Nunca</span>}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between border-t px-5 py-3">
                <p className="text-xs text-slate-500">
                  Mostrando {firstItem}–{lastItem} de {filtered.length} leads
                </p>
                <div className="flex items-center gap-1">
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(1)}             disabled={page === 1}>«</Button>
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(p => p - 1)}    disabled={page === 1}><ChevronLeft className="h-3.5 w-3.5" /></Button>
                  <span className="px-3 text-xs text-slate-600 whitespace-nowrap">{page} / {totalPages}</span>
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(p => p + 1)}    disabled={page >= totalPages}><ChevronRight className="h-3.5 w-3.5" /></Button>
                  <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(totalPages)}    disabled={page >= totalPages}>»</Button>
                </div>
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
