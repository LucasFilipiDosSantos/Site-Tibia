import AdminLayout from "@/components/lootera/AdminLayout";
import { users } from "@/data/mockData";
import { Shield, User } from "lucide-react";

const AdminUsers = () => {
  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Usuários</h1>

      <div className="rounded-xl border border-border bg-card overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
              <th className="px-4 py-3">Usuário</th>
              <th className="px-4 py-3">E-mail</th>
              <th className="px-4 py-3">Tipo</th>
              <th className="px-4 py-3">Membro desde</th>
              <th className="px-4 py-3 text-right">Total gasto</th>
              <th className="px-4 py-3 text-right">Pedidos</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="border-b border-border last:border-0">
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted text-muted-foreground">
                      {u.role === "admin" ? <Shield size={14} /> : <User size={14} />}
                    </div>
                    <span className="font-medium text-foreground">{u.name}</span>
                  </div>
                </td>
                <td className="px-4 py-3 text-muted-foreground">{u.email}</td>
                <td className="px-4 py-3">
                  <span className={`rounded-md px-2 py-0.5 text-xs font-medium ${u.role === "admin" ? "bg-primary/15 text-primary" : "bg-muted text-muted-foreground"}`}>
                    {u.role === "admin" ? "Admin" : "Cliente"}
                  </span>
                </td>
                <td className="px-4 py-3 text-muted-foreground">{new Date(u.createdAt).toLocaleDateString("pt-BR")}</td>
                <td className="px-4 py-3 text-right text-foreground">R$ {u.totalSpent.toFixed(2)}</td>
                <td className="px-4 py-3 text-right text-muted-foreground">{u.ordersCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
};

export default AdminUsers;
