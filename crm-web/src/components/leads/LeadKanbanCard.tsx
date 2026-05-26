'use client';

import { Lead } from '@/services/leads';
import { Loader2, Mail, MessageCircle } from 'lucide-react';

interface Props {
    lead: Lead;
    isUpdating: boolean;
    onDragStart: (e: React.DragEvent, leadId: string) => void;
}

export function LeadKanbanCard({ lead, isUpdating, onDragStart }: Props) {
    return (
        <div
            draggable={!isUpdating}
            onDragStart={(e) => onDragStart(e, lead.id)}
            className={`rounded-lg p-3 cursor-grab active:cursor-grabbing select-none transition-all duration-150 ${isUpdating ? 'opacity-50 cursor-not-allowed' : ''}`}
            style={{ background: 'rgba(255,255,255,0.06)', border: '1px solid rgba(255,255,255,0.1)' }}
        >
            <div className="flex items-start justify-between gap-2">
                <div className="min-w-0 flex-1">
                    <p className="text-sm font-semibold text-slate-900 truncate" title={lead.name}>
                        {lead.name}
                    </p>
                    {lead.companyName && (
                        <p className="text-xs text-slate-500 truncate" title={lead.companyName}>
                            {lead.companyName}
                        </p>
                    )}
                </div>
                {isUpdating && <Loader2 className="h-4 w-4 shrink-0 animate-spin text-blue-500" />}
            </div>

            {lead.email && (
                <p className="mt-1.5 text-xs text-slate-400 truncate" title={lead.email}>
                    {lead.email}
                </p>
            )}

            <div className="mt-2 flex items-center gap-2">
                {lead.leadScore != null && (
                    <span className={`text-xs font-bold px-1.5 py-0.5 rounded ${
                        lead.leadScore >= 80 ? 'bg-green-100 text-green-700'
                        : lead.leadScore >= 50 ? 'bg-yellow-100 text-yellow-700'
                        : 'bg-slate-100 text-slate-500'
                    }`}>
                        {lead.leadScore}
                    </span>
                )}
                {lead.email && (
                    <Mail className="h-3 w-3 text-slate-300" />
                )}
                {lead.whatsApp && (
                    <MessageCircle className="h-3 w-3 text-slate-300" />
                )}
            </div>
        </div>
    );
}
