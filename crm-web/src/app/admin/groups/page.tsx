'use client';

import { useEffect, useState } from 'react';
import { adminGroupsService, UserGroup } from '@/services/adminGroups';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Table,
  TableHeader,
  TableRow,
  TableHead,
  TableBody,
  TableCell,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Loader2, Plus, Users, Trash2 } from 'lucide-react';
import { useToast } from '@/components/ui/use-toast'; 

export default function UserGroupsPage() {
  const [groups, setGroups] = useState<UserGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [newGroup, setNewGroup] = useState({ name: '', description: '' });
  const [creating, setCreating] = useState(false);
  
  const { toast } = useToast();

  const loadGroups = async () => {
    try {
      setLoading(true);
      const data = await adminGroupsService.getAll();
      setGroups(data);
    } catch (error) {
        console.error(error);
      toast({
        title: 'Error',
        description: 'Failed to load user groups.',
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadGroups();
  }, []);

  const handleCreate = async () => {
    if (!newGroup.name) return;
    
    try {
      setCreating(true);
      await adminGroupsService.create(newGroup);
      toast({ title: 'Success', description: 'Group created successfully.' });
      setIsCreateOpen(false);
      setNewGroup({ name: '', description: '' });
      loadGroups();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to create group.',
        variant: 'destructive',
      });
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this group?')) return;
    
    try {
      await adminGroupsService.delete(id);
      setGroups(groups.filter(g => g.id !== id));
      toast({ title: 'Success', description: 'Group deleted.' });
    } catch (error) {
      toast({ title: 'Error', description: 'Failed to delete group.', variant: 'destructive' });
    }
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">User Groups</h1>
          <p className="text-muted-foreground">Manage user groups and their access permissions.</p>
        </div>
        
        <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" /> New Group
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Create User Group</DialogTitle>
              <DialogDescription>
                Create a new group to organize users and assign permissions.
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name</Label>
                <Input 
                  id="name" 
                  value={newGroup.name} 
                  onChange={(e) => setNewGroup({ ...newGroup, name: e.target.value })} 
                  placeholder="e.g. Content Writers"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="desc">Description</Label>
                <Textarea 
                  id="desc" 
                  value={newGroup.description} 
                  onChange={(e) => setNewGroup({ ...newGroup, description: e.target.value })} 
                  placeholder="Optional description"
                />
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setIsCreateOpen(false)}>Cancel</Button>
              <Button onClick={handleCreate} disabled={creating || !newGroup.name}>
                {creating && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Create
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-xl flex items-center gap-2">
            <Users className="h-5 w-5" /> Groups
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
             <div className="flex justify-center p-8">
               <Loader2 className="h-8 w-8 animate-spin text-primary" />
             </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Members</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {groups.map((group) => (
                  <TableRow key={group.id}>
                    <TableCell className="font-medium">{group.name}</TableCell>
                    <TableCell className="text-muted-foreground">{group.description}</TableCell>
                    <TableCell>
                        <span className="text-xs bg-secondary px-2 py-1 rounded-full">
                            {group.memberCount || 0} users
                        </span>
                    </TableCell>
                    <TableCell className="text-right space-x-2">
                      <Button variant="outline" size="sm" asChild>
                        <a href={`/admin/groups/${group.id}`}>Manage Access</a>
                      </Button>
                      <Button variant="ghost" size="icon" onClick={() => handleDelete(group.id)}>
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {groups.length === 0 && (
                    <TableRow>
                        <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                            No groups found. Create one above.
                        </TableCell>
                    </TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
