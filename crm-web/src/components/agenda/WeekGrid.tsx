'use client';

import { Appointment } from '@/types/agenda';
import { addDays, format, isSameDay, parseISO, startOfWeek } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { useState } from 'react';

const HOUR_START = 7;
const HOUR_END = 22;
const TOTAL_HOURS = HOUR_END - HOUR_START;
const HOUR_HEIGHT = 64; // px per hour

interface WeekGridProps {
    currentDate: Date;
    appointments: Appointment[];
    onSlotClick: (date: Date) => void;
    onAppointmentClick: (appt: Appointment) => void;
    onAppointmentDrop?: (id: string, newDateISO: string) => void;
}

export function WeekGrid({ currentDate, appointments, onSlotClick, onAppointmentClick, onAppointmentDrop }: WeekGridProps) {
    const weekStart = startOfWeek(currentDate, { weekStartsOn: 1 }); // Monday
    const days = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));
    const hours = Array.from({ length: TOTAL_HOURS }, (_, i) => HOUR_START + i);

    const [draggingId, setDraggingId] = useState<string | null>(null);
    const [dropHighlight, setDropHighlight] = useState<string | null>(null); // "dayISO-hour"

    const getApptStyle = (appt: Appointment) => {
        const d = parseISO(appt.date);
        const minutes = d.getHours() * 60 + d.getMinutes();
        const startMinFromGridStart = minutes - HOUR_START * 60;
        const top = (startMinFromGridStart / 60) * HOUR_HEIGHT;
        const height = Math.max(((appt.durationMinutes || 60) / 60) * HOUR_HEIGHT, 24);
        return { top: `${top}px`, height: `${height}px` };
    };

    const getApptColor = (appt: Appointment) => {
        if (appt.label?.color) return appt.label.color;
        const fallback: Record<string, string> = {
            Medical: '#3B82F6',
            HomeService: '#F97316',
            Payment: '#EF4444',
            Other: '#64748b',
        };
        return fallback[appt.type] ?? '#64748b';
    };

    const handleDrop = (day: Date, h: number, e: React.DragEvent) => {
        e.preventDefault();
        if (!draggingId || !onAppointmentDrop) return;
        const newDate = new Date(day);
        newDate.setHours(h, 0, 0, 0);
        onAppointmentDrop(draggingId, newDate.toISOString());
        setDraggingId(null);
        setDropHighlight(null);
    };

    return (
        <div className="flex flex-col h-full overflow-hidden">
            {/* Header row */}
            <div className="flex border-b border-slate-200 bg-slate-50/50">
                {/* Time gutter */}
                <div className="w-14 flex-shrink-0" />
                {days.map(day => (
                    <div key={day.toISOString()} className="flex-1 py-3 text-center border-l border-slate-200">
                        <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">
                            {format(day, 'EEE', { locale: ptBR })}
                        </p>
                        <p className={`text-lg font-bold mt-0.5 w-9 h-9 flex items-center justify-center rounded-full mx-auto
                            ${isSameDay(day, new Date()) ? 'bg-blue-600 text-white' : 'text-slate-700'}`}>
                            {format(day, 'd')}
                        </p>
                    </div>
                ))}
            </div>

            {/* Scrollable grid */}
            <div className="flex overflow-y-auto flex-1">
                {/* Time gutter */}
                <div className="w-14 flex-shrink-0 relative">
                    {hours.map(h => (
                        <div key={h} className="relative" style={{ height: `${HOUR_HEIGHT}px` }}>
                            <span className="absolute -top-2.5 right-2 text-xs text-slate-400">
                                {String(h).padStart(2, '0')}:00
                            </span>
                        </div>
                    ))}
                </div>

                {/* Day columns */}
                {days.map(day => {
                    const dayAppts = appointments.filter(a => isSameDay(parseISO(a.date), day));
                    return (
                        <div
                            key={day.toISOString()}
                            className="flex-1 relative border-l border-slate-200"
                            style={{ minHeight: `${TOTAL_HOURS * HOUR_HEIGHT}px` }}
                        >
                            {/* Hour slots — droppable */}
                            {hours.map(h => {
                                const slotKey = `${day.toISOString()}-${h}`;
                                const isHighlighted = dropHighlight === slotKey;
                                return (
                                    <div
                                        key={h}
                                        className={`absolute left-0 right-0 border-t border-slate-100 cursor-pointer transition-colors
                                            ${isHighlighted ? 'bg-blue-100/60' : 'hover:bg-blue-50/40'}`}
                                        style={{ top: `${(h - HOUR_START) * HOUR_HEIGHT}px`, height: `${HOUR_HEIGHT}px` }}
                                        onClick={() => {
                                            const d = new Date(day);
                                            d.setHours(h, 0, 0, 0);
                                            onSlotClick(d);
                                        }}
                                        onDragOver={(e) => {
                                            e.preventDefault();
                                            setDropHighlight(slotKey);
                                        }}
                                        onDragLeave={() => setDropHighlight(null)}
                                        onDrop={(e) => handleDrop(day, h, e)}
                                    />
                                );
                            })}

                            {/* Appointments */}
                            {dayAppts.map(appt => {
                                const color = getApptColor(appt);
                                const style = getApptStyle(appt);
                                const isDragging = draggingId === appt.id;
                                return (
                                    <div
                                        key={appt.id}
                                        draggable={!!onAppointmentDrop}
                                        className={`absolute left-0.5 right-0.5 rounded-md px-1.5 py-0.5 overflow-hidden shadow-sm hover:shadow-md transition-all z-10
                                            ${onAppointmentDrop ? 'cursor-grab active:cursor-grabbing' : 'cursor-pointer'}
                                            ${isDragging ? 'opacity-40 scale-95' : 'opacity-100'}`}
                                        style={{
                                            ...style,
                                            backgroundColor: color + '25',
                                            borderLeft: `3px solid ${color}`,
                                        }}
                                        onDragStart={(e) => {
                                            setDraggingId(appt.id);
                                            e.dataTransfer.effectAllowed = 'move';
                                        }}
                                        onDragEnd={() => {
                                            setDraggingId(null);
                                            setDropHighlight(null);
                                        }}
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            onAppointmentClick(appt);
                                        }}
                                    >
                                        <p className="text-[11px] font-semibold leading-tight truncate" style={{ color }}>
                                            {format(parseISO(appt.date), 'HH:mm')} {appt.title}
                                        </p>
                                        {appt.label && (
                                            <p className="text-[10px] opacity-70 truncate" style={{ color }}>
                                                {appt.label.name}
                                            </p>
                                        )}
                                    </div>
                                );
                            })}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}
