import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import AdminLayout from "@/components/lootera/AdminLayout";
import { adminService } from "@/features/admin/services/admin.service";
import { AlertTriangle } from "lucide-react";
import { toast } from "sonner";

const AdminInventory = () => {
  const queryClient = useQueryClient();
  const { data: products = [], isLoading, isError } = useQuery({
    queryKey: ["admin", "products"],
    queryFn: adminService.getProducts,
  });
  const [adjustments, setAdjustments] = useState<Record<string, number>>({});

  const sorted = [...products].sort((a, b) => a.stock - b.stock);
  const lowStockCount = sorted.filter((product) => product.stock < 10).length;

  const adjustInventory = useMutation({
    mutationFn: ({ productId, delta }: { productId: string; delta: number }) =>
      adminService.adjustInventory(productId, delta, "Ajuste manual pelo painel admin"),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin", "products"] });
      toast.success("Estoque atualizado");
      setAdjustments({});
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel ajustar o estoque.");
    },
  });

  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Controle de Estoque</h1>

      {isLoading && <p className="mb-4 rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">Carregando estoque...</p>}
      {isError && <p className="mb-4 rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">Nao foi possivel carregar o estoque.</p>}

      {lowStockCount > 0 && (
        <div className="mb-6 flex items-center gap-3 rounded-xl border border-brand-gold/20 bg-brand-gold/5 px-4 py-3">
          <AlertTriangle size={18} className="text-brand-gold shrink-0" />
          <p className="text-sm text-brand-gold">{lowStockCount} produto(s) com estoque baixo</p>
        </div>
      )}

      <div className="rounded-xl border border-border bg-card overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
              <th className="px-4 py-3">Produto</th>
              <th className="px-4 py-3">Categoria</th>
              <th className="px-4 py-3">Servidor</th>
              <th className="px-4 py-3 text-right">Estoque</th>
              <th className="px-4 py-3 text-right">Vendas</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-right">Ajuste</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((product) => {
              const isLow = product.stock < 10;
              return (
                <tr key={product.id} className="border-b border-border last:border-0">
                  <td className="px-4 py-3 font-medium text-foreground">{product.name}</td>
                  <td className="px-4 py-3 text-muted-foreground">{product.category}</td>
                  <td className="px-4 py-3 text-muted-foreground">{product.server}</td>
                  <td className={`px-4 py-3 text-right font-semibold ${isLow ? "text-destructive" : "text-foreground"}`}>{product.stock}</td>
                  <td className="px-4 py-3 text-right text-muted-foreground">{product.sales}</td>
                  <td className="px-4 py-3">
                    <span className={`rounded-md px-2 py-0.5 text-xs font-medium ${isLow ? "bg-destructive/15 text-destructive" : "bg-green-500/15 text-green-400"}`}>
                      {isLow ? "Baixo" : "OK"}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <input
                        type="number"
                        value={adjustments[product.id] ?? 0}
                        onChange={(event) => setAdjustments((current) => ({ ...current, [product.id]: Number(event.target.value) }))}
                        className="w-20 rounded-lg border border-border bg-input px-2 py-1 text-right text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                      />
                      <button
                        onClick={() => adjustInventory.mutate({ productId: product.id, delta: adjustments[product.id] ?? 0 })}
                        disabled={(adjustments[product.id] ?? 0) === 0 || adjustInventory.isPending}
                        className="rounded-lg bg-primary px-3 py-1 text-xs font-medium text-primary-foreground disabled:opacity-50"
                      >
                        Aplicar
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
};

export default AdminInventory;
