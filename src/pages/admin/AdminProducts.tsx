import { useState } from "react";
import AdminLayout from "@/components/lootera/AdminLayout";
import { products as initialProducts, categories } from "@/data/mockData";
import type { Product } from "@/data/mockData";
import { Plus, Pencil, Trash2, X } from "lucide-react";
import { toast } from "sonner";

const AdminProducts = () => {
  const [productList, setProductList] = useState<Product[]>(initialProducts);
  const [editing, setEditing] = useState<Product | null>(null);
  const [showForm, setShowForm] = useState(false);

  const emptyProduct: Product = { id: "", name: "", category: "Moedas", server: "Antica", price: 0, description: "", image: "/placeholder.svg", stock: 0, rating: 0, sales: 0 };

  const handleSave = (p: Product) => {
    if (p.id) {
      setProductList((prev) => prev.map((x) => (x.id === p.id ? p : x)));
      toast.success("Produto atualizado");
    } else {
      setProductList((prev) => [...prev, { ...p, id: crypto.randomUUID() }]);
      toast.success("Produto criado");
    }
    setEditing(null);
    setShowForm(false);
  };

  const handleDelete = (id: string) => {
    setProductList((prev) => prev.filter((x) => x.id !== id));
    toast.info("Produto removido");
  };

  return (
    <AdminLayout>
      <div className="flex items-center justify-between mb-6">
        <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">Produtos</h1>
        <button onClick={() => { setEditing(emptyProduct); setShowForm(true); }} className="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
          <Plus size={16} /> Novo produto
        </button>
      </div>

      {/* Product form modal */}
      {showForm && editing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4">
          <div className="w-full max-w-lg rounded-xl border border-border bg-card p-6 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-display text-base font-semibold text-foreground">{editing.id ? "Editar produto" : "Novo produto"}</h2>
              <button onClick={() => { setShowForm(false); setEditing(null); }} className="text-muted-foreground hover:text-foreground"><X size={20} /></button>
            </div>
            <form onSubmit={(e) => { e.preventDefault(); handleSave(editing); }} className="space-y-3">
              <div><label className="text-xs text-muted-foreground">Nome</label><input value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground">Categoria</label><select value={editing.category} onChange={(e) => setEditing({ ...editing, category: e.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary">{categories.map((c) => <option key={c}>{c}</option>)}</select></div>
                <div><label className="text-xs text-muted-foreground">Servidor</label><input value={editing.server} onChange={(e) => setEditing({ ...editing, server: e.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground">Preço (R$)</label><input type="number" step="0.01" value={editing.price} onChange={(e) => setEditing({ ...editing, price: Number(e.target.value) })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
                <div><label className="text-xs text-muted-foreground">Estoque</label><input type="number" value={editing.stock} onChange={(e) => setEditing({ ...editing, stock: Number(e.target.value) })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              </div>
              <div><label className="text-xs text-muted-foreground">Descrição</label><textarea value={editing.description} onChange={(e) => setEditing({ ...editing, description: e.target.value })} rows={3} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <button type="submit" className="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">Salvar</button>
            </form>
          </div>
        </div>
      )}

      {/* Table */}
      <div className="rounded-xl border border-border bg-card overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
              <th className="px-4 py-3">Nome</th>
              <th className="px-4 py-3">Categoria</th>
              <th className="px-4 py-3">Servidor</th>
              <th className="px-4 py-3 text-right">Preço</th>
              <th className="px-4 py-3 text-right">Estoque</th>
              <th className="px-4 py-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody>
            {productList.map((p) => (
              <tr key={p.id} className="border-b border-border last:border-0">
                <td className="px-4 py-3 font-medium text-foreground">{p.name}</td>
                <td className="px-4 py-3 text-muted-foreground">{p.category}</td>
                <td className="px-4 py-3 text-muted-foreground">{p.server}</td>
                <td className="px-4 py-3 text-right text-foreground">R$ {p.price.toFixed(2)}</td>
                <td className="px-4 py-3 text-right"><span className={p.stock < 10 ? "text-destructive" : "text-muted-foreground"}>{p.stock}</span></td>
                <td className="px-4 py-3 text-right">
                  <button onClick={() => { setEditing(p); setShowForm(true); }} className="mr-2 text-muted-foreground hover:text-primary"><Pencil size={14} /></button>
                  <button onClick={() => handleDelete(p.id)} className="text-muted-foreground hover:text-destructive"><Trash2 size={14} /></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
};

export default AdminProducts;
