'use client';

import { EmptyState } from '@/components/dashboard/EmptyState';
import { FinanceNav } from '@/components/finance/FinanceNav';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { Textarea } from '@/components/ui/textarea';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { cn, formatCurrency } from '@/lib/utils';
import {
    Asset,
    AssetClass,
    AssetLiquidity,
    AssetOwnership,
    AssetValuationSource,
    CreateAssetRequest,
    NetWorthSummary,
    NextAction,
    NextActionCategory,
    Opportunity,
    OpportunityRisk,
    UpdateAssetRequest,
    UpdateWealthProfileRequest,
    WealthProfile,
    patrimonioService,
} from '@/services/patrimonio';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
    CheckCircle2,
    CircleDollarSign,
    Compass,
    Gem,
    Layers,
    Lightbulb,
    ListChecks,
    Loader2,
    Pencil,
    PieChart,
    Plus,
    RefreshCw,
    Sparkles,
    Target,
    Trash2,
    Users,
    Wallet,
    XCircle,
} from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

// ─── Query keys ─────────────────────────────────────────────────────────────

const ASSETS_KEY = ['patrimonio', 'assets'] as const;
const NET_WORTH_KEY = ['patrimonio', 'net-worth'] as const;
const OPPORTUNITIES_KEY = ['patrimonio', 'opportunities'] as const;
const ACTIONS_KEY = ['patrimonio', 'actions'] as const;
const PROFILE_KEY = ['patrimonio', 'profile'] as const;

// ─── Labels & styling maps ──────────────────────────────────────────────────

const ASSET_CLASS_LABELS: Record<AssetClass, string> = {
    [AssetClass.Ouro]: 'Ouro',
    [AssetClass.Diamante]: 'Diamante',
    [AssetClass.Acao]: 'Ações',
    [AssetClass.Fii]: 'FIIs',
    [AssetClass.RendaFixa]: 'Renda Fixa',
    [AssetClass.Fundo]: 'Fundos',
    [AssetClass.Multimercado]: 'Multimercado',
    [AssetClass.Veiculo]: 'Veículo',
    [AssetClass.Imovel]: 'Imóvel',
    [AssetClass.Consorcio]: 'Consórcio',
    [AssetClass.Moeda]: 'Moeda',
    [AssetClass.Exterior]: 'Exterior',
    [AssetClass.Milhas]: 'Milhas',
    [AssetClass.Cripto]: 'Cripto',
    [AssetClass.Dinheiro]: 'Dinheiro (espécie)',
    [AssetClass.Fgts]: 'FGTS',
    [AssetClass.Titulo]: 'Títulos Públicos',
    [AssetClass.Etf]: 'ETF',
    [AssetClass.Bdr]: 'BDR',
    [AssetClass.CrowdfundingImob]: 'Crowdfunding Imob.',
    [AssetClass.CartaCredito]: 'Carta de Crédito',
    [AssetClass.Joias]: 'Joias',
    [AssetClass.Arte]: 'Obras de Arte',
    [AssetClass.PropriedadeIntelectual]: 'Propriedade Intelectual',
    [AssetClass.Negocio]: 'Negócio/Operação',
    [AssetClass.Outro]: 'Outro',
};

const ASSET_LIQUIDITY_LABELS: Record<AssetLiquidity, string> = {
    [AssetLiquidity.Liquido]: 'Líquido',
    [AssetLiquidity.Iliquido]: 'Ilíquido',
    [AssetLiquidity.Travado]: 'Travado',
    [AssetLiquidity.Contingente]: 'Contingente',
};

const ASSET_OWNERSHIP_LABELS: Record<AssetOwnership, string> = {
    [AssetOwnership.Alexandre]: 'Alexandre',
    [AssetOwnership.Esposa]: 'Esposa',
    [AssetOwnership.Conjunto]: 'Conjunto',
};

const ASSET_VALUATION_SOURCE_LABELS: Record<AssetValuationSource, string> = {
    [AssetValuationSource.Market]: 'Mercado',
    [AssetValuationSource.Indexed]: 'Indexado',
    [AssetValuationSource.ManualDepreciation]: 'Depreciação Manual',
    [AssetValuationSource.Manual]: 'Manual',
};

const LIQUIDITY_BADGE_CLASS: Record<AssetLiquidity, string> = {
    [AssetLiquidity.Liquido]: 'border-emerald-500/20 bg-emerald-500/10 text-emerald-400',
    [AssetLiquidity.Iliquido]: 'border-zinc-700/50 bg-zinc-800/50 text-zinc-400',
    [AssetLiquidity.Travado]: 'border-sky-500/20 bg-sky-500/10 text-sky-400',
    [AssetLiquidity.Contingente]: 'border-amber-500/20 bg-amber-500/10 text-amber-400',
};

const RISK_LABELS: Record<OpportunityRisk, string> = {
    baixo: 'Baixo risco',
    medio: 'Risco médio',
    alto: 'Alto risco',
};

const RISK_BADGE_CLASS: Record<OpportunityRisk, string> = {
    baixo: 'border-emerald-500/20 bg-emerald-500/10 text-emerald-400',
    medio: 'border-amber-500/20 bg-amber-500/10 text-amber-400',
    alto: 'border-rose-500/20 bg-rose-500/10 text-rose-400',
};

const ACTION_CATEGORY_LABELS: Record<NextActionCategory, string> = {
    aporte: 'Aporte',
    diversificar: 'Diversificar',
    rebalancear: 'Rebalancear',
    resgatar: 'Resgatar',
    adquirir: 'Adquirir',
};

const ACTION_CATEGORY_BADGE_CLASS: Record<NextActionCategory, string> = {
    aporte: 'border-emerald-500/20 bg-emerald-500/10 text-emerald-400',
    diversificar: 'border-sky-500/20 bg-sky-500/10 text-sky-400',
    rebalancear: 'border-teal-500/20 bg-teal-500/10 text-teal-400',
    resgatar: 'border-amber-500/20 bg-amber-500/10 text-amber-400',
    adquirir: 'border-violet-500/20 bg-violet-500/10 text-violet-400',
};

const RISK_PROFILE_OPTIONS = ['Conservador', 'Moderado', 'Arrojado', 'Agressivo'];

const EMPTY_CREATE: CreateAssetRequest = {
    name: '',
    class: AssetClass.RendaFixa,
    ownership: AssetOwnership.Alexandre,
    liquidity: AssetLiquidity.Liquido,
    currency: 'BRL',
    currentValue: 0,
    costBasis: undefined,
    acquiredAt: '',
    valuationSource: AssetValuationSource.Manual,
    notes: '',
};

function formatMoney(value: number, currency: string = 'BRL') {
    if (!currency || currency === 'BRL') return formatCurrency(value);
    try {
        return new Intl.NumberFormat('pt-BR', { style: 'currency', currency }).format(value);
    } catch {
        return `${currency} ${value.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`;
    }
}

function toIsoDateOrUndefined(d?: string) {
    if (!d) return undefined;
    const p = new Date(d);
    return isNaN(p.getTime()) ? undefined : p.toISOString();
}

// ─── Page ───────────────────────────────────────────────────────────────────

export default function PatrimonioPage() {
    const qc = useQueryClient();
    const { showConfirm, confirmDialogNode } = useConfirmDialog();

    const {
        data: assets = [],
        isLoading: assetsLoading,
        isError: assetsError,
    } = useQuery({
        queryKey: ASSETS_KEY,
        queryFn: () => patrimonioService.listAssets(),
    });

    const {
        data: netWorth,
        isLoading: netWorthLoading,
        isError: netWorthError,
    } = useQuery<NetWorthSummary>({
        queryKey: NET_WORTH_KEY,
        queryFn: () => patrimonioService.getNetWorth(),
    });

    const createMutation = useMutation({
        mutationFn: (data: CreateAssetRequest) => patrimonioService.createAsset(data),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ASSETS_KEY });
            qc.invalidateQueries({ queryKey: NET_WORTH_KEY });
        },
    });

    const updateMutation = useMutation({
        mutationFn: ({ id, data }: { id: string; data: UpdateAssetRequest }) =>
            patrimonioService.updateAsset(id, data),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ASSETS_KEY });
            qc.invalidateQueries({ queryKey: NET_WORTH_KEY });
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => patrimonioService.deleteAsset(id),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ASSETS_KEY });
            qc.invalidateQueries({ queryKey: NET_WORTH_KEY });
        },
    });

    const saving = createMutation.isPending || updateMutation.isPending || deleteMutation.isPending;

    // ── Zona 2 — Onde investir ─────────────────────────────────────────────
    const {
        data: opportunities = [],
        isLoading: opportunitiesLoading,
        isError: opportunitiesError,
    } = useQuery({
        queryKey: OPPORTUNITIES_KEY,
        queryFn: () => patrimonioService.getOpportunities(),
    });

    const refreshOpportunitiesMutation = useMutation({
        mutationFn: () => patrimonioService.refreshOpportunities(),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: OPPORTUNITIES_KEY });
            toast.success('Oportunidades atualizadas');
        },
        onError: () => toast.error('Erro ao atualizar oportunidades'),
    });

    // ── Zona 3 — Ações a tomar ──────────────────────────────────────────────
    const {
        data: actions = [],
        isLoading: actionsLoading,
        isError: actionsError,
    } = useQuery({
        queryKey: ACTIONS_KEY,
        queryFn: () => patrimonioService.getActions(),
    });

    const generateActionsMutation = useMutation({
        mutationFn: () => patrimonioService.generateActions(),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ACTIONS_KEY });
            toast.success('Plano de ação gerado');
        },
        onError: () => toast.error('Erro ao gerar ações'),
    });

    const completeActionMutation = useMutation({
        mutationFn: (id: string) => patrimonioService.completeAction(id),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ACTIONS_KEY });
            toast.success('Ação concluída');
        },
        onError: () => toast.error('Erro ao concluir ação'),
    });

    // ── Perfil & Metas ───────────────────────────────────────────────────────
    const { data: profile, isLoading: profileLoading } = useQuery({
        queryKey: PROFILE_KEY,
        queryFn: () => patrimonioService.getProfile(),
    });

    const updateProfileMutation = useMutation({
        mutationFn: (data: UpdateWealthProfileRequest) => patrimonioService.updateProfile(data),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: PROFILE_KEY });
        },
    });

    const [showProfileEdit, setShowProfileEdit] = useState(false);
    const [profileForm, setProfileForm] = useState<UpdateWealthProfileRequest | null>(null);

    const openProfileEdit = () => {
        setProfileForm({
            riskProfile: profile?.riskProfile ?? 'Moderado',
            goalAmount: profile?.goalAmount,
            goalYears: profile?.goalYears,
            targetAllocationJson: profile?.targetAllocationJson,
        });
        setShowProfileEdit(true);
    };

    const handleProfileSave = () => {
        if (!profileForm || !profileForm.riskProfile.trim()) {
            toast.error('Selecione um perfil de risco');
            return;
        }
        updateProfileMutation.mutate(profileForm, {
            onSuccess: () => {
                toast.success('Perfil e metas atualizados');
                setShowProfileEdit(false);
            },
            onError: () => toast.error('Erro ao atualizar perfil'),
        });
    };

    // ── Modals ─────────────────────────────────────────────────────────────
    const [showCreate, setShowCreate] = useState(false);
    const [editAsset, setEditAsset] = useState<Asset | null>(null);

    const [createForm, setCreateForm] = useState<CreateAssetRequest>(EMPTY_CREATE);
    const [editForm, setEditForm] = useState<UpdateAssetRequest | null>(null);

    const handleCreate = () => {
        if (!createForm.name.trim() || createForm.currentValue <= 0) {
            toast.error('Preencha nome e valor atual do ativo');
            return;
        }
        createMutation.mutate(
            { ...createForm, acquiredAt: toIsoDateOrUndefined(createForm.acquiredAt) },
            {
                onSuccess: () => {
                    toast.success('Ativo cadastrado com sucesso');
                    setShowCreate(false);
                    setCreateForm(EMPTY_CREATE);
                },
                onError: () => toast.error('Erro ao cadastrar ativo'),
            },
        );
    };

    const openEdit = (asset: Asset) => {
        setEditAsset(asset);
        setEditForm({
            name: asset.name,
            class: asset.class,
            ownership: asset.ownership,
            liquidity: asset.liquidity,
            currency: asset.currency,
            currentValue: asset.currentValue,
            costBasis: asset.costBasis,
            acquiredAt: asset.acquiredAt ? asset.acquiredAt.slice(0, 10) : '',
            valuationSource: asset.valuationSource,
            notes: asset.notes ?? '',
        });
    };

    const handleEdit = () => {
        if (!editAsset || !editForm) return;
        if (!editForm.name?.trim() || (editForm.currentValue ?? 0) <= 0) {
            toast.error('Preencha nome e valor atual do ativo');
            return;
        }
        updateMutation.mutate(
            { id: editAsset.id, data: { ...editForm, acquiredAt: toIsoDateOrUndefined(editForm.acquiredAt) } },
            {
                onSuccess: () => {
                    toast.success('Ativo atualizado');
                    setEditAsset(null);
                    setEditForm(null);
                },
                onError: () => toast.error('Erro ao atualizar ativo'),
            },
        );
    };

    const handleDelete = (asset: Asset) => {
        showConfirm(
            `Tem certeza que deseja excluir o ativo "${asset.name}"? Esta ação não pode ser desfeita.`,
            () => {
                deleteMutation.mutate(asset.id, {
                    onSuccess: () => toast.success('Ativo excluído com sucesso'),
                    onError: () => toast.error('Erro ao excluir ativo'),
                });
            },
        );
    };

    const isLoading = assetsLoading || netWorthLoading;
    const isError = assetsError || netWorthError;

    if (isLoading) {
        return (
            <div className="container mx-auto px-4 py-6 max-w-7xl">
                <FinanceNav />
                <div className="flex flex-col items-center justify-center py-20 gap-4">
                    <Loader2 className="h-8 w-8 animate-spin text-emerald-400" />
                    <p className="text-zinc-400 text-sm animate-pulse">Carregando patrimônio...</p>
                </div>
            </div>
        );
    }

    const sortedByClass = netWorth ? [...netWorth.byClass].sort((a, b) => b.totalBRL - a.totalBRL) : [];
    const sortedByOwnership = netWorth ? [...netWorth.byOwnership].sort((a, b) => b.totalBRL - a.totalBRL) : [];
    const sortedByLiquidity = netWorth ? [...netWorth.byLiquidity].sort((a, b) => b.totalBRL - a.totalBRL) : [];

    const sortByEaseThenScore = (a: Opportunity, b: Opportunity) => {
        const ea = a.easeRank ?? Number.MAX_SAFE_INTEGER;
        const eb = b.easeRank ?? Number.MAX_SAFE_INTEGER;
        if (ea !== eb) return ea - eb;
        return b.score - a.score;
    };
    const TANGIBLE_CLASSES: AssetClass[] = [
        AssetClass.Imovel, AssetClass.Ouro, AssetClass.Joias,
        AssetClass.Veiculo, AssetClass.Arte, AssetClass.Diamante,
    ];
    const isTangible = (c: AssetClass) => TANGIBLE_CLASSES.includes(c);
    const investiqOpportunities = opportunities.filter((o) => o.source === 'investiq').sort(sortByEaseThenScore);
    const tangibleOpportunities = opportunities.filter((o) => isTangible(o.class)).sort(sortByEaseThenScore);
    const ideaOpportunities = opportunities.filter((o) => o.source === 'idea' && !isTangible(o.class)).sort(sortByEaseThenScore);

    const pendingActions = actions.filter((a) => a.status === 'pending');
    const resolvedActions = actions.filter((a) => a.status !== 'pending');

    return (
        <div className="container mx-auto px-4 py-6 max-w-7xl">
            {confirmDialogNode}
            <FinanceNav />

            {/* ── Header ── */}
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-8">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
                        <Gem className="h-6 w-6 text-teal-400" />
                        Patrimônio & Investimentos
                    </h1>
                    <p className="text-zinc-400 text-sm mt-1">
                        Visão consolidada do seu patrimônio: ativos líquidos, ilíquidos e contingentes
                    </p>
                </div>
                <Button
                    onClick={() => setShowCreate(true)}
                    className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold transition-all duration-300 gap-2 rounded-xl"
                >
                    <Plus className="h-4 w-4" />
                    Novo Ativo
                </Button>
            </div>

            {isError && (
                <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-4 rounded-xl mb-6 flex items-center gap-2 text-sm font-semibold">
                    <XCircle className="h-4 w-4 shrink-0" />
                    Erro ao carregar dados de patrimônio. Por favor, tente novamente.
                </div>
            )}

            {/* ── Perfil & Metas ── */}
            <div className="rounded-2xl p-5 bg-[#0a130f]/40 border border-zinc-800/60 mb-8 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="flex flex-wrap items-center gap-x-8 gap-y-3">
                    <div className="flex items-center gap-2.5">
                        <div className="p-2 rounded-lg bg-teal-500/10">
                            <Target className="h-4 w-4 text-teal-400" />
                        </div>
                        <div>
                            <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-400 block">Perfil de Risco</span>
                            {profileLoading ? (
                                <span className="text-sm text-zinc-500">Carregando...</span>
                            ) : (
                                <span className="text-sm font-semibold text-zinc-100">{profile?.riskProfile ?? 'Não definido'}</span>
                            )}
                        </div>
                    </div>
                    <div className="flex items-center gap-2.5">
                        <div className="p-2 rounded-lg bg-emerald-500/10">
                            <CircleDollarSign className="h-4 w-4 text-emerald-400" />
                        </div>
                        <div>
                            <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-400 block">Meta</span>
                            {profileLoading ? (
                                <span className="text-sm text-zinc-500">Carregando...</span>
                            ) : profile?.goalAmount ? (
                                <span className="text-sm font-semibold text-zinc-100">
                                    {formatCurrency(profile.goalAmount)}
                                    {profile.goalYears ? ` em ${profile.goalYears} ${profile.goalYears === 1 ? 'ano' : 'anos'}` : ''}
                                </span>
                            ) : (
                                <span className="text-sm text-zinc-500">Nenhuma meta definida</span>
                            )}
                        </div>
                    </div>
                </div>
                <Button variant="outline" size="sm" onClick={openProfileEdit} className="gap-2 shrink-0">
                    <Pencil className="h-3.5 w-3.5" />
                    Editar Perfil & Metas
                </Button>
            </div>

            {/* ── Stat cards ── */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
                <div className="relative overflow-hidden rounded-2xl p-6 bg-[#0a130f]/60 backdrop-blur-md border border-zinc-800/80">
                    <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
                    <div className="flex items-center gap-2 mb-3">
                        <div className="p-2 rounded-lg bg-emerald-500/10">
                            <Wallet className="h-4 w-4 text-emerald-400" />
                        </div>
                        <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-400">Patrimônio Realizável</span>
                    </div>
                    <span className="text-2xl font-bold tracking-tight tabular-nums text-emerald-400">
                        {formatCurrency(netWorth?.totalRealizableBRL ?? 0)}
                    </span>
                    <p className="text-xs text-zinc-500 mt-1">Ativos líquidos disponíveis</p>
                </div>

                <div className="relative overflow-hidden rounded-2xl p-6 bg-[#0a130f]/60 backdrop-blur-md border border-zinc-800/80">
                    <div className="absolute inset-0 bg-gradient-to-br from-amber-500/5 to-transparent pointer-events-none" />
                    <div className="flex items-center gap-2 mb-3">
                        <div className="p-2 rounded-lg bg-amber-500/10">
                            <Sparkles className="h-4 w-4 text-amber-400" />
                        </div>
                        <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-400">Contingente / A Receber</span>
                    </div>
                    <span className="text-2xl font-bold tracking-tight tabular-nums text-amber-400">
                        {formatCurrency(netWorth?.totalContingentBRL ?? 0)}
                    </span>
                    <p className="text-xs text-zinc-500 mt-1">Valores condicionais ou pendentes</p>
                </div>

                <div className="relative overflow-hidden rounded-2xl p-6 bg-[#0a130f]/60 backdrop-blur-md border border-zinc-800/80">
                    <div className="absolute inset-0 bg-gradient-to-br from-teal-500/5 to-transparent pointer-events-none" />
                    <div className="flex items-center gap-2 mb-3">
                        <div className="p-2 rounded-lg bg-teal-500/10">
                            <Gem className="h-4 w-4 text-teal-400" />
                        </div>
                        <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-400">Patrimônio Projetado</span>
                    </div>
                    <span className="text-2xl font-bold tracking-tight tabular-nums text-teal-400">
                        {formatCurrency(netWorth?.totalProjectedBRL ?? 0)}
                    </span>
                    <p className="text-xs text-zinc-500 mt-1">Realizável + contingente</p>
                </div>
            </div>

            {/* ── Allocation sections ── */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
                <div className="rounded-2xl p-6 bg-[#0a130f]/40 border border-zinc-800/60">
                    <div className="flex items-center gap-2 mb-4">
                        <PieChart className="h-4 w-4 text-zinc-400" />
                        <h2 className="text-sm font-semibold text-zinc-100">Alocação por Classe</h2>
                    </div>
                    {sortedByClass.length === 0 ? (
                        <p className="text-xs text-zinc-500">Sem ativos cadastrados ainda.</p>
                    ) : (
                        <div className="space-y-3">
                            {sortedByClass.map((item) => (
                                <div key={item.class} className="space-y-1">
                                    <div className="flex items-center justify-between text-xs">
                                        <span className="text-zinc-300 font-medium">{ASSET_CLASS_LABELS[item.class]}</span>
                                        <span className="text-zinc-500 tabular-nums">
                                            {formatCurrency(item.totalBRL)} · {item.pct.toFixed(1)}%
                                        </span>
                                    </div>
                                    <div className="h-1.5 w-full rounded-full bg-white/5 overflow-hidden">
                                        <div
                                            className="h-full rounded-full bg-teal-400"
                                            style={{ width: `${Math.min(item.pct, 100)}%` }}
                                        />
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                <div className="rounded-2xl p-6 bg-[#0a130f]/40 border border-zinc-800/60">
                    <div className="flex items-center gap-2 mb-4">
                        <Users className="h-4 w-4 text-zinc-400" />
                        <h2 className="text-sm font-semibold text-zinc-100">Titularidade</h2>
                    </div>
                    {sortedByOwnership.length === 0 ? (
                        <p className="text-xs text-zinc-500">Sem ativos cadastrados ainda.</p>
                    ) : (
                        <div className="space-y-3">
                            {sortedByOwnership.map((item) => (
                                <div key={item.ownership} className="flex items-center justify-between py-1.5 border-b border-white/5 last:border-0">
                                    <span className="text-xs text-zinc-300 font-medium">{ASSET_OWNERSHIP_LABELS[item.ownership]}</span>
                                    <span className="text-sm font-semibold text-zinc-100 tabular-nums">{formatCurrency(item.totalBRL)}</span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                <div className="rounded-2xl p-6 bg-[#0a130f]/40 border border-zinc-800/60">
                    <div className="flex items-center gap-2 mb-4">
                        <Layers className="h-4 w-4 text-zinc-400" />
                        <h2 className="text-sm font-semibold text-zinc-100">Liquidez</h2>
                    </div>
                    {sortedByLiquidity.length === 0 ? (
                        <p className="text-xs text-zinc-500">Sem ativos cadastrados ainda.</p>
                    ) : (
                        <div className="space-y-3">
                            {sortedByLiquidity.map((item) => (
                                <div key={item.liquidity} className="flex items-center justify-between py-1.5 border-b border-white/5 last:border-0">
                                    <Badge className={cn('font-semibold', LIQUIDITY_BADGE_CLASS[item.liquidity])}>
                                        {ASSET_LIQUIDITY_LABELS[item.liquidity]}
                                    </Badge>
                                    <span className="text-sm font-semibold text-zinc-100 tabular-nums">{formatCurrency(item.totalBRL)}</span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>

            {/* ── Assets table ── */}
            <div className="rounded-2xl overflow-hidden border border-zinc-800/60 bg-[#0a130f]/40 mb-8">
                <div className="px-6 py-4 flex items-center justify-between border-b border-zinc-800/60">
                    <h2 className="text-sm font-semibold text-zinc-100">Ativos</h2>
                    <span className="h-5 px-2 rounded-full bg-emerald-500/10 text-emerald-400 text-[10px] flex items-center justify-center font-semibold border border-emerald-500/20">
                        {assets.length}
                    </span>
                </div>

                {assets.length === 0 ? (
                    <div className="p-6">
                        <EmptyState
                            title="Nenhum ativo cadastrado"
                            description="Cadastre ações, FIIs, ouro, imóveis, criptomoedas e outros ativos para acompanhar seu patrimônio consolidado."
                            icon={Gem}
                            actionLabel="Novo Ativo"
                            onAction={() => setShowCreate(true)}
                        />
                    </div>
                ) : (
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Nome</TableHead>
                                <TableHead>Classe</TableHead>
                                <TableHead>Titular</TableHead>
                                <TableHead>Liquidez</TableHead>
                                <TableHead>Moeda</TableHead>
                                <TableHead className="text-right">Valor Atual</TableHead>
                                <TableHead className="text-right">Custo</TableHead>
                                <TableHead className="text-right">Ações</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {assets.map((asset) => {
                                const isContingent = asset.liquidity === AssetLiquidity.Contingente;
                                return (
                                    <TableRow
                                        key={asset.id}
                                        className={cn(isContingent && 'bg-amber-500/[0.04] hover:bg-amber-500/[0.07]')}
                                    >
                                        <TableCell className="font-medium text-zinc-100">
                                            <div className="flex items-center gap-2">
                                                {asset.name}
                                                {isContingent && (
                                                    <Badge className="border-amber-500/20 bg-amber-500/10 text-amber-400 text-[10px]">
                                                        A receber
                                                    </Badge>
                                                )}
                                            </div>
                                        </TableCell>
                                        <TableCell>{ASSET_CLASS_LABELS[asset.class]}</TableCell>
                                        <TableCell>{ASSET_OWNERSHIP_LABELS[asset.ownership]}</TableCell>
                                        <TableCell>
                                            <Badge className={cn('font-semibold', LIQUIDITY_BADGE_CLASS[asset.liquidity])}>
                                                {ASSET_LIQUIDITY_LABELS[asset.liquidity]}
                                            </Badge>
                                        </TableCell>
                                        <TableCell>
                                            {asset.currency && asset.currency !== 'BRL' ? (
                                                <Badge variant="outline" className="text-zinc-300 border-zinc-700">
                                                    {asset.currency}
                                                </Badge>
                                            ) : (
                                                <span className="text-zinc-500 text-xs">BRL</span>
                                            )}
                                        </TableCell>
                                        <TableCell className="text-right tabular-nums font-semibold text-zinc-100">
                                            {formatMoney(asset.currentValue, asset.currency)}
                                        </TableCell>
                                        <TableCell className="text-right tabular-nums text-zinc-400">
                                            {asset.costBasis !== undefined && asset.costBasis !== null
                                                ? formatMoney(asset.costBasis, asset.currency)
                                                : '—'}
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <div className="flex items-center justify-end gap-1">
                                                <button
                                                    onClick={() => openEdit(asset)}
                                                    className="p-2 rounded-lg text-zinc-400 hover:text-emerald-400 hover:bg-zinc-900/50 transition-colors"
                                                >
                                                    <Pencil className="h-4 w-4" />
                                                    <span className="sr-only">Editar</span>
                                                </button>
                                                <button
                                                    onClick={() => handleDelete(asset)}
                                                    disabled={saving}
                                                    className="p-2 rounded-lg text-zinc-400 hover:text-rose-400 hover:bg-zinc-900/50 transition-colors disabled:opacity-50"
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                    <span className="sr-only">Excluir</span>
                                                </button>
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                );
                            })}
                        </TableBody>
                    </Table>
                )}
            </div>

            {/* ── Zona 2 — Onde investir ── */}
            <div className="rounded-2xl p-6 bg-[#0a130f]/40 border border-zinc-800/60 mb-8">
                <div className="flex items-center justify-between mb-4 flex-wrap gap-3">
                    <div className="flex items-center gap-2">
                        <Compass className="h-4 w-4 text-teal-400" />
                        <h2 className="text-sm font-semibold text-zinc-100">Onde investir</h2>
                    </div>
                    <Button
                        variant="outline"
                        size="sm"
                        onClick={() => refreshOpportunitiesMutation.mutate()}
                        disabled={refreshOpportunitiesMutation.isPending}
                        className="gap-2"
                    >
                        {refreshOpportunitiesMutation.isPending
                            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            : <RefreshCw className="h-3.5 w-3.5" />}
                        Atualizar oportunidades
                    </Button>
                </div>

                {opportunitiesLoading ? (
                    <div className="flex items-center justify-center py-10">
                        <Loader2 className="h-5 w-5 animate-spin text-emerald-400" />
                    </div>
                ) : opportunitiesError ? (
                    <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-4 rounded-xl flex items-center gap-2 text-sm font-semibold">
                        <XCircle className="h-4 w-4 shrink-0" />
                        Erro ao carregar oportunidades. Por favor, tente novamente.
                    </div>
                ) : (
                    <div className="space-y-8">
                        {/* 🏆 Bens palpáveis (imóvel, ouro, joias, carro, arte) */}
                        {tangibleOpportunities.length > 0 && (
                            <div>
                                <h3 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-3 flex items-center gap-1.5">
                                    <Gem className="h-3.5 w-3.5 text-teal-400" />
                                    Bens palpáveis
                                </h3>
                                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                                    {tangibleOpportunities.map((opp) => (
                                        <OpportunityCard key={opp.id} opportunity={opp} />
                                    ))}
                                </div>
                            </div>
                        )}

                        {/* Oportunidades do dia (InvestIQ) */}
                        <div>
                            <h3 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-3">
                                Oportunidades do dia
                            </h3>
                            {investiqOpportunities.length === 0 ? (
                                <p className="text-xs text-zinc-500 bg-zinc-900/30 border border-dashed border-zinc-800 rounded-xl p-4">
                                    Conecte o InvestIQ para oportunidades de mercado ao vivo.
                                </p>
                            ) : (
                                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                                    {investiqOpportunities.map((opp) => (
                                        <OpportunityCard key={opp.id} opportunity={opp} />
                                    ))}
                                </div>
                            )}
                        </div>

                        {/* Ideias pra você */}
                        <div>
                            <h3 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-3 flex items-center gap-1.5">
                                <Lightbulb className="h-3.5 w-3.5 text-amber-400" />
                                Ideias pra você
                            </h3>
                            {ideaOpportunities.length === 0 ? (
                                <EmptyState
                                    icon={Lightbulb}
                                    title="Sem ideias no momento"
                                    description="Cadastre mais ativos e defina sua meta para receber ideias personalizadas de diversificação."
                                />
                            ) : (
                                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                                    {ideaOpportunities.map((opp) => (
                                        <OpportunityCard key={opp.id} opportunity={opp} />
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </div>

            {/* ── Zona 3 — Ações a tomar ── */}
            <div className="rounded-2xl p-6 bg-[#0a130f]/40 border border-zinc-800/60 mb-8">
                <div className="flex items-center justify-between mb-4 flex-wrap gap-3">
                    <div className="flex items-center gap-2">
                        <ListChecks className="h-4 w-4 text-teal-400" />
                        <h2 className="text-sm font-semibold text-zinc-100">Ações a tomar</h2>
                        {pendingActions.length > 0 && (
                            <span className="h-5 px-2 rounded-full bg-emerald-500/10 text-emerald-400 text-[10px] flex items-center justify-center font-semibold border border-emerald-500/20">
                                {pendingActions.length}
                            </span>
                        )}
                    </div>
                    <Button
                        variant="outline"
                        size="sm"
                        onClick={() => generateActionsMutation.mutate()}
                        disabled={generateActionsMutation.isPending}
                        className="gap-2"
                    >
                        {generateActionsMutation.isPending
                            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            : <Sparkles className="h-3.5 w-3.5" />}
                        Gerar ações
                    </Button>
                </div>

                {actionsLoading ? (
                    <div className="flex items-center justify-center py-10">
                        <Loader2 className="h-5 w-5 animate-spin text-emerald-400" />
                    </div>
                ) : actionsError ? (
                    <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-4 rounded-xl flex items-center gap-2 text-sm font-semibold">
                        <XCircle className="h-4 w-4 shrink-0" />
                        Erro ao carregar ações. Por favor, tente novamente.
                    </div>
                ) : actions.length === 0 ? (
                    <EmptyState
                        icon={ListChecks}
                        title="Nenhuma ação pendente"
                        description="Cadastre seus ativos e defina sua meta para gerar o plano de ação"
                        actionLabel="Gerar ações"
                        onAction={() => generateActionsMutation.mutate()}
                    />
                ) : (
                    <div className="space-y-2">
                        {pendingActions.map((action) => (
                            <NextActionRow
                                key={action.id}
                                action={action}
                                onComplete={() => completeActionMutation.mutate(action.id)}
                                completing={completeActionMutation.isPending && completeActionMutation.variables === action.id}
                            />
                        ))}
                        {resolvedActions.map((action) => (
                            <NextActionRow key={action.id} action={action} resolved />
                        ))}
                    </div>
                )}
            </div>

            {/* ── Create Modal ── */}
            <Dialog
                open={showCreate}
                onOpenChange={(v) => {
                    if (!saving) {
                        setShowCreate(v);
                        if (!v) setCreateForm(EMPTY_CREATE);
                    }
                }}
            >
                <DialogContent className="sm:max-w-lg">
                    <DialogHeader>
                        <DialogTitle>Novo Ativo</DialogTitle>
                    </DialogHeader>
                    <AssetForm
                        form={createForm}
                        onChange={(field, value) => setCreateForm((f) => ({ ...f, [field]: value }))}
                    />
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setShowCreate(false)} disabled={saving}>
                            Cancelar
                        </Button>
                        <Button onClick={handleCreate} disabled={saving}>
                            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                            Criar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* ── Edit Modal ── */}
            <Dialog
                open={!!editAsset}
                onOpenChange={(v) => {
                    if (!saving && !v) {
                        setEditAsset(null);
                        setEditForm(null);
                    }
                }}
            >
                <DialogContent className="sm:max-w-lg">
                    <DialogHeader>
                        <DialogTitle>Editar Ativo</DialogTitle>
                    </DialogHeader>
                    {editForm && (
                        <AssetForm
                            form={editForm}
                            onChange={(field, value) => setEditForm((f) => (f ? { ...f, [field]: value } : f))}
                        />
                    )}
                    <DialogFooter>
                        <Button variant="outline" onClick={() => { setEditAsset(null); setEditForm(null); }} disabled={saving}>
                            Cancelar
                        </Button>
                        <Button onClick={handleEdit} disabled={saving}>
                            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                            Salvar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* ── Perfil & Metas Modal ── */}
            <Dialog
                open={showProfileEdit}
                onOpenChange={(v) => {
                    if (!updateProfileMutation.isPending) {
                        setShowProfileEdit(v);
                        if (!v) setProfileForm(null);
                    }
                }}
            >
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>Perfil & Metas</DialogTitle>
                    </DialogHeader>
                    {profileForm && (
                        <div className="space-y-3 py-2">
                            <div className="space-y-1.5">
                                <Label htmlFor="pf-risk">Perfil de Risco</Label>
                                <Select
                                    value={profileForm.riskProfile}
                                    onValueChange={(v) => setProfileForm((f) => (f ? { ...f, riskProfile: v } : f))}
                                >
                                    <SelectTrigger id="pf-risk"><SelectValue /></SelectTrigger>
                                    <SelectContent>
                                        {RISK_PROFILE_OPTIONS.map((opt) => (
                                            <SelectItem key={opt} value={opt}>{opt}</SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <div className="grid grid-cols-2 gap-3">
                                <div className="space-y-1.5">
                                    <Label htmlFor="pf-amount">Meta (R$)</Label>
                                    <Input
                                        id="pf-amount"
                                        type="number"
                                        min="0"
                                        step="0.01"
                                        value={profileForm.goalAmount ?? ''}
                                        onChange={(e) => setProfileForm((f) => (f ? {
                                            ...f,
                                            goalAmount: e.target.value === '' ? undefined : parseFloat(e.target.value) || 0,
                                        } : f))}
                                        placeholder="Ex: 1000000"
                                    />
                                </div>
                                <div className="space-y-1.5">
                                    <Label htmlFor="pf-years">Prazo (anos)</Label>
                                    <Input
                                        id="pf-years"
                                        type="number"
                                        min="0"
                                        step="1"
                                        value={profileForm.goalYears ?? ''}
                                        onChange={(e) => setProfileForm((f) => (f ? {
                                            ...f,
                                            goalYears: e.target.value === '' ? undefined : parseInt(e.target.value, 10) || 0,
                                        } : f))}
                                        placeholder="Ex: 10"
                                    />
                                </div>
                            </div>
                        </div>
                    )}
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setShowProfileEdit(false)} disabled={updateProfileMutation.isPending}>
                            Cancelar
                        </Button>
                        <Button onClick={handleProfileSave} disabled={updateProfileMutation.isPending}>
                            {updateProfileMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                            Salvar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}

// ─── Shared AssetForm ────────────────────────────────────────────────────────

interface AssetFormValue {
    name?: string;
    class?: AssetClass;
    ownership?: AssetOwnership;
    liquidity?: AssetLiquidity;
    currency?: string;
    currentValue?: number;
    costBasis?: number;
    acquiredAt?: string;
    valuationSource?: AssetValuationSource;
    notes?: string;
}

interface AssetFormProps {
    form: AssetFormValue;
    onChange: (field: string, value: string | number | undefined) => void;
}

function AssetForm({ form, onChange }: AssetFormProps) {
    return (
        <div className="space-y-3 py-2">
            <div className="space-y-1.5">
                <Label htmlFor="af-name">Nome</Label>
                <Input
                    id="af-name"
                    value={form.name ?? ''}
                    onChange={(e) => onChange('name', e.target.value)}
                    placeholder="Ex: Tesouro Selic 2029"
                />
            </div>

            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                    <Label htmlFor="af-class">Classe</Label>
                    <Select
                        value={form.class !== undefined ? String(form.class) : undefined}
                        onValueChange={(v) => onChange('class', parseInt(v, 10))}
                    >
                        <SelectTrigger id="af-class"><SelectValue /></SelectTrigger>
                        <SelectContent>
                            {Object.entries(ASSET_CLASS_LABELS).map(([value, label]) => (
                                <SelectItem key={value} value={value}>{label}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                <div className="space-y-1.5">
                    <Label htmlFor="af-ownership">Titular</Label>
                    <Select
                        value={form.ownership !== undefined ? String(form.ownership) : undefined}
                        onValueChange={(v) => onChange('ownership', parseInt(v, 10))}
                    >
                        <SelectTrigger id="af-ownership"><SelectValue /></SelectTrigger>
                        <SelectContent>
                            {Object.entries(ASSET_OWNERSHIP_LABELS).map(([value, label]) => (
                                <SelectItem key={value} value={value}>{label}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                    <Label htmlFor="af-liquidity">Liquidez</Label>
                    <Select
                        value={form.liquidity !== undefined ? String(form.liquidity) : undefined}
                        onValueChange={(v) => onChange('liquidity', parseInt(v, 10))}
                    >
                        <SelectTrigger id="af-liquidity"><SelectValue /></SelectTrigger>
                        <SelectContent>
                            {Object.entries(ASSET_LIQUIDITY_LABELS).map(([value, label]) => (
                                <SelectItem key={value} value={value}>{label}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                <div className="space-y-1.5">
                    <Label htmlFor="af-valuation">Fonte de Avaliação</Label>
                    <Select
                        value={form.valuationSource !== undefined ? String(form.valuationSource) : undefined}
                        onValueChange={(v) => onChange('valuationSource', parseInt(v, 10))}
                    >
                        <SelectTrigger id="af-valuation"><SelectValue /></SelectTrigger>
                        <SelectContent>
                            {Object.entries(ASSET_VALUATION_SOURCE_LABELS).map(([value, label]) => (
                                <SelectItem key={value} value={value}>{label}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
                <div className="space-y-1.5">
                    <Label htmlFor="af-currency">Moeda</Label>
                    <Input
                        id="af-currency"
                        value={form.currency ?? 'BRL'}
                        onChange={(e) => onChange('currency', e.target.value.toUpperCase())}
                        maxLength={3}
                        placeholder="BRL"
                    />
                </div>
                <div className="space-y-1.5">
                    <Label htmlFor="af-value">Valor Atual</Label>
                    <Input
                        id="af-value"
                        type="number"
                        min="0"
                        step="0.01"
                        value={form.currentValue ?? ''}
                        onChange={(e) => onChange('currentValue', parseFloat(e.target.value) || 0)}
                    />
                </div>
                <div className="space-y-1.5">
                    <Label htmlFor="af-cost">Custo (opcional)</Label>
                    <Input
                        id="af-cost"
                        type="number"
                        min="0"
                        step="0.01"
                        value={form.costBasis ?? ''}
                        onChange={(e) => onChange('costBasis', e.target.value === '' ? undefined : parseFloat(e.target.value) || 0)}
                    />
                </div>
            </div>

            <div className="space-y-1.5">
                <Label htmlFor="af-acquired">Data de Aquisição (opcional)</Label>
                <Input
                    id="af-acquired"
                    type="date"
                    value={form.acquiredAt ?? ''}
                    onChange={(e) => onChange('acquiredAt', e.target.value)}
                />
            </div>

            <div className="space-y-1.5">
                <Label htmlFor="af-notes">Notas (opcional)</Label>
                <Textarea
                    id="af-notes"
                    value={form.notes ?? ''}
                    onChange={(e) => onChange('notes', e.target.value)}
                    placeholder="Observações sobre este ativo..."
                    rows={3}
                />
            </div>
        </div>
    );
}

// ─── Zona 2 — OpportunityCard ────────────────────────────────────────────────

function OpportunityCard({ opportunity }: { opportunity: Opportunity }) {
    return (
        <div className="rounded-xl p-4 bg-zinc-900/30 border border-zinc-800/60 hover:border-zinc-700/60 transition-colors flex flex-col gap-2.5">
            <div className="flex items-start justify-between gap-2">
                <div className="flex items-center gap-1.5 flex-wrap">
                    <Badge variant="outline" className="text-zinc-300 border-zinc-700 text-[10px]">
                        {ASSET_CLASS_LABELS[opportunity.class]}
                    </Badge>
                    {opportunity.ticker && (
                        <span className="text-[10px] font-mono font-semibold text-teal-400">{opportunity.ticker}</span>
                    )}
                </div>
                <Badge className={cn('font-semibold text-[10px] shrink-0', RISK_BADGE_CLASS[opportunity.risk])}>
                    {RISK_LABELS[opportunity.risk]}
                </Badge>
            </div>

            <h4 className="text-sm font-semibold text-zinc-100 leading-snug">{opportunity.title}</h4>
            <p className="text-xs text-zinc-400 leading-relaxed">{opportunity.thesis}</p>

            <div className="flex items-center justify-between pt-2 mt-auto border-t border-white/5">
                <span className="text-[10px] text-zinc-500">
                    Score <span className="font-semibold text-zinc-300 tabular-nums">{opportunity.score.toFixed(1)}</span>
                </span>
                {opportunity.suggestedAllocationPct !== undefined && opportunity.suggestedAllocationPct !== null && (
                    <span className="text-[10px] text-zinc-500">
                        Alocação sugerida <span className="font-semibold text-emerald-400 tabular-nums">{opportunity.suggestedAllocationPct.toFixed(1)}%</span>
                    </span>
                )}
            </div>
        </div>
    );
}

// ─── Zona 3 — NextActionRow ──────────────────────────────────────────────────

interface NextActionRowProps {
    action: NextAction;
    onComplete?: () => void;
    completing?: boolean;
    resolved?: boolean;
}

function NextActionRow({ action, onComplete, completing, resolved }: NextActionRowProps) {
    return (
        <div
            className={cn(
                'flex items-start justify-between gap-4 p-4 rounded-xl border transition-colors',
                resolved
                    ? 'bg-zinc-900/10 border-zinc-800/40 opacity-60'
                    : 'bg-zinc-900/30 border-zinc-800/60 hover:border-zinc-700/60',
            )}
        >
            <div className="flex-1 min-w-0 space-y-1.5">
                <div className="flex items-center gap-2 flex-wrap">
                    <Badge className={cn('font-semibold text-[10px]', ACTION_CATEGORY_BADGE_CLASS[action.category])}>
                        {ACTION_CATEGORY_LABELS[action.category]}
                    </Badge>
                    {action.targetClass !== undefined && action.targetClass !== null && (
                        <Badge variant="outline" className="text-zinc-300 border-zinc-700 text-[10px]">
                            {ASSET_CLASS_LABELS[action.targetClass]}
                        </Badge>
                    )}
                    {resolved && (
                        <span className="text-[10px] text-zinc-500 font-semibold">
                            {action.status === 'done' ? 'Concluída' : 'Dispensada'}
                        </span>
                    )}
                </div>
                <p className={cn('text-sm font-semibold text-zinc-100', resolved && 'line-through decoration-zinc-600')}>
                    {action.title}
                </p>
                <p className="text-xs text-zinc-400 leading-relaxed">{action.rationale}</p>
                {action.suggestedAmount !== undefined && action.suggestedAmount !== null && (
                    <p className="text-xs text-emerald-400 font-semibold tabular-nums">
                        {formatCurrency(action.suggestedAmount)}
                    </p>
                )}
            </div>
            {!resolved && onComplete && (
                <Button
                    variant="outline"
                    size="sm"
                    onClick={onComplete}
                    disabled={completing}
                    className="gap-1.5 shrink-0"
                >
                    {completing
                        ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                        : <CheckCircle2 className="h-3.5 w-3.5" />}
                    Concluir
                </Button>
            )}
        </div>
    );
}
