import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import AdminLayout from "@/components/lootera/AdminLayout";
import { ProductImage } from "@/components/lootera/ProductImage";
import { adminService, type AdminOrder } from "@/features/admin/services/admin.service";
import type { Product } from "@/features/products/types/product.types";
import { DollarSign, ShoppingBag, Users, TrendingUp, Plus, Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";

const Dashboard = () => {
  const queryClient = useQueryClient();
  const { data: products = [], isLoading: loadingProducts } = useQuery({
    queryKey: ["admin", "products"],
    queryFn: adminService.getProducts,
  });
  const { data: orders = [], isLoading: loadingOrders } = useQuery({
    queryKey: ["admin", "orders"],
    queryFn: adminService.getOrders,
  });
  const { data: users = [], isLoading: loadingUsers } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: adminService.getUsers,
  });

  const loading = loadingProducts || loadingOrders || loadingUsers;
  const totalRevenue = orders
    .filter((order) => order.status === "paid")
    .reduce((sum, order) => sum + order.totalAmount, 0);
  const totalOrders = orders.length;
  const totalUsers = users.filter((user) => user.role === "customer").length;
  const totalProducts = products.length;
  const recentOrders = orders.slice(0, 5);
  const recentProducts = products.slice(0, 5);

  const statusColors: Record<string, string> = {
    pending: "bg-brand-gold/15 text-brand-gold",
    paid: "bg-green-500/15 text-green-400",
    cancelled: "bg-destructive/15 text-destructive",
  };

  const deleteProduct = useMutation({
    mutationFn: async (product: Product) => {
      if (!product.slug) {
        throw new Error("Produto sem slug para exclusao.");
      }

      await adminService.deleteProduct(product.slug);
      return product;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin", "products"] });
      toast.success("Produto removido");
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel remover o produto.");
    },
  });

  const confirmDeleteProduct = (product: Product) => {
    const confirmed = window.confirm(`Remover o produto "${product.name}"? Essa acao nao pode ser desfeita.`);
    if (confirmed) {
      deleteProduct.mutate(product);
    }
  };

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

  const confirmDeleteOrder = (order: AdminOrder) => {
    const confirmed = window.confirm(`Deletar a compra ${order.id.slice(0, 8)}? Essa acao nao pode ser desfeita.`);
    if (confirmed) {
      deleteOrder.mutate(order.id);
    }
  };

  return (
    <AdminLayout>
      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">Dashboard</h1>
        <Link to="/admin/produtos?new=1" className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
          <Plus size={16} /> Adicionar produto
        </Link>
      </div>

      {loading && <p className="mb-4 rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">Carregando informacoes da loja...</p>}

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

      <div className="mt-8 rounded-xl border border-border bg-card">
        <div className="flex items-center justify-between border-b border-border px-6 py-4">
          <h2 className="font-display text-base font-semibold text-foreground">Produtos</h2>
          <Link to="/admin/produtos" className="text-sm font-medium text-primary hover:underline">Ver todos</Link>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
                <th className="px-6 py-3">Produto</th>
                <th className="px-6 py-3">Categoria</th>
                <th className="px-6 py-3 text-right">Preco</th>
                <th className="px-6 py-3 text-right">Estoque</th>
                <th className="px-6 py-3 text-right">Acoes</th>
              </tr>
            </thead>
            <tbody>
              {recentProducts.map((product) => (
                <tr key={product.id} className="border-b border-border last:border-0">
                  <td className="px-6 py-3">
                    <div className="flex items-center gap-3">
                      <ProductImage src={product.image} alt={product.name} fallbackLabel={product.category} className="h-12 w-16 shrink-0" />
                      <span className="font-medium text-foreground">{product.name}</span>
                    </div>
                  </td>
                  <td className="px-6 py-3 text-muted-foreground">{product.category}</td>
                  <td className="px-6 py-3 text-right text-foreground">R$ {product.price.toFixed(2)}</td>
                  <td className="px-6 py-3 text-right text-muted-foreground">{product.stock}</td>
                  <td className="px-6 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <Link to={`/admin/produtos?edit=${encodeURIComponent(product.slug ?? product.id)}`} className="inline-flex items-center gap-1 rounded-lg border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary/40 hover:text-primary">
                        <Pencil size={14} /> Editar
                      </Link>
                      <button
                        onClick={() => confirmDeleteProduct(product)}
                        disabled={deleteProduct.isPending}
                        className="inline-flex items-center gap-1 rounded-lg border border-destructive/30 px-3 py-1.5 text-xs font-medium text-destructive transition-colors hover:bg-destructive/10 disabled:opacity-60"
                      >
                        <Trash2 size={14} /> Deletar
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {!loading && recentProducts.length === 0 && (
                <tr>
                  <td className="px-6 py-6 text-sm text-muted-foreground" colSpan={5}>Nenhum produto encontrado.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="mt-8 rounded-xl border border-border bg-card">
        <div className="border-b border-border px-6 py-4">
          <h2 className="font-display text-base font-semibold text-foreground">Pedidos recentes</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
                <th className="px-6 py-3">Pedido</th>
                <th className="px-6 py-3">Cliente</th>
                <th className="px-6 py-3">Data</th>
                <th className="px-6 py-3 text-right">Total</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Referencia</th>
                <th className="px-6 py-3 text-right">Acoes</th>
              </tr>
            </thead>
            <tbody>
              {recentOrders.map((order) => (
                <tr key={order.id} className="border-b border-border last:border-0">
                  <td className="px-6 py-3 font-medium text-foreground">{order.id.slice(0, 8)}</td>
                  <td className="px-6 py-3">
                    <div className="font-medium text-foreground">{order.customerName ?? "Cliente nao informado"}</div>
                    <div className="text-xs text-muted-foreground">{order.customerEmail ?? "Sem e-mail"}</div>
                  </td>
                  <td className="px-6 py-3 text-muted-foreground">{new Date(order.createdAt).toLocaleDateString("pt-BR")}</td>
                  <td className="px-6 py-3 text-right text-foreground">R$ {order.totalAmount.toFixed(2)}</td>
                  <td className="px-6 py-3">
                    <span className={`rounded-md px-2 py-0.5 text-xs font-medium ${statusColors[order.status] ?? "bg-muted text-muted-foreground"}`}>{order.statusLabel}</span>
                  </td>
                  <td className="px-6 py-3 text-muted-foreground">{order.orderIntentKey}</td>
                  <td className="px-6 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <Link to={`/admin/pedidos?edit=${encodeURIComponent(order.id)}`} className="inline-flex items-center gap-1 rounded-lg border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary/40 hover:text-primary">
                        <Pencil size={14} /> Editar
                      </Link>
                      <button
                        onClick={() => confirmDeleteOrder(order)}
                        disabled={deleteOrder.isPending}
                        className="inline-flex items-center gap-1 rounded-lg border border-destructive/30 px-3 py-1.5 text-xs font-medium text-destructive transition-colors hover:bg-destructive/10 disabled:opacity-60"
                      >
                        <Trash2 size={14} /> Deletar
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {!loading && recentOrders.length === 0 && (
                <tr>
                  <td className="px-6 py-6 text-sm text-muted-foreground" colSpan={7}>Nenhum pedido encontrado.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </AdminLayout>
  );
};

export default Dashboard;
