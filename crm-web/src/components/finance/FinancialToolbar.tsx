'use client';

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
    ExpenseCategory,
    FinancialAccount,
    FinancialFilters,
    IncomeCategory
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
  categories: (IncomeCategory | ExpenseCategory)[];
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
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Buscar por descrição..."
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            className="pl-11 pr-10 h-12 rounded-xl"
          />
          {searchValue && (
            <button
              onClick={() => setSearchValue("")}
              className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <select
            value={filters.categoryId || ""}
            onChange={(e) => onFilterChange({ ...filters, categoryId: e.target.value || undefined, page: 1 })}
            className="h-12 rounded-xl border border-gray-200 bg-white px-4 py-2 text-sm outline-none focus:ring-2 focus:ring-accent/20 focus:border-accent min-w-[180px] shadow-sm transition-all"
          >
            <option value="">Todas as Categorias</option>
            {categories.map((cat) => (
              <option key={cat.id} value={cat.id}>{cat.name}</option>
            ))}
          </select>

          <select
            value={filters.financialAccountId || ""}
            onChange={(e) => onFilterChange({ ...filters, financialAccountId: e.target.value || undefined, page: 1 })}
            className="h-12 rounded-xl border border-gray-200 bg-white px-4 py-2 text-sm outline-none focus:ring-2 focus:ring-accent/20 focus:border-accent min-w-[180px] shadow-sm transition-all"
          >
            <option value="">Todas as Contas</option>
            {accounts.map((acc) => (
              <option key={acc.id} value={acc.id}>{acc.name}</option>
            ))}
          </select>

          <div className="flex items-center gap-2 bg-white border border-gray-200 rounded-xl px-3 py-1 shadow-sm">
             <CalendarIcon className="h-4 w-4 text-gray-400 mr-1" />
             <input
               type="date"
               value={filters.startDate || ""}
               onChange={(e) => onFilterChange({ ...filters, startDate: e.target.value || undefined, page: 1 })}
               className="bg-transparent border-none text-sm outline-none h-10 w-32"
             />
             <span className="text-gray-300">até</span>
             <input
               type="date"
               value={filters.endDate || ""}
               onChange={(e) => onFilterChange({ ...filters, endDate: e.target.value || undefined, page: 1 })}
               className="bg-transparent border-none text-sm outline-none h-10 w-32"
             />
          </div>

          {(filters.search || filters.categoryId || filters.financialAccountId || filters.startDate || filters.endDate) && (
            <Button
              variant="ghost"
              onClick={handleClear}
              className="text-gray-500 hover:text-red-600 hover:bg-red-50 gap-2 h-12 px-4 rounded-xl"
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
