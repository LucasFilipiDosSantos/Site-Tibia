import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Minus, Plus, Shield, ShoppingCart } from "lucide-react";
import PublicLayout from "@/components/lootera/PublicLayout";
import { ProductImage } from "@/components/lootera/ProductImage";
import { StarRating, StarRatingInput } from "@/components/lootera/StarRating";
import { useCart } from "@/contexts/CartContext";
import { useAuth } from "@/features/auth/context/AuthContext";
import { useProduct, useProducts } from "@/features/products/hooks/useProducts";
import { productService } from "@/features/products/services/product.service";
import type { Product } from "@/features/products/types/product.types";
import { toast } from "sonner";

const ProductRecommendations = ({ products, title }: { products: Product[]; title: string }) => {
  if (products.length === 0) {
    return null;
  }

  return (
    <section className="mt-12">
      <h2 className="mb-4 font-body text-lg font-semibold text-foreground">{title}</h2>
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {products.map((relatedProduct) => (
          <Link key={relatedProduct.slug ?? relatedProduct.id} to={`/produto/${relatedProduct.slug ?? relatedProduct.id}`} className="font-body flex flex-col items-start rounded-xl border border-border bg-card p-4 text-left transition-all hover:border-primary/30">
            <ProductImage src={relatedProduct.image} alt={relatedProduct.name} fallbackLabel={relatedProduct.category} className="mb-2 h-20" />
            <h3 className="font-body line-clamp-1 text-sm text-foreground">{relatedProduct.name}</h3>
            <p className="mt-2 text-xs text-muted-foreground">
              <span className="inline-flex rounded border border-border bg-muted/40 px-1.5 py-0.5 text-[10px] leading-none text-foreground">{relatedProduct.server}</span>
              <span className="mx-1.5 text-muted-foreground">|</span>
              {relatedProduct.stock} disponiveis
            </p>
            <StarRating rating={relatedProduct.reviewCount > 0 ? relatedProduct.rating : null} className="mt-2" />
            {relatedProduct.sales > 0 && <p className="mt-1 text-xs text-muted-foreground">{relatedProduct.sales} vendidos</p>}
            <p className="mt-auto pt-3 text-base font-semibold text-primary">R$ {relatedProduct.price.toFixed(2)}</p>
          </Link>
        ))}
      </div>
    </section>
  );
};

const ProductDetail = () => {
  const { slug = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();
  const { addItem } = useCart();
  const [quantity, setQuantity] = useState(1);
  const [reviewRating, setReviewRating] = useState(0);
  const [reviewComment, setReviewComment] = useState("");
  const { data: product, isLoading, isError } = useProduct(slug);
  const { data: myReview } = useQuery({
    queryKey: ["product-review", slug, "me"],
    queryFn: () => productService.getMyReview(slug),
    enabled: Boolean(slug) && isAuthenticated,
  });
  const { data: relatedProducts = [], isLoading: isLoadingRecommendations } = useProducts({
    category: product?.categorySlug,
    page: 1,
    pageSize: 8,
  });
  const { data: fallbackProducts = [], isLoading: isLoadingFallbackRecommendations } = useProducts({
    page: 1,
    pageSize: 8,
  });

  const related = useMemo(
    () => relatedProducts.filter((candidate) => candidate.slug !== product?.slug).slice(0, 4),
    [product?.slug, relatedProducts],
  );
  const fallbackRecommendations = useMemo(
    () => fallbackProducts.filter((candidate) => candidate.slug !== product?.slug).slice(0, 4),
    [product?.slug, fallbackProducts],
  );
  const recommendations = related.length > 0 ? related : fallbackRecommendations;
  const productServer = product?.server?.trim() ? product.server : "Nao informado";
  const hasReviews = (product?.reviewCount ?? 0) > 0;

  const submitReview = useMutation({
    mutationFn: async () => {
      if (!isAuthenticated) {
        throw new Error("Faça login para avaliar este produto.");
      }

      if (reviewRating < 1) {
        throw new Error("Selecione de 1 a 5 estrelas para enviar sua avaliacao.");
      }

      return productService.createReview(slug, {
        rating: reviewRating,
        comment: reviewComment.trim() || null,
      });
    },
    onSuccess: async () => {
      toast.success("Avaliacao enviada com sucesso.");
      setReviewComment("");
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["product", slug] }),
        queryClient.invalidateQueries({ queryKey: ["products"] }),
        queryClient.invalidateQueries({ queryKey: ["product-review", slug, "me"] }),
      ]);
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel enviar sua avaliacao.");
    },
  });

  if (isLoading) {
    return (
      <PublicLayout>
        <div className="container mx-auto px-4 py-20 text-center text-sm text-muted-foreground">Carregando produto...</div>
      </PublicLayout>
    );
  }

  if (isError || !product) {
    return (
      <PublicLayout>
        <div className="container mx-auto px-4 py-12 lg:py-16">
          <Link to="/produtos" className="mb-6 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
            <ArrowLeft size={14} /> Voltar
          </Link>

          <div className="max-w-2xl text-left">
            <h1 className="font-body text-2xl font-semibold text-foreground">Produto nao encontrado</h1>
            <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
              Nao encontramos um produto para este link. Veja algumas opcoes disponiveis no catalogo.
            </p>
          </div>

          {isLoadingRecommendations || isLoadingFallbackRecommendations ? (
            <div className="mt-10 text-sm text-muted-foreground">Carregando recomendacoes...</div>
          ) : (
            <ProductRecommendations products={fallbackRecommendations} title="Produtos recomendados" />
          )}
        </div>
      </PublicLayout>
    );
  }

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <Link to="/produtos" className="mb-6 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
          <ArrowLeft size={14} /> Voltar
        </Link>

        <div className="grid gap-8 lg:grid-cols-2">
          <ProductImage src={product.image} alt={product.name} fallbackLabel={product.category} className="h-64 rounded-xl border border-border bg-card lg:h-96" />

          <div>
            <span className="mb-2 inline-block rounded-md bg-primary/15 px-2 py-1 text-xs font-medium text-primary">{product.category}</span>
            <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">{product.name}</h1>
            <p className="mt-1 text-sm text-muted-foreground">Servidor: {productServer}</p>
            <p className="mt-1 text-sm text-muted-foreground">{product.stock} disponiveis</p>
            <StarRating rating={hasReviews ? product.rating : null} className="mt-2" showValue showFallbackText />
            {hasReviews && <p className="mt-1 text-sm text-muted-foreground">{product.reviewCount} avaliacao(oes)</p>}
            {product.sales > 0 && <p className="mt-1 text-sm text-muted-foreground">{product.sales} vendidos</p>}

            <div className="mt-6">
              <p className="text-3xl font-bold text-primary">R$ {product.price.toFixed(2)}</p>
            </div>

            <p className="mt-4 text-sm leading-relaxed text-muted-foreground">{product.description}</p>

            <div className="mt-6 flex items-center gap-4">
              <div className="flex items-center rounded-lg border border-border">
                <button onClick={() => setQuantity(Math.max(1, quantity - 1))} className="px-3 py-2 text-muted-foreground transition-colors hover:text-foreground">
                  <Minus size={16} />
                </button>
                <span className="min-w-[2rem] text-center text-sm font-medium text-foreground">{quantity}</span>
                <button onClick={() => setQuantity(quantity + 1)} className="px-3 py-2 text-muted-foreground transition-colors hover:text-foreground">
                  <Plus size={16} />
                </button>
              </div>
              <button
                onClick={() =>
                  addItem(
                    {
                      productId: product.slug ?? product.id,
                      name: product.name,
                      price: product.price,
                      server: product.server,
                      image: product.image,
                    },
                    quantity,
                  )
                }
                className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-brand-gold px-6 py-3 text-sm font-medium text-background transition-colors hover:bg-brand-gold/90"
              >
                <ShoppingCart size={18} /> Adicionar ao carrinho
              </button>
            </div>

            <div className="mt-6 space-y-2">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Shield size={14} className="text-primary" /> Compra protegida com validacao de estoque antes da finalizacao
              </div>
            </div>

            <div className="mt-8 rounded-xl border border-border bg-card p-4">
              <h2 className="text-sm font-semibold text-foreground">Avaliar produto</h2>

              {!isAuthenticated && (
                <div className="mt-3 space-y-3">
                  <p className="text-sm text-muted-foreground">Faça login para avaliar este produto.</p>
                  <button
                    type="button"
                    onClick={() => navigate("/login", { state: { from: { pathname: `/produto/${slug}` } } })}
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                  >
                    Fazer login
                  </button>
                </div>
              )}

              {isAuthenticated && myReview && (
                <div className="mt-3 space-y-2">
                  <p className="text-sm text-muted-foreground">Você já avaliou este produto.</p>
                  <StarRating rating={myReview.rating} showValue />
                  {myReview.comment && <p className="text-sm text-muted-foreground">{myReview.comment}</p>}
                </div>
              )}

              {isAuthenticated && !myReview && (
                <form
                  onSubmit={(event) => {
                    event.preventDefault();
                    submitReview.mutate();
                  }}
                  className="mt-3 space-y-3"
                >
                  <div>
                    <label className="text-xs text-muted-foreground">Nota</label>
                    <StarRatingInput
                      value={reviewRating}
                      onChange={setReviewRating}
                      className="mt-2"
                      showValue
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">Comentario opcional</label>
                    <textarea
                      value={reviewComment}
                      onChange={(event) => setReviewComment(event.target.value)}
                      rows={3}
                      className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                    />
                  </div>
                  <button
                    type="submit"
                    disabled={submitReview.isPending || reviewRating < 1}
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                  >
                    {submitReview.isPending ? "Enviando..." : "Enviar avaliacao"}
                  </button>
                </form>
              )}
            </div>
          </div>
        </div>

        {isLoadingRecommendations || isLoadingFallbackRecommendations ? (
          <div className="mt-10 text-sm text-muted-foreground">Carregando recomendacoes...</div>
        ) : (
          <ProductRecommendations products={recommendations} title={related.length > 0 ? "Produtos relacionados" : "Produtos recomendados"} />
        )}
      </div>
    </PublicLayout>
  );
};

export default ProductDetail;
