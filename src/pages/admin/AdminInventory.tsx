import AdminLayout from "@/components/lootera/AdminLayout";
import { products } from "@/data/mockData";
import { AlertTriangle } from "lucide-react";

const AdminInventory = () => {
  const sorted = [...products].sort((a, b) => a.stock - b.stock);

  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Controle de Estoque</h1>

      {/* Low stock alert */}
      {sorted.filter((p) => p.stock < 10 && p.stock !== 999).length > 0 && (
        <div className="mb-6 flex items-center gap-3 rounded-xl border border-brand-gold/20 bg-brand-gold/5 px-4 py-3">
          <AlertTriangle size={18} className="text-brand-gold shrink-0" />
          <p className="text-sm text-brand-gold">
            {sorted.filter((p) => p.stock < 10 && p.stock !== 999).length} produto(s) com estoque baixo
          </p>
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
            </tr>
          </thead>
          <tbody>
            {sorted.map((p) => {
              const isLow = p.stock < 10 && p.stock !== 999;
              const isDigital = p.stock === 999;
              return (
                <tr key={p.id} className="border-b border-border last:border-0">
                  <td className="px-4 py-3 font-medium text-foreground">{p.name}</td>
                  <td className="px-4 py-3 text-muted-foreground">{p.category}</td>
                  <td className="px-4 py-3 text-muted-foreground">{p.server}</td>
                  <td className={`px-4 py-3 text-right font-semibold ${isLow ? "text-destructive" : "text-foreground"}`}>{isDigital ? "∞" : p.stock}</td>
                  <td className="px-4 py-3 text-right text-muted-foreground">{p.sales}</td>
                  <td className="px-4 py-3">
                    <span className={`rounded-md px-2 py-0.5 text-xs font-medium ${isDigital ? "bg-primary/15 text-primary" : isLow ? "bg-destructive/15 text-destructive" : "bg-green-500/15 text-green-400"}`}>
                      {isDigital ? "Digital" : isLow ? "Baixo" : "OK"}
                    </span>
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
