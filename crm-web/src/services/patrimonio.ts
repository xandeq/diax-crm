import { apiFetch } from './api';

// =====================================================
// PATRIMÔNIO & INVESTIMENTOS — ENUMS
//
// NOTE: these are numeric (not string) enums, matching the actual wire
// format of the api-core backend (Diax.Api Program.cs only configures
// camelCase PropertyNamingPolicy — no JsonStringEnumConverter is
// registered globally or per-DTO for Diax.Domain.Finance.Assets.*), the
// same convention already used by AccountType/PaymentMethod/etc. in
// finance.ts. Ordinal values below mirror the C# enum declarations 1:1.
// =====================================================

export enum AssetClass {
    Ouro = 0,
    Diamante = 1,
    Acao = 2,
    Fii = 3,
    RendaFixa = 4,
    Fundo = 5,
    Multimercado = 6,
    Veiculo = 7,
    Imovel = 8,
    Consorcio = 9,
    Moeda = 10,
    Exterior = 11,
    Milhas = 12,
    Cripto = 13,
    Dinheiro = 14,
    Fgts = 15,
    Titulo = 16,
    Etf = 17,
    Bdr = 18,
    CrowdfundingImob = 19,
    CartaCredito = 20,
    Joias = 21,
    Arte = 22,
    PropriedadeIntelectual = 23,
    Negocio = 24,
    Outro = 99,
}

export enum AssetLiquidity {
    Liquido = 0,
    Iliquido = 1,
    Travado = 2,
    Contingente = 3,
}

export enum AssetOwnership {
    Alexandre = 0,
    Esposa = 1,
    Conjunto = 2,
}

export enum AssetValuationSource {
    Market = 0,
    Indexed = 1,
    ManualDepreciation = 2,
    Manual = 3,
}

// =====================================================
// PATRIMÔNIO & INVESTIMENTOS — INTERFACES
// =====================================================

export interface Asset {
    id: string;
    name: string;
    class: AssetClass;
    ownership: AssetOwnership;
    liquidity: AssetLiquidity;
    currency: string;
    currentValue: number;
    costBasis?: number;
    acquiredAt?: string;
    valuationSource: AssetValuationSource;
    notes?: string;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateAssetRequest {
    name: string;
    class: AssetClass;
    ownership: AssetOwnership;
    liquidity: AssetLiquidity;
    currentValue: number;
    valuationSource: AssetValuationSource;
    currency?: string;
    costBasis?: number;
    acquiredAt?: string;
    notes?: string;
}

export interface UpdateAssetRequest {
    name: string;
    class: AssetClass;
    ownership: AssetOwnership;
    liquidity: AssetLiquidity;
    currentValue: number;
    valuationSource: AssetValuationSource;
    currency?: string;
    costBasis?: number;
    acquiredAt?: string;
    notes?: string;
}

export interface AddValuationRequest {
    value: number;
    currency?: string;
    asOf?: string;
    source?: AssetValuationSource;
}

export interface AssetFilters {
    class?: AssetClass;
    ownership?: AssetOwnership;
    liquidity?: AssetLiquidity;
}

export interface NetWorthByClassRow {
    class: AssetClass;
    totalBRL: number;
    pct: number;
}

export interface NetWorthByLiquidityRow {
    liquidity: AssetLiquidity;
    totalBRL: number;
}

export interface NetWorthByOwnershipRow {
    ownership: AssetOwnership;
    totalBRL: number;
}

export interface NetWorthCurrencyRow {
    currency: string;
    total: number;
}

export interface NetWorthSummary {
    totalRealizableBRL: number;
    totalContingentBRL: number;
    totalProjectedBRL: number;
    byClass: NetWorthByClassRow[];
    byLiquidity: NetWorthByLiquidityRow[];
    byOwnership: NetWorthByOwnershipRow[];
    currencies: NetWorthCurrencyRow[];
}

// =====================================================
// PATRIMÔNIO & INVESTIMENTOS — SERVICE
// Base path: /patrimonio (see Diax.Api.Controllers.V1.PatrimonioController)
// =====================================================

export const patrimonioService = {
    listAssets: async (filters?: AssetFilters) => {
        const params = new URLSearchParams();
        if (filters) {
            Object.entries(filters).forEach(([key, value]) => {
                if (value !== undefined && value !== null) {
                    params.append(key, String(value));
                }
            });
        }
        const query = params.toString();
        return apiFetch<Asset[]>(`/patrimonio/assets${query ? `?${query}` : ''}`);
    },
    getAsset: async (id: string) => {
        return apiFetch<Asset>(`/patrimonio/assets/${id}`);
    },
    createAsset: async (data: CreateAssetRequest) => {
        return apiFetch<Asset>('/patrimonio/assets', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateAsset: async (id: string, data: UpdateAssetRequest) => {
        return apiFetch<void>(`/patrimonio/assets/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteAsset: async (id: string) => {
        return apiFetch<void>(`/patrimonio/assets/${id}`, {
            method: 'DELETE',
        });
    },
    addValuation: async (id: string, data: AddValuationRequest) => {
        return apiFetch<void>(`/patrimonio/assets/${id}/valuations`, {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    getNetWorth: async () => {
        return apiFetch<NetWorthSummary>('/patrimonio/networth');
    },
};
