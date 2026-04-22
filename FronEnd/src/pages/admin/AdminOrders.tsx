import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import AdminLayout from "@/components/lootera/AdminLayout";
import { adminService, type AdminOrder } from "@/features/admin/services/admin.service";
import { Pencil, Trash2, X } from "lucide-react";
import { toast } from "sonner";

const statusColors: Record<string, string> = {
  pending: "bg-brand-gold/15 text-brand-gold",
  paid: "bg-green-500/15 text-green-400",
  cancelled: "bg-destructive/15 text-destructive",
};

const statusLabels: Record<string, string> = {
  pending: "Pendente",
  paid: "Pago",
  cancelled: "Cancelado",
};

const statusOptions = ["pending", "paid", "cancelled"] as const;

const AdminOrders = () => {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: orderList = [], isLoading, isError } = useQuery({
    queryKey: ["admin", "orders"],
    queryFn: adminService.getOrders,
  });
  const [filter, setFilter] = useState("");
  const [editing, setEditing] = useState<AdminOrder | null>(null);

  const filtered = filter ? orderList.filter((order) => order.status === filter) : orderList;

  const closeEditor = () => {
    setEditing(null);
    setSearchParams({});
  };

  useEffect(() => {
    if (editing || isLoading) {
      return;
    }

    const editId = searchParams.get("edit");
    if (!editId) {
      return;
    }

    const order = orderList.find((item) => item.id === editId);
    if (order) {
      setEditing(order);
    }
  }, [editing, isLoading, orderList, searchParams]);

  const updateOrder = useMutation({
    mutationFn: async (order: AdminOrder) => adminService.updateOrder({
      id: order.id,
      customerName: order.customerName ?? "",
      customerEmail: order.customerEmail ?? "",
      customerDiscord: order.customerDiscord,
      paymentMethod: order.paymentMethod,
      status: order.status,
    }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin", "orders"] });
      toast.success("Compra atualizada");
      closeEditor();
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel atualizar a compra.");
    },
  });

  const deleteOrder = useMutation({
    mutationFn: adminService.deleteOrder,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin", "orders"] });
      toast.success("Compra removida");
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel remover a compra.");
    },
  });

  const confirmDelete = (order: AdminOrder) => {
    const confirmed = window.confirm(`Deletar a compra ${order.id.slice(0, 8)}? Essa acao nao pode ser desfeita.`);
    if (confirmed) {
      deleteOrder.mutate(order.id);
    }
  };

  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Pedidos</h1>

      {isLoading && <p className="mb-4 rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">Carregando pedidos...</p>}
      {isError && <p className="mb-4 rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">Nao foi possivel carregar os pedidos.</p>}

      {editing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4">
          <div className="w-full max-w-lg rounded-xl border border-border bg-card p-6">
            <div className="mb-4 flex items-center justify-between">
              <h2 className="font-display text-base font-semibold text-foreground">Editar compra</h2>
              <button onClick={closeEditor} className="text-muted-foreground hover:text-foreground"><X size={20} /></button>
            </div>
            <form onSubmit={(event) => { event.preventDefault(); updateOrder.mutate(editing); }} className="space-y-3">
              <div><label className="text-xs text-muted-foreground">Nome do cliente</label><input value={editing.customerName ?? ""} onChange={(event) => setEditing({ ...editing, customerName: event.target.value })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <div><label className="text-xs text-muted-foreground">E-mail</label><input type="email" value={editing.customerEmail ?? ""} onChange={(event) => setEditing({ ...editing, customerEmail: event.target.value })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground">Discord</label><input value={editing.customerDiscord ?? ""} onChange={(event) => setEditing({ ...editing, customerDiscord: event.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
                <div><label className="text-xs text-muted-foreground">Metodo</label><select value={editing.paymentMethod ?? "pix"} onChange={(event) => setEditing({ ...editing, paymentMethod: event.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"><option value="pix">PIX</option><option value="card">Cartao</option><option value="whatsapp">WhatsApp</option></select></div>
              </div>
              <div><label className="text-xs text-muted-foreground">Status</label><select value={editing.status} onChange={(event) => setEditing({ ...editing, status: event.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary">{statusOptions.map((status) => <option key={status} value={status}>{statusLabels[status]}</option>)}</select></div>
              <button type="submit" disabled={updateOrder.isPending} className="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60">{updateOrder.isPending ? "Salvando..." : "Salvar compra"}</button>
            </form>
          </div>
        </div>
      )}

      <div className="mb-4 flex flex-wrap gap-2">
        <button onClick={() => setFilter("")} className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${!filter ? "bg-primary text-primary-foreground" : "border border-border text-muted-foreground hover:bg-muted"}`}>Todos ({orderList.length})</button>
        {statusOptions.map((status) => (
          <button key={status} onClick={() => setFilter(status)} className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${filter === status ? "bg-primary text-primary-foreground" : "border border-border text-muted-foreground hover:bg-muted"}`}>
            {statusLabels[status]} ({orderList.filter((order) => order.status === status).length})
          </button>
        ))}
      </div>

      <div className="rounded-xl border border-border bg-card overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
              <th className="px-4 py-3">Pedido</th>
              <th className="px-4 py-3">Cliente</th>
              <th className="px-4 py-3">Data</th>
              <th className="px-4 py-3 text-right">Total</th>
              <th className="px-4 py-3">Referencia</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-right">Acoes</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((order) => (
              <tr key={order.id} className="border-b border-border last:border-0">
                <td className="px-4 py-3 font-medium text-foreground">{order.id.slice(0, 8)}</td>
                <td className="px-4 py-3">
                  <div className="font-medium text-foreground">{order.customerName ?? "Cliente nao informado"}</div>
                  <div className="text-xs text-muted-foreground">{order.customerEmail ?? "Sem e-mail"}</div>
                  {order.customerDiscord && <div className="text-xs text-muted-foreground">Discord: {order.customerDiscord}</div>}
                </td>
                <td className="px-4 py-3 text-muted-foreground">{new Date(order.createdAt).toLocaleDateString("pt-BR")}</td>
                <td className="px-4 py-3 text-right text-foreground">R$ {order.totalAmount.toFixed(2)}</td>
                <td className="px-4 py-3 text-muted-foreground">{order.orderIntentKey}</td>
                <td className="px-4 py-3">
                  <span className={`rounded-md px-2 py-1 text-xs font-medium ${statusColors[order.status] ?? "bg-muted text-muted-foreground"}`}>
                    {order.statusLabel}
                  </span>
                </td>
                <td className="px-4 py-3 text-right">
                  <div className="flex justify-end gap-2">
                    <button onClick={() => setEditing(order)} className="inline-flex items-center gap-1 rounded-lg border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary/40 hover:text-primary">
                      <Pencil size={14} /> Editar
                    </button>
                    <button onClick={() => confirmDelete(order)} disabled={deleteOrder.isPending} className="inline-flex items-center gap-1 rounded-lg border border-destructive/30 px-3 py-1.5 text-xs font-medium text-destructive transition-colors hover:bg-destructive/10 disabled:opacity-60">
                      <Trash2 size={14} /> Deletar
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {!isLoading && filtered.length === 0 && (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={7}>Nenhum pedido encontrado.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
};

export default AdminOrders;
