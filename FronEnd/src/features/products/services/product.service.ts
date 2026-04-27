import { ApiError, apiRequest } from "@/lib/api";
import type { Product } from "../types/product.types";
import { getCategoryLabel, removeAureraFromText } from "../utils/catalog";

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
  reviewCount?: number;
  salesCount?: number;
};

export type ProductReview = {
  userId: string;
  productId: string;
  rating: number;
  comment?: string | null;
  createdAtUtc: string;
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

const PRODUCT_DISPLAY_OVERRIDES: Record<string, Partial<Pick<Product, "name" | "category">>> = {
  "100kk-gold-coins": { name: "Coin 100kk", category: "Coin" },
  "50kk-gold-coins": { name: "Coin 50kk", category: "Coin" },
  "200kk-gold-coins": { name: "Coin 200kk", category: "Coin" },
  "10kk-gold-coins": { name: "Coin 10kk", category: "Coin" },
  "coin-aurera-100kk": { name: "Coin 100kk", category: "Coin" },
  "coin-aurera-50kk": { name: "Coin 50kk", category: "Coin" },
  "magic-sword": { name: "Itens - Magic Sword", category: "Itens" },
  "elite-knight-500": { name: "Personagem - Elite Knight 500", category: "Personagens" },
  "master-sorcerer-350": { name: "Personagem - Master Sorcerer 350", category: "Personagens" },
  "annihilator-service": { name: "Servico - Annihilator", category: "Servicos" },
};

export type ProductListFilters = {
  category?: string;
  slug?: string;
  page?: number;
  pageSize?: number;
};

const toProduct = (product: ProductListItemResponse | ProductResponse): Product => ({
  id: product.id ?? product.slug,
  slug: product.slug,
  name: removeAureraFromText(PRODUCT_DISPLAY_OVERRIDES[product.slug]?.name ?? product.name),
  category: removeAureraFromText(PRODUCT_DISPLAY_OVERRIDES[product.slug]?.category ?? getCategoryLabel(product.categorySlug)),
  categorySlug: product.categorySlug,
  price: product.price,
  description: removeAureraFromText(product.description),
  image: product.imageUrl || "/placeholder.svg",
  stock: product.availableStock ?? 0,
  rating: product.rating ?? 0,
  reviewCount: product.reviewCount ?? 0,
  sales: product.salesCount ?? 0,
  server: product.server ?? "",
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

  async getMyReview(slug: string): Promise<ProductReview | null> {
    try {
      return await apiRequest<ProductReview>(`/products/${slug}/reviews/me`, { auth: true });
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        return null;
      }

      throw error;
    }
  },

  async getReviews(slug: string): Promise<ProductReview[]> {
    return apiRequest<ProductReview[]>(`/products/${slug}/reviews`);
  },

  async createReview(slug: string, input: { rating: number; comment?: string | null }): Promise<ProductReview> {
    return apiRequest<ProductReview>(`/products/${slug}/reviews`, {
      auth: true,
      method: "POST",
      body: JSON.stringify(input),
    });
  },
};
