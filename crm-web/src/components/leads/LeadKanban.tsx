'use client';

import { Lead, CustomerStatus } from '@/services/leads';
import { useState } from 'react';
import { LeadKanbanCard } from './LeadKanbanCard';

interface Column {
    status: CustomerStatus;
    label: string;
    color: string;
    headerBg: string;
    dotColor: string;
}

const COLUMNS: Column[] = [
    { status: CustomerStatus.Lead,        label: 'Lead',        color: 'border-slate-300', headerBg: 'bg-slate-50',  dotColor: 'bg-slate-400'  },
    { status: CustomerStatus.Contacted,   label: 'Contatado',   color: 'border-blue-200',  headerBg: 'bg-blue-50',   dotColor: 'bg-blue-400'   },
    { status: CustomerStatus.Qualified,   label: 'Qualificado', color: 'border-violet-200',headerBg: 'bg-violet-50', dotColor: 'bg-violet-500' },
    { status: CustomerStatus.Negotiating, label: 'Negociando',  color: 'border-amber-200', headerBg: 'bg-amber-50',  dotColor: 'bg-amber-500'  },
    { status: CustomerStatus.Customer,    label: 'Cliente',     color: 'border-green-200', headerBg: 'bg-green-50',  dotColor: 'bg-green-500'  },
    { status: CustomerStatus.Inactive,    label: 'Inativo',     color: 'border-gray-200',  headerBg: 'bg-gray-50',   dotColor: 'bg-gray-400'   },
    { status: CustomerStatus.Churned,     label: 'Churned',     color: 'border-red-200',   headerBg: 'bg-red-50',    dotColor: 'bg-red-400'    },
];

interface Props {
    leads: Lead[];
    onStatusChange: (leadId: string, newStatus: CustomerStatus) => Promise<void>;
}

export function LeadKanban({ leads, onStatusChange }: Props) {
    const [draggedId, setDraggedId] = useState<string | null>(null);
    const [dragOverStatus, setDragOverStatus] = useState<CustomerStatus | null>(null);
    const [updatingId, setUpdatingId] = useState<string | null>(null);

    const grouped = COLUMNS.reduce<Record<CustomerStatus, Lead[]>>((acc, col) => {
        acc[col.status] = leads.filter(l => l.status === col.status);
        return acc;
    }, {} as Record<CustomerStatus, Lead[]>);

    const handleDragStart = (e: React.DragEvent, leadId: string) => {
        setDraggedId(leadId);
        e.dataTransfer.effectAllowed = 'move';
    };

    const handleDragOver = (e: React.DragEvent, status: CustomerStatus) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        setDragOverStatus(status);
    };

    const handleDragLeave = () => {
        setDragOverStatus(null);
    };

    const handleDrop = async (e: React.DragEvent, targetStatus: CustomerStatus) => {
        e.preventDefault();
        setDragOverStatus(null);

        if (!draggedId) return;
        const lead = leads.find(l => l.id === draggedId);
        if (!lead || lead.status === targetStatus) {
            setDraggedId(null);
            return;
        }

        setDraggedId(null);
        setUpdatingId(lead.id);
        try {
            await onStatusChange(lead.id, targetStatus);
        } finally {
            setUpdatingId(null);
        }
    };

    const handleDragEnd = () => {
        setDraggedId(null);
        setDragOverStatus(null);
    };

    return (
        <div className="flex gap-3 overflow-x-auto pb-4 min-h-[60vh]">
            {COLUMNS.map((col) => {
                const cards = grouped[col.status] ?? [];
                const isOver = dragOverStatus === col.status;

                return (
                    <div
                        key={col.status}
                        className={`
                            flex flex-col shrink-0 w-60 rounded-xl border-2 transition-colors duration-150
                            ${isOver ? 'border-blue-400 bg-blue-50/60' : `${col.color} bg-white/60`}
                        `}
                        onDragOver={(e) => handleDragOver(e, col.status)}
                        onDragLeave={handleDragLeave}
                        onDrop={(e) => handleDrop(e, col.status)}
                    >
                        {/* Column header */}
                        <div className={`px-3 py-2.5 rounded-t-xl ${col.headerBg} border-b ${col.color}`}>
                            <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                    <span className={`h-2.5 w-2.5 rounded-full ${col.dotColor}`} />
                                    <span className="text-sm font-semibold text-slate-700">{col.label}</span>
                                </div>
                                <span className="text-xs font-bold text-slate-500 bg-white/70 px-1.5 py-0.5 rounded-full">
                                    {cards.length}
                                </span>
                            </div>
                        </div>

                        {/* Cards */}
                        <div className="flex flex-col gap-2 p-2 flex-1 min-h-[100px]">
                            {cards.map((lead) => (
                                <LeadKanbanCard
                                    key={lead.id}
                                    lead={lead}
                                    isUpdating={updatingId === lead.id}
                                    onDragStart={handleDragStart}
                                />
                            ))}

                            {cards.length === 0 && (
                                <div className={`
                                    flex-1 flex items-center justify-center rounded-lg border-2 border-dashed
                                    transition-colors duration-150 min-h-[80px]
                                    ${isOver ? 'border-blue-300 bg-blue-50' : 'border-slate-200'}
                                `}>
                                    <span className="text-xs text-slate-400">Soltar aqui</span>
                                </div>
                            )}
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
