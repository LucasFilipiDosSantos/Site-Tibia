import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import AdminLayout from "@/components/lootera/AdminLayout";
import { adminService, type AdminUser } from "@/features/admin/services/admin.service";
import { Pencil, Shield, Trash2, User, X } from "lucide-react";
import { toast } from "sonner";

type EditingUser = AdminUser & {
  newPassword: string;
};

const AdminUsers = () => {
  const queryClient = useQueryClient();
  const { data: users = [], isLoading, isError } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: adminService.getUsers,
  });
  const [editing, setEditing] = useState<EditingUser | null>(null);

  const updateUser = useMutation({
    mutationFn: async (user: EditingUser) => adminService.updateUser({
      id: user.id,
      name: user.name,
      email: user.email,
      role: user.role,
      emailVerified: user.emailVerified,
      newPassword: user.newPassword,
    }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Usuario atualizado");
      setEditing(null);
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel atualizar o usuario.");
    },
  });

  const deleteUser = useMutation({
    mutationFn: adminService.deleteUser,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Usuario excluido");
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel excluir o usuario.");
    },
  });

  const openEditor = (user: AdminUser) => {
    setEditing({ ...user, newPassword: "" });
  };

  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Usuarios</h1>

      {isLoading && <p className="mb-4 rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">Carregando usuarios...</p>}
      {isError && <p className="mb-4 rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">Nao foi possivel carregar os usuarios.</p>}

      {editing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4">
          <div className="w-full max-w-lg rounded-xl border border-border bg-card p-6">
            <div className="mb-4 flex items-center justify-between">
              <h2 className="font-display text-base font-semibold text-foreground">Editar usuario</h2>
              <button onClick={() => setEditing(null)} className="text-muted-foreground hover:text-foreground"><X size={20} /></button>
            </div>
            <form onSubmit={(event) => { event.preventDefault(); updateUser.mutate(editing); }} className="space-y-3">
              <div><label className="text-xs text-muted-foreground">Nome</label><input value={editing.name} onChange={(event) => setEditing({ ...editing, name: event.target.value })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <div><label className="text-xs text-muted-foreground">E-mail</label><input type="email" value={editing.email} onChange={(event) => setEditing({ ...editing, email: event.target.value })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground">Tipo</label><select value={editing.role} onChange={(event) => setEditing({ ...editing, role: event.target.value as AdminUser["role"] })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"><option value="customer">Cliente</option><option value="admin">Admin</option></select></div>
                <label className="mt-6 flex items-center gap-2 text-sm text-foreground"><input type="checkbox" checked={editing.emailVerified} onChange={(event) => setEditing({ ...editing, emailVerified: event.target.checked })} className="h-4 w-4 accent-primary" /> E-mail verificado</label>
              </div>
              <div>
                <label className="text-xs text-muted-foreground">Nova senha para recuperar acesso</label>
                <input type="password" value={editing.newPassword} onChange={(event) => setEditing({ ...editing, newPassword: event.target.value })} placeholder="Deixe em branco para manter a senha atual" className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" />
                <p className="mt-1 text-xs text-muted-foreground">A senha atual nao pode ser visualizada; o sistema armazena apenas o hash. Para recuperar acesso, defina uma nova senha.</p>
              </div>
              <button type="submit" disabled={updateUser.isPending} className="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60">{updateUser.isPending ? "Salvando..." : "Salvar usuario"}</button>
            </form>
          </div>
        </div>
      )}

      <div className="rounded-xl border border-border bg-card overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
              <th className="px-4 py-3">Usuario</th>
              <th className="px-4 py-3">E-mail</th>
              <th className="px-4 py-3">Tipo</th>
              <th className="px-4 py-3">Verificado</th>
              <th className="px-4 py-3">Membro desde</th>
              <th className="px-4 py-3 text-right">Acoes</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className="border-b border-border last:border-0">
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted text-muted-foreground">
                      {user.role === "admin" ? <Shield size={14} /> : <User size={14} />}
                    </div>
                    <span className="font-medium text-foreground">{user.name}</span>
                  </div>
                </td>
                <td className="px-4 py-3 text-muted-foreground">{user.email}</td>
                <td className="px-4 py-3">
                  <span className={`rounded-md px-2 py-0.5 text-xs font-medium ${user.role === "admin" ? "bg-primary/15 text-primary" : "bg-muted text-muted-foreground"}`}>
                    {user.role === "admin" ? "Admin" : "Cliente"}
                  </span>
                </td>
                <td className="px-4 py-3 text-muted-foreground">{user.emailVerified ? "Sim" : "Nao"}</td>
                <td className="px-4 py-3 text-muted-foreground">{new Date(user.createdAt).toLocaleDateString("pt-BR")}</td>
                <td className="px-4 py-3 text-right">
                  <div className="flex justify-end gap-2">
                    <button onClick={() => openEditor(user)} className="inline-flex items-center gap-1 rounded-lg border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary/40 hover:text-primary">
                      <Pencil size={14} /> Editar
                    </button>
                    <button
                      onClick={() => {
                        if (window.confirm(`Excluir usuario ${user.email}?`)) {
                          deleteUser.mutate(user.id);
                        }
                      }}
                      disabled={deleteUser.isPending}
                      className="inline-flex items-center gap-1 rounded-lg border border-destructive/30 px-3 py-1.5 text-xs font-medium text-destructive transition-colors hover:bg-destructive/10 disabled:opacity-60"
                    >
                      <Trash2 size={14} /> Excluir
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {!isLoading && users.length === 0 && (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={6}>Nenhum usuario encontrado.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
};

export default AdminUsers;
