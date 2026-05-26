'use client';

import { Input } from '@/components/ui/input';
import { normalizePhoneBR } from '@/lib/whatsapp-navigation';
import { searchContacts, Customer } from '@/services/customers';
import { Loader2, Search, X } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

export interface ContactOption {
  id: string;
  name: string;
  whatsApp: string | null;
  phone: string | null;
  email: string;
  companyName: string | null;
}

export function ContactSearchInput({
  onSelect,
  selectedContact,
  onClear,
}: {
  onSelect: (contact: ContactOption) => void;
  selectedContact: ContactOption | null;
  onClear: () => void;
}) {
  const [searchText, setSearchText] = useState('');
  const [results, setResults] = useState<ContactOption[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [searching, setSearching] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (searchText.length < 2) {
      setResults([]);
      setIsOpen(false);
      return;
    }

    const timer = setTimeout(async () => {
      setSearching(true);
      try {
        const res = await searchContacts(searchText, 10);
        const contacts: ContactOption[] = res.items.map((c: Customer) => ({
          id: c.id,
          name: c.name,
          whatsApp: c.whatsApp ?? null,
          phone: c.phone ?? null,
          email: c.email,
          companyName: c.companyName ?? null,
        }));
        setResults(contacts);
        setIsOpen(contacts.length > 0);
      } catch {
        setResults([]);
      } finally {
        setSearching(false);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [searchText]);

  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  if (selectedContact) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 rounded-md" style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.12)' }}>
        <div className="flex-1 min-w-0">
          <span className="font-medium" style={{ color: '#F9FAFB' }}>{selectedContact.name}</span>
          {selectedContact.companyName && (
            <span className="text-xs ml-2" style={{ color: '#6B7280' }}>({selectedContact.companyName})</span>
          )}
          <span className="text-sm ml-2 font-mono text-emerald-400">
            {normalizePhoneBR(selectedContact.whatsApp ?? selectedContact.phone) || selectedContact.email}
          </span>
        </div>
        <button
          type="button"
          onClick={onClear}
          className="p-1 rounded-md transition-colors"
          style={{ color: '#6B7280' }}
          title="Limpar seleção"
        >
          <X className="h-4 w-4" />
        </button>
      </div>
    );
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
        <Input
          value={searchText}
          onChange={(e) => {
            setSearchText(e.target.value);
            if (e.target.value.length >= 2) setIsOpen(true);
          }}
          onFocus={() => results.length > 0 && setIsOpen(true)}
          placeholder="Buscar por nome, email ou empresa..."
          className="pl-9"
        />
        {searching && (
          <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 animate-spin text-slate-400" />
        )}
      </div>
      {isOpen && results.length > 0 && (
        <div className="absolute z-50 w-full mt-1 rounded-md shadow-lg max-h-64 overflow-y-auto" style={{ background: '#0D1F18', border: '1px solid rgba(255,255,255,0.12)' }}>
          {results.map((contact) => (
            <button
              key={contact.id}
              type="button"
              className="w-full text-left px-3 py-2.5 flex items-center justify-between transition-colors last:border-0"
              style={{ borderBottom: '1px solid rgba(255,255,255,0.06)', color: '#D1D5DB' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.05)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
              onClick={() => {
                onSelect(contact);
                setSearchText('');
                setIsOpen(false);
              }}
            >
              <div className="min-w-0">
                <span className="font-medium" style={{ color: '#F9FAFB' }}>{contact.name}</span>
                {contact.companyName && (
                  <span className="text-xs ml-2" style={{ color: '#6B7280' }}>({contact.companyName})</span>
                )}
              </div>
              <span className="text-sm ml-3 font-mono shrink-0 text-emerald-400">
                {normalizePhoneBR(contact.whatsApp ?? contact.phone) || '–'}
              </span>
            </button>
          ))}
        </div>
      )}
      {isOpen && searchText.length >= 2 && results.length === 0 && !searching && (
        <div className="absolute z-50 w-full mt-1 rounded-md shadow-lg p-3 text-sm text-center" style={{ background: '#0D1F18', border: '1px solid rgba(255,255,255,0.12)', color: '#6B7280' }}>
          Nenhum contato encontrado.
        </div>
      )}
    </div>
  );
}
