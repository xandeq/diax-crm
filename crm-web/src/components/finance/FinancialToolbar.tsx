'use client';

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
    FinancialAccount,
    FinancialFilters,
    TransactionCategory
} from "@/services/finance";
import {
    Calendar as CalendarIcon,
    Search,
    X,
} from "lucide-react";
import { useEffect, useState } from "react";

interface FinancialToolbarProps {
  filters: FinancialFilters;
  onFilterChange: (filters: FinancialFilters) => void;
  categories: TransactionCategory[];
  accounts: FinancialAccount[];
}

export function FinancialToolbar({
  filters,
  onFilterChange,
  categories,
  accounts
}: FinancialToolbarProps) {
  const [searchValue, setSearchValue] = useState(filters.search || "");

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => {
      if (searchValue !== filters.search) {
        onFilterChange({ ...filters, search: searchValue, page: 1 });
      }
    }, 500);
    return () => clearTimeout(timer);
  }, [searchValue, filters, onFilterChange]);

  const handleClear = () => {
    setSearchValue("");
    onFilterChange({
      page: 1,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDescending: filters.sortDescending
    });
  };

  return (
    <div className="flex flex-col gap-4 mb-6">
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[250px] lg:min-w-[400px]">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-zinc-400" />
          <Input
            placeholder="Buscar por descrição..."
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            className="pl-11 pr-10 h-12 rounded-xl border-white/10 bg-white/5 focus-visible:ring-[#00D4AA] focus-visible:ring-offset-0 text-zinc-100 placeholder:text-zinc-500"
          />
          {searchValue && (
            <button
              onClick={() => setSearchValue("")}
              className="absolute right-4 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-200"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <select
            value={filters.categoryId || ""}
            onChange={(e) => onFilterChange({ ...filters, categoryId: e.target.value || undefined, page: 1 })}
            className="h-12 rounded-xl px-4 py-2 text-sm outline-none min-w-[180px] border border-white/10 bg-white/5 hover:bg-white/10 focus:border-emerald-500/50 text-zinc-200 transition-all cursor-pointer"
          >
            <option value="" className="bg-[#0B1510] text-zinc-300">Todas as Categorias</option>
            {categories.map((cat) => (
              <option key={cat.id} value={cat.id} className="bg-[#0B1510] text-zinc-300">{cat.name}</option>
            ))}
          </select>

          <select
            value={filters.financialAccountId || ""}
            onChange={(e) => onFilterChange({ ...filters, financialAccountId: e.target.value || undefined, page: 1 })}
            className="h-12 rounded-xl px-4 py-2 text-sm outline-none min-w-[180px] border border-white/10 bg-white/5 hover:bg-white/10 focus:border-emerald-500/50 text-zinc-200 transition-all cursor-pointer"
          >
            <option value="" className="bg-[#0B1510] text-zinc-300">Todas as Contas</option>
            {accounts.map((acc) => (
              <option key={acc.id} value={acc.id} className="bg-[#0B1510] text-zinc-300">{acc.name}</option>
            ))}
          </select>

          <div className="flex items-center gap-2 rounded-xl px-3.5 py-1 border border-white/10 bg-white/5 focus-within:border-emerald-500/50 transition-all">
             <CalendarIcon className="h-4 w-4 mr-1 text-zinc-400" />
             <input
               type="date"
               value={filters.startDate || ""}
               onChange={(e) => onFilterChange({ ...filters, startDate: e.target.value || undefined, page: 1 })}
               className="bg-transparent border-none text-sm outline-none h-10 w-28 text-zinc-300 focus:ring-0 cursor-pointer"
               style={{ colorScheme: 'dark' }}
             />
             <span className="text-zinc-500 text-xs font-semibold px-1">até</span>
             <input
               type="date"
               value={filters.endDate || ""}
               onChange={(e) => onFilterChange({ ...filters, endDate: e.target.value || undefined, page: 1 })}
               className="bg-transparent border-none text-sm outline-none h-10 w-28 text-zinc-300 focus:ring-0 cursor-pointer"
               style={{ colorScheme: 'dark' }}
             />
          </div>

          {(filters.search || filters.categoryId || filters.financialAccountId || filters.startDate || filters.endDate) && (
            <Button
              variant="ghost"
              onClick={handleClear}
              className="text-zinc-400 hover:text-rose-400 hover:bg-rose-500/10 border border-transparent hover:border-rose-500/20 gap-2 h-12 px-4 rounded-xl transition-all duration-200"
            >
              <X className="h-4 w-4" />
              Limpar Filtros
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
