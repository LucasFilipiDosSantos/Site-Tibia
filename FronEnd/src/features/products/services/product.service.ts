import { apiRequest } from "@/lib/api";
import type { Product } from "../types/product.types";
import { getCategoryLabel } from "../utils/catalog";

type ProductListItemResponse = {
  id?: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  categorySlug: string;
  imageUrl?: string | null;
  server?: string;
  availableStock?: number;
  rating?: number;
  salesCount?: number;
};

type ProductResponse = ProductListItemResponse;

type ProductListResponse = {
  items: ProductListItemResponse[];
  page: number;
  pageSize: number;
  pagination: {
    hasPreviousPage: boolean;
    hasNextPage: boolean;
  };
};

const PRODUCT_DISPLAY_OVERRIDES: Record<string, Partial<Pick<Product, "name" | "server" | "category">>> = {
  "100kk-gold-coins": { name: "Coin Aurera 100kk", server: "Aurera", category: "Coin Aurera" },
  "50kk-gold-coins": { name: "Coin Aurera 50kk", server: "Aurera", category: "Coin Aurera" },
  "200kk-gold-coins": { name: "Coin Aurera 200kk", server: "Aurera", category: "Coin Aurera" },
  "10kk-gold-coins": { name: "Coin Aurera 10kk", server: "Aurera", category: "Coin Aurera" },
  "coin-aurera-100kk": { name: "Coin Aurera 100kk", server: "Aurera", category: "Coin Aurera" },
  "coin-aurera-50kk": { name: "Coin Aurera 50kk", server: "Aurera", category: "Coin Aurera" },
  "magic-sword": { name: "Itens Aurera - Magic Sword", server: "Aurera", category: "Itens" },
  "elite-knight-500": { name: "Personagem Aurera - Elite Knight 500", server: "Aurera", category: "Personagens" },
  "master-sorcerer-350": { name: "Personagem Aurera - Master Sorcerer 350", server: "Aurera", category: "Personagens" },
  "annihilator-service": { name: "Servico Aurera - Annihilator", server: "Aurera", category: "Servicos" },
};

export type ProductListFilters = {
  category?: string;
  slug?: string;
  page?: number;
  pageSize?: number;
};

const inferServer = (name: string, description: string): string => {
  const text = `${name} ${description}`.toLowerCase();

  if (text.includes("aurera") || text.includes("coin") || text.includes("gold") || text.includes("dinheiro")) {
    return "Aurera";
  }

  return "Aurera";
};

const toProduct = (product: ProductListItemResponse | ProductResponse): Product => ({
  id: product.id ?? product.slug,
  slug: product.slug,
  name: PRODUCT_DISPLAY_OVERRIDES[product.slug]?.name ?? product.name,
  category: PRODUCT_DISPLAY_OVERRIDES[product.slug]?.category ?? getCategoryLabel(product.categorySlug),
  categorySlug: product.categorySlug,
  price: product.price,
  description: product.description,
  image: product.imageUrl || "/placeholder.svg",
  stock: product.availableStock ?? 0,
  rating: product.rating ?? 0,
  sales: product.salesCount ?? 0,
  server: product.server || PRODUCT_DISPLAY_OVERRIDES[product.slug]?.server || inferServer(product.name, product.description),
});

const buildQueryString = (filters: ProductListFilters = {}): string => {
  const params = new URLSearchParams();

  if (filters.category) {
    params.set("category", filters.category);
  }

  if (filters.slug) {
    params.set("slug", filters.slug);
  }

  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 12));

  const query = params.toString();
  return query ? `?${query}` : "";
};

export const productService = {
  async getProducts(filters: ProductListFilters = {}): Promise<Product[]> {
    const response = await apiRequest<ProductListResponse>(`/products${buildQueryString(filters)}`);
    return response.items.map(toProduct);
  },

  async getProductBySlug(slug: string): Promise<Product> {
    const response = await apiRequest<ProductResponse>(`/products/${slug}`);
    return toProduct(response);
  },
};
