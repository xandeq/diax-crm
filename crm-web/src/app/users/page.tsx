'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { userService } from '@/services/users';
import { UserRole } from '@/types/auth';
import { UserResponse, UpdateUserRequest } from '@/types/users';
import { AlertCircle, Plus, RefreshCw, Trash2, UserPlus, Shield, User } from 'lucide-react';
import { useEffect, useState } from 'react';

export default function UsersPage() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // Form state
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    role: 'User' as UserRole,
    isActive: true
  });
  const [editingId, setEditingId] = useState<string | null>(null);

  useEffect(() => {
    loadUsers();
  }, []);

  async function loadUsers() {
    try {
      setLoading(true);
      setError(null);
      const data = await userService.getAll();
      setUsers(data);
    } catch (err: any) {
      setError('Erro ao carregar usuários. Verifique se você tem permissão.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  const resetForm = () => {
    setFormData({ email: '', password: '', role: 'User', isActive: true });
    setEditingId(null);
    setIsFormOpen(false);
  };

  const handleEdit = (user: UserResponse) => {
    setEditingId(user.id);
    setFormData({
      email: user.email,
      password: '',
      role: user.role,
      isActive: user.isActive
    });
    setIsFormOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      if (editingId) {
        const updateData: UpdateUserRequest = {
          role: formData.role,
          isActive: formData.isActive
        };
        // Só incluir password se foi preenchido
        if (formData.password && formData.password.trim() !== '') {
          updateData.password = formData.password;
        }
        console.log('Updating user with data:', updateData);
        await userService.update(editingId, updateData);
      } else {
        const createData = {
          email: formData.email,
          password: formData.password,
          role: formData.role
        };
        console.log('Creating user with data:', createData);
        await userService.create(createData);
      }
      loadUsers();
      resetForm();
    } catch (err: any) {
      console.error('Error saving user:', err);
      setError(err.response?.data?.message || err.message || 'Erro ao salvar usuário.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (user: UserResponse) => {
    if (!confirm(`Tem certeza que deseja deletar o usuário ${user.email}?`)) return;

    try {
      await userService.delete(user.id);
      loadUsers();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Erro ao deletar usuário.');
    }
  };

  return (
    <RoleGuard allowedRoles={['Admin']}>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Gerenciamento de Usuários</h1>
            <p className="text-gray-500 mt-1">Administre as contas e permissões do sistema</p>
          </div>
          <div className="flex gap-2">
            <Button onClick={loadUsers} variant="outline" disabled={loading}>
              <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
              Atualizar
            </Button>
            <Button onClick={() => setIsFormOpen(true)} className="bg-indigo-600 hover:bg-indigo-700">
              <Plus className="h-4 w-4 mr-2" />
              Novo Usuário
            </Button>
          </div>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="h-5 w-5 text-red-500" />
            <p className="text-sm text-red-600 flex-1">{error}</p>
            <Button variant="ghost" size="sm" onClick={() => setError(null)}>X</Button>
          </div>
        )}

        {isFormOpen && (
          <div className="bg-white p-6 rounded-lg border shadow-sm">
            <h2 className="text-lg font-semibold mb-4">
              {editingId ? 'Editar Usuário' : 'Novo Usuário'}
            </h2>
            <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">E-mail</label>
                <Input
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  disabled={!!editingId || submitting}
                  required
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Senha {editingId && '(deixe em branco para manter)'}</label>
                <Input
                  type="password"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  required={!editingId}
                  disabled={submitting}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Perfil</label>
                <Select
                  value={formData.role}
                  onValueChange={(val: string) => {
                    console.log('Role changed to:', val);
                    setFormData({ ...formData, role: val as UserRole });
                  }}
                  disabled={submitting}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Selecione o perfil" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Admin">Administrador</SelectItem>
                    <SelectItem value="User">Usuário Comum</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2 flex items-end">
                <div className="flex items-center gap-2 pb-2">
                   <input
                    type="checkbox"
                    id="isActive"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                  />
                  <label htmlFor="isActive" className="text-sm font-medium">Ativo</label>
                </div>
              </div>
              <div className="col-span-full flex gap-2 justify-end pt-2">
                <Button type="button" variant="ghost" onClick={resetForm} disabled={submitting}>Cancelar</Button>
                <Button type="submit" disabled={submitting} className="bg-indigo-600 hover:bg-indigo-700">
                  {submitting ? 'Salvando...' : 'Salvar Alterações'}
                </Button>
              </div>
            </form>
          </div>
        )}

        <div className="bg-white rounded-lg border overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Usuário</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Perfil</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Criado em</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Ações</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-10 text-center text-gray-500">Carregando usuários...</td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                   <td colSpan={5} className="px-6 py-10 text-center text-gray-500">Nenhum usuário cadastrado.</td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-3">
                        <div className="h-8 w-8 rounded-full bg-gray-100 flex items-center justify-center">
                          <User className="h-4 w-4 text-gray-500" />
                        </div>
                        <span className="text-sm font-medium text-gray-900">{user.email}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {user.role === 'Admin' ? (
                        <div className="flex items-center text-purple-700 text-sm font-medium">
                          <Shield className="h-3 w-3 mr-1" /> Administrador
                        </div>
                      ) : (
                        <div className="flex items-center text-gray-600 text-sm">
                           Usuário
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {user.isActive ? (
                        <Badge className="bg-green-100 text-green-700 border-green-200">Ativo</Badge>
                      ) : (
                        <Badge className="bg-red-100 text-red-700 border-red-200">Inativo</Badge>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(user.createdAt).toLocaleDateString('pt-BR')}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <div className="flex justify-end gap-2">
                        <Button variant="ghost" size="sm" onClick={() => handleEdit(user)}>Editar</Button>
                        <Button variant="ghost" size="sm" className="text-red-600 hover:text-red-800" onClick={() => handleDelete(user)}>
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
