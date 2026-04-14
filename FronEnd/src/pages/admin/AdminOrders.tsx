import { useState } from "react";
import AdminLayout from "@/components/lootera/AdminLayout";
import { orders as initialOrders } from "@/data/mockData";
import { toast } from "sonner";

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
const statusOptions = ["pending", "processing", "completed", "cancelled"] as const;

const AdminOrders = () => {
  const [orderList, setOrderList] = useState(initialOrders);
  const [filter, setFilter] = useState("");

  const filtered = filter ? orderList.filter((o) => o.status === filter) : orderList;

  const updateStatus = (id: string, status: string) => {
    setOrderList((prev) => prev.map((o) => (o.id === id ? { ...o, status: status as any } : o)));
    toast.success(`Pedido ${id} atualizado para ${statusLabels[status]}`);
  };

  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Pedidos</h1>

      <div className="mb-4 flex flex-wrap gap-2">
        <button onClick={() => setFilter("")} className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${!filter ? "bg-primary text-primary-foreground" : "border border-border text-muted-foreground hover:bg-muted"}`}>Todos ({orderList.length})</button>
        {statusOptions.map((s) => (
          <button key={s} onClick={() => setFilter(s)} className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${filter === s ? "bg-primary text-primary-foreground" : "border border-border text-muted-foreground hover:bg-muted"}`}>
            {statusLabels[s]} ({orderList.filter((o) => o.status === s).length})
          </button>
        ))}
      </div>

      <div className="rounded-xl border border-border bg-card overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
              <th className="px-4 py-3">Pedido</th>
              <th className="px-4 py-3">Data</th>
              <th className="px-4 py-3">Itens</th>
              <th className="px-4 py-3">Pagamento</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-right">Total</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((o) => (
              <tr key={o.id} className="border-b border-border last:border-0">
                <td className="px-4 py-3 font-medium text-foreground">{o.id}</td>
                <td className="px-4 py-3 text-muted-foreground">{new Date(o.createdAt).toLocaleDateString("pt-BR")}</td>
                <td className="px-4 py-3 text-muted-foreground">{o.items.length} item(ns)</td>
                <td className="px-4 py-3 text-muted-foreground">{o.paymentMethod}</td>
                <td className="px-4 py-3">
                  <select value={o.status} onChange={(e) => updateStatus(o.id, e.target.value)} className={`rounded-md px-2 py-1 text-xs font-medium border-0 cursor-pointer ${statusColors[o.status]}`}>
                    {statusOptions.map((s) => <option key={s} value={s}>{statusLabels[s]}</option>)}
                  </select>
                </td>
                <td className="px-4 py-3 text-right font-semibold text-foreground">R$ {o.total.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
};

export default AdminOrders;
