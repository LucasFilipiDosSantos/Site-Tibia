import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import AdminLayout from "@/components/lootera/AdminLayout";
import { ProductImage } from "@/components/lootera/ProductImage";
import { adminService } from "@/features/admin/services/admin.service";
import { CATEGORY_OPTIONS } from "@/features/products/utils/catalog";
import type { Product } from "@/features/products/types/product.types";
import { Plus, Pencil, Trash2, X } from "lucide-react";
import { toast } from "sonner";

const WORLD_OPTIONS = ["Aurera", "Eternia", "Antica", "Secura"];
const ACCEPTED_IMAGE_TYPES = ["image/png", "image/jpeg", "image/webp"];
const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;

const createEmptyProduct = (): Product => ({
  id: "",
  slug: "",
  name: "",
  category: "Coin",
  categorySlug: "coin",
  server: "",
  price: 0,
  description: "",
  image: "",
  stock: 0,
  rating: 0,
  reviewCount: 0,
  sales: 0,
});

const AdminProducts = () => {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: productList = [], isLoading, isError } = useQuery({
    queryKey: ["admin", "products"],
    queryFn: adminService.getProducts,
  });
  const [editing, setEditing] = useState<Product | null>(null);
  const [initialStock, setInitialStock] = useState(0);
  const [showForm, setShowForm] = useState(false);
  const [imageMode, setImageMode] = useState<"url" | "upload">("url");
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreviewUrl, setImagePreviewUrl] = useState<string | null>(null);

  const clearImageFile = () => {
    if (imagePreviewUrl) {
      URL.revokeObjectURL(imagePreviewUrl);
    }

    setImagePreviewUrl(null);
    setImageFile(null);
  };

  const openNewProductForm = () => {
    clearImageFile();
    setImageMode("url");
    setEditing(createEmptyProduct());
    setInitialStock(0);
    setShowForm(true);
  };

  const openEditProductForm = (product: Product) => {
    clearImageFile();
    setImageMode(product.image && product.image !== "/placeholder.svg" ? "url" : "upload");
    setEditing(product);
    setInitialStock(product.stock);
    setShowForm(true);
  };

  const closeForm = () => {
    clearImageFile();
    setShowForm(false);
    setEditing(null);
    setSearchParams({});
  };

  useEffect(() => {
    if (showForm || isLoading) {
      return;
    }

    if (searchParams.get("new") === "1") {
      openNewProductForm();
      return;
    }

    const editSlug = searchParams.get("edit");
    if (editSlug) {
      const product = productList.find((item) => item.slug === editSlug || item.id === editSlug);
      if (product) {
        openEditProductForm(product);
      }
    }
  }, [isLoading, productList, searchParams, showForm]);

  const saveProduct = useMutation({
    mutationFn: async (product: Product) => {
      const slug = product.slug || adminService.buildSlug(product.name);
      const categorySlug = product.categorySlug ?? "coin";
      const server = product.server.trim();
      if (categorySlug === "gold" && !server) {
        throw new Error("Mundo e obrigatorio para produtos da categoria Gold.");
      }

      const imageUrl = !imageFile && product.image && product.image !== "/placeholder.svg"
        ? product.image
        : null;
      if (!product.id && !imageUrl && !imageFile) {
        throw new Error("Informe uma URL de imagem ou envie um arquivo.");
      }

      const payload = {
        slug,
        name: product.name,
        description: product.description,
        price: product.price,
        categorySlug,
        server,
        imageUrl: imageFile ? null : imageUrl,
        imageFile,
      };

      const saved = product.id
        ? await adminService.updateProduct({ ...payload, routeSlug: slug })
        : await adminService.createProduct(payload);
      const stockDelta = product.stock - (product.id ? initialStock : saved.stock);

      if (stockDelta !== 0) {
        await adminService.adjustInventory(saved.id, stockDelta, product.id ? "Ajuste de estoque ao editar produto" : "Estoque inicial do produto");
      }

      return saved;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin", "products"] });
      toast.success("Produto salvo no catalogo");
      closeForm();
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel salvar o produto.");
    },
  });

  const deleteProduct = useMutation({
    mutationFn: async (product: Product) => {
      const slug = product.slug ?? product.id;
      await adminService.deleteProduct(slug);
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

  return (
    <AdminLayout>
      <div className="flex items-center justify-between mb-6">
        <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">Produtos</h1>
        <button onClick={openNewProductForm} className="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
          <Plus size={16} /> Novo produto
        </button>
      </div>

      {isLoading && <p className="mb-4 rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">Carregando produtos...</p>}
      {isError && <p className="mb-4 rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">Nao foi possivel carregar os produtos.</p>}

      {showForm && editing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4">
          <div className="w-full max-w-lg rounded-xl border border-border bg-card p-6 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-display text-base font-semibold text-foreground">{editing.id ? "Editar produto" : "Novo produto"}</h2>
              <button onClick={closeForm} className="text-muted-foreground hover:text-foreground"><X size={20} /></button>
            </div>
            <form onSubmit={(event) => { event.preventDefault(); saveProduct.mutate(editing); }} className="space-y-3">
              <div><label className="text-xs text-muted-foreground">Nome</label><input value={editing.name} onChange={(event) => setEditing({ ...editing, name: event.target.value, slug: editing.id ? editing.slug : adminService.buildSlug(event.target.value) })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <div><label className="text-xs text-muted-foreground">Slug</label><input value={editing.slug ?? ""} onChange={(event) => setEditing({ ...editing, slug: event.target.value })} required disabled={Boolean(editing.id)} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground disabled:opacity-60 focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground">Categoria</label><select value={editing.categorySlug ?? "coin"} onChange={(event) => setEditing({ ...editing, categorySlug: event.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary">{CATEGORY_OPTIONS.map((category) => <option key={category.slug} value={category.slug}>{category.label}</option>)}</select></div>
                <div>
                  <label className="text-xs text-muted-foreground">Mundo</label>
                  <input
                    value={editing.server}
                    onChange={(event) => setEditing({ ...editing, server: event.target.value })}
                    list="product-world-options"
                    required={editing.categorySlug === "gold"}
                    placeholder="Aurera"
                    className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  />
                  <datalist id="product-world-options">
                    {WORLD_OPTIONS.map((world) => <option key={world} value={world} />)}
                  </datalist>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground">Preco (R$)</label><input type="number" step="0.01" value={editing.price} onChange={(event) => setEditing({ ...editing, price: Number(event.target.value) })} required className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
                <div><label className="text-xs text-muted-foreground">Estoque disponivel</label><input type="number" min="0" value={editing.stock} onChange={(event) => setEditing({ ...editing, stock: Number(event.target.value) })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              </div>
              <div>
                <div className="mb-2 flex items-center justify-between gap-3">
                  <label className="text-xs text-muted-foreground">Imagem do produto</label>
                  <div className="flex rounded-lg border border-border p-1 text-xs">
                    <button type="button" onClick={() => { setImageMode("url"); clearImageFile(); }} className={`rounded-md px-3 py-1 ${imageMode === "url" ? "bg-primary text-primary-foreground" : "text-muted-foreground"}`}>URL</button>
                    <button type="button" onClick={() => setImageMode("upload")} className={`rounded-md px-3 py-1 ${imageMode === "upload" ? "bg-primary text-primary-foreground" : "text-muted-foreground"}`}>Upload</button>
                  </div>
                </div>
                {imageMode === "url" ? (
                  <input
                    type="url"
                    value={editing.image === "/placeholder.svg" ? "" : editing.image}
                    onChange={(event) => setEditing({ ...editing, image: event.target.value })}
                    placeholder="https://exemplo.com/imagem-produto.png"
                    className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  />
                ) : (
                  <input
                    type="file"
                    accept=".png,.jpg,.jpeg,.webp,image/png,image/jpeg,image/webp"
                    onChange={(event) => {
                      const file = event.target.files?.[0] ?? null;
                      clearImageFile();
                      if (!file) {
                        return;
                      }

                      if (!ACCEPTED_IMAGE_TYPES.includes(file.type) || !/\.(png|jpe?g|webp)$/i.test(file.name)) {
                        toast.error("Envie uma imagem PNG, JPG, JPEG ou WEBP.");
                        event.target.value = "";
                        return;
                      }

                      if (file.size > MAX_IMAGE_SIZE_BYTES) {
                        toast.error("A imagem deve ter no maximo 5MB.");
                        event.target.value = "";
                        return;
                      }

                      const previewUrl = URL.createObjectURL(file);
                      setImageFile(file);
                      setImagePreviewUrl(previewUrl);
                    }}
                    className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-primary-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  />
                )}
                <ProductImage src={imagePreviewUrl ?? editing.image} alt={editing.name || "Imagem do produto"} fallbackLabel={editing.category} className="mt-3 h-28 w-full" />
              </div>
              <div><label className="text-xs text-muted-foreground">Descricao</label><textarea value={editing.description} onChange={(event) => setEditing({ ...editing, description: event.target.value })} rows={3} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
              <button type="submit" disabled={saveProduct.isPending} className="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60">{saveProduct.isPending ? "Salvando..." : "Salvar"}</button>
            </form>
          </div>
        </div>
      )}

      <div className="rounded-xl border border-border bg-card overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase text-muted-foreground">
              <th className="px-4 py-3">Nome</th>
              <th className="px-4 py-3">Imagem</th>
              <th className="px-4 py-3">Categoria</th>
              <th className="px-4 py-3">Mundo</th>
              <th className="px-4 py-3 text-right">Preco</th>
              <th className="px-4 py-3 text-right">Estoque</th>
              <th className="px-4 py-3 text-right">Acoes</th>
            </tr>
          </thead>
          <tbody>
            {productList.map((product) => (
              <tr key={product.id} className="border-b border-border last:border-0">
                <td className="px-4 py-3 font-medium text-foreground">{product.name}</td>
                <td className="px-4 py-3">
                  <ProductImage src={product.image} alt={product.name} fallbackLabel={product.category} className="h-12 w-16" />
                </td>
                <td className="px-4 py-3 text-muted-foreground">{product.category}</td>
                <td className="px-4 py-3 text-muted-foreground">{product.server}</td>
                <td className="px-4 py-3 text-right text-foreground">R$ {product.price.toFixed(2)}</td>
                <td className="px-4 py-3 text-right"><span className={product.stock < 10 ? "text-destructive" : "text-muted-foreground"}>{product.stock}</span></td>
                <td className="px-4 py-3 text-right">
                  <div className="flex justify-end gap-2">
                    <button onClick={() => openEditProductForm(product)} className="inline-flex items-center gap-1 rounded-lg border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary/40 hover:text-primary">
                      <Pencil size={14} /> Editar
                    </button>
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
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
};

export default AdminProducts;
