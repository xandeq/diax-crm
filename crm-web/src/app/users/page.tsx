'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { Badge } from '@/components/ui/badge';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { adminGroupsService, UserGroup } from '@/services/adminGroups';
import { userService } from '@/services/users';
import { UpdateUserRequest, UserResponse } from '@/types/users';
import { AlertCircle, Loader2, Plus, RefreshCw, Shield, Trash2, User, Users } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { toast } from 'sonner';

export default function UsersPage() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    groupKeys: [] as string[],
    isActive: true
  });
  const [editingId, setEditingId] = useState<string | null>(null);
  const { showConfirm, confirmDialogNode } = useConfirmDialog();

  const { data: users = [], isLoading, isError, error, refetch } = useQuery({
    queryKey: ['users'],
    queryFn: () => userService.getAll(),
  });

  const { data: groups = [] } = useQuery({
    queryKey: ['userGroups'],
    queryFn: () => adminGroupsService.getAll(),
  });

  const loading = isLoading;
  const errorMessage = isError ? (error instanceof Error ? error.message : 'Erro ao carregar dados. Verifique se você tem permissão.') : null;

  const saveMutation = useMutation({
    mutationFn: async (data: typeof formData & { id: string | null }) => {
      if (data.id) {
        const updateData: UpdateUserRequest = {
          isActive: data.isActive,
          groupKeys: data.groupKeys,
        };
        if (data.password.trim()) updateData.password = data.password;
        return userService.update(data.id, updateData);
      }
      return userService.create({
        email: data.email,
        password: data.password,
        groupKeys: data.groupKeys.length > 0 ? data.groupKeys : undefined,
      });
    },
    onSuccess: (_, variables) => {
      toast.success(variables.id ? 'Usuário atualizado com sucesso.' : 'Usuário criado com sucesso.');
      queryClient.invalidateQueries({ queryKey: ['users'] });
      resetForm();
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : 'Erro ao salvar usuário.';
      toast.error(msg);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => userService.delete(id),
    onSuccess: () => {
      toast.success('Usuário deletado.');
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : 'Erro ao deletar usuário.';
      toast.error(msg);
    },
  });

  const resetForm = () => {
    setFormData({ email: '', password: '', groupKeys: [], isActive: true });
    setEditingId(null);
    setIsFormOpen(false);
  };

  const handleEdit = (user: UserResponse) => {
    setEditingId(user.id);
    setFormData({
      email: user.email,
      password: '',
      groupKeys: user.groups || [],
      isActive: user.isActive
    });
    setIsFormOpen(true);
  };

  const toggleGroup = (groupKey: string, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      groupKeys: checked
        ? [...prev.groupKeys, groupKey]
        : prev.groupKeys.filter(k => k !== groupKey)
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    saveMutation.mutate({ ...formData, id: editingId });
  };

  const handleDelete = (user: UserResponse) => {
    showConfirm(`Tem certeza que deseja deletar o usuário ${user.email}?`, () => {
      deleteMutation.mutate(user.id);
    });
  };

  // Helper: resolve group key to group name
  const getGroupName = (key: string) => {
    const group = groups.find(g => g.key === key);
    return group?.name || key;
  };

  return (
    <RoleGuard allowedRoles={['Admin']}>
      {confirmDialogNode}
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold" style={{ color: '#F9FAFB' }}>Gerenciamento de Usuários</h1>
            <p className="mt-1" style={{ color: '#9CA3AF' }}>Administre contas, grupos e permissões do sistema</p>
          </div>
          <div className="flex gap-2">
            <Button onClick={() => refetch()} variant="outline" disabled={loading}>
              <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
              Atualizar
            </Button>
            <Button onClick={() => { resetForm(); setIsFormOpen(true); }} className="bg-indigo-600 hover:bg-indigo-700">
              <Plus className="h-4 w-4 mr-2" />
              Novo Usuário
            </Button>
          </div>
        </div>

        {errorMessage && (
          <div className="rounded-lg p-4 flex items-center gap-3" style={{ background: 'rgba(239,68,68,0.08)', border: '1px solid rgba(239,68,68,0.25)' }}>
            <AlertCircle className="h-5 w-5 text-red-400 flex-shrink-0" />
            <p className="text-sm flex-1" style={{ color: '#fca5a5' }}>{errorMessage}</p>
          </div>
        )}

        {/* Create / Edit Form */}
        {isFormOpen && (
          <div className="p-6 rounded-lg" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
            <h2 className="text-lg font-semibold mb-4" style={{ color: '#F9FAFB' }}>
              {editingId ? 'Editar Usuário' : 'Novo Usuário'}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-5">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="email">E-mail</Label>
                  <Input
                    id="email"
                    type="email"
                    autoComplete="email"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    disabled={!!editingId || saveMutation.isPending}
                    required
                    placeholder="usuario@email.com"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="password">
                    Senha {editingId && <span className="text-muted-foreground font-normal">(em branco para manter)</span>}
                  </Label>
                  <Input
                    id="password"
                    type="password"
                    autoComplete={editingId ? 'new-password' : 'new-password'}
                    value={formData.password}
                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    required={!editingId}
                    disabled={saveMutation.isPending}
                    placeholder={editingId ? '••••••••' : 'Senha segura'}
                  />
                </div>
              </div>

              {/* Groups selection */}
              <div className="space-y-3">
                <Label className="flex items-center gap-2">
                  <Users className="h-4 w-4" />
                  Grupos
                </Label>
                <div className="rounded-lg p-4 space-y-3" style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)' }}>
                  {groups.length === 0 ? (
                    <p className="text-sm text-muted-foreground">Nenhum grupo cadastrado. Crie um em Grupos & Permissões.</p>
                  ) : (
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                      {groups.filter(g => g.key).map((group) => {
                        const groupKey = group.key;
                        const isChecked = formData.groupKeys.includes(groupKey);
                        return (
                          <div
                            key={group.id}
                            className="flex items-start space-x-3 p-3 rounded-md border transition-colors"
                            style={{
                              background: isChecked ? 'rgba(99,102,241,0.12)' : 'rgba(255,255,255,0.03)',
                              borderColor: isChecked ? 'rgba(99,102,241,0.4)' : 'rgba(255,255,255,0.09)'
                            }}
                          >
                            <Checkbox
                              id={`group-${group.id}`}
                              checked={isChecked}
                              onCheckedChange={(checked) => toggleGroup(groupKey, checked === true)}
                              disabled={saveMutation.isPending}
                            />
                            <div className="flex-1">
                              <Label
                                htmlFor={`group-${group.id}`}
                                className="text-sm font-medium cursor-pointer leading-none"
                              >
                                {group.name}
                              </Label>
                              {group.description && (
                                <p className="text-xs text-muted-foreground mt-1">{group.description}</p>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>

              {/* Active toggle */}
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isActive"
                  checked={formData.isActive}
                  onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked === true })}
                  disabled={saveMutation.isPending}
                />
                <Label htmlFor="isActive" className="cursor-pointer">Usuário Ativo</Label>
              </div>

              <div className="flex gap-2 justify-end pt-2" style={{ borderTop: '1px solid rgba(255,255,255,0.07)' }}>
                <Button type="button" variant="ghost" onClick={resetForm} disabled={saveMutation.isPending}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={saveMutation.isPending} className="bg-indigo-600 hover:bg-indigo-700">
                  {saveMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                  {editingId ? 'Salvar Alterações' : 'Criar Usuário'}
                </Button>
              </div>
            </form>
          </div>
        )}

        {/* Users Table */}
        <div className="rounded-lg overflow-hidden" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
          <table className="min-w-full">
            <thead style={{ background: 'rgba(255,255,255,0.04)', borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Usuário</th>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Grupos</th>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Criado em</th>
                <th className="px-6 py-3 text-right text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Ações</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-10 text-center" style={{ color: '#9CA3AF' }}>
                    <Loader2 className="h-6 w-6 animate-spin mx-auto mb-2 text-indigo-400" />
                    Carregando usuários...
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-10 text-center" style={{ color: '#9CA3AF' }}>Nenhum usuário cadastrado.</td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr
                    key={user.id}
                    style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}
                    onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.03)')}
                    onMouseLeave={e => (e.currentTarget.style.background = '')}
                  >
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-3">
                        <div
                          className="h-8 w-8 rounded-full flex items-center justify-center"
                          style={{ background: user.isAdmin ? 'rgba(147,51,234,0.15)' : 'rgba(255,255,255,0.07)' }}
                        >
                          {user.isAdmin
                            ? <Shield className="h-4 w-4 text-purple-400" />
                            : <User className="h-4 w-4" style={{ color: '#9CA3AF' }} />
                          }
                        </div>
                        <div>
                          <span className="text-sm font-medium" style={{ color: '#F9FAFB' }}>{user.email}</span>
                          {user.isAdmin && (
                            <span className="ml-2 text-xs font-medium" style={{ color: '#c084fc' }}>Admin</span>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-1">
                        {(user.groups && user.groups.length > 0) ? (
                          user.groups.map((groupKey) => (
                            <Badge
                              key={groupKey}
                              variant="secondary"
                              className="text-xs"
                            >
                              {getGroupName(groupKey)}
                            </Badge>
                          ))
                        ) : (
                          <span className="text-xs italic" style={{ color: '#6B7280' }}>Sem grupo</span>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {user.isActive ? (
                        <Badge className="bg-green-100 text-green-700 border-green-200">Ativo</Badge>
                      ) : (
                        <Badge className="bg-red-100 text-red-700 border-red-200">Inativo</Badge>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm" style={{ color: '#9CA3AF' }}>
                      {new Date(user.createdAt).toLocaleDateString('pt-BR')}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <div className="flex justify-end gap-2">
                        <Button variant="ghost" size="sm" onClick={() => handleEdit(user)}>
                          Editar
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-red-400 hover:text-red-300"
                          onClick={() => handleDelete(user)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </RoleGuard>
  );
}
