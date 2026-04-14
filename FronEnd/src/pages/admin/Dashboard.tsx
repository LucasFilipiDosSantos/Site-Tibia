import AdminLayout from "@/components/lootera/AdminLayout";
import { orders, products, users } from "@/data/mockData";
import { DollarSign, ShoppingBag, Users, TrendingUp } from "lucide-react";

const Dashboard = () => {
  const totalRevenue = orders.filter((o) => o.status === "completed").reduce((s, o) => s + o.total, 0);
  const totalOrders = orders.length;
  const totalUsers = users.filter((u) => u.role === "customer").length;
  const totalProducts = products.length;

  const recentOrders = orders.slice(0, 5);

  const statusColors: Record<string, string> = {
    pending: "bg-brand-gold/15 text-brand-gold",
    processing: "bg-primary/15 text-primary",
    completed: "bg-green-500/15 text-green-400",
    cancelled: "bg-destructive/15 text-destructive",
  };
  const statusLabels: Record<string, string> = {
    pending: "Pendente",
    processing: "Processando",
    completed: "Concluído",
    cancelled: "Cancelado",
  };

  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Dashboard</h1>

      {/* KPI cards */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {[
          { label: "Receita", value: `R$ ${totalRevenue.toFixed(2)}`, icon: DollarSign, color: "text-primary" },
          { label: "Pedidos", value: totalOrders, icon: ShoppingBag, color: "text-brand-gold" },
          { label: "Clientes", value: totalUsers, icon: Users, color: "text-green-400" },
          { label: "Produtos", value: totalProducts, icon: TrendingUp, color: "text-purple-400" },
        ].map((kpi) => (
          <div key={kpi.label} className="rounded-xl border border-border bg-card p-5">
            <div className="flex items-center justify-between">
              <p className="text-xs font-medium uppercase text-muted-foreground">{kpi.label}</p>
              <kpi.icon size={18} className={kpi.color} />
            </div>
            <p className={`mt-2 text-2xl font-bold ${kpi.color}`}>{kpi.value}</p>
          </div>
        ))}
      </div>

      {/* Recent orders */}
      <div className="mt-8 rounded-xl border border-border bg-card">
        <div className="border-b border-border px-6 py-4">
          <h2 className="font-display text-base font-semibold text-foreground">Pedidos recentes</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
                <th className="px-6 py-3">Pedido</th>
                <th className="px-6 py-3">Data</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Pagamento</th>
                <th className="px-6 py-3 text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {recentOrders.map((o) => (
                <tr key={o.id} className="border-b border-border last:border-0">
                  <td className="px-6 py-3 font-medium text-foreground">{o.id}</td>
                  <td className="px-6 py-3 text-muted-foreground">{new Date(o.createdAt).toLocaleDateString("pt-BR")}</td>
                  <td className="px-6 py-3"><span className={`rounded-md px-2 py-0.5 text-xs font-medium ${statusColors[o.status]}`}>{statusLabels[o.status]}</span></td>
                  <td className="px-6 py-3 text-muted-foreground">{o.paymentMethod}</td>
                  <td className="px-6 py-3 text-right font-semibold text-foreground">R$ {o.total.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </AdminLayout>
  );
};

export default Dashboard;
