export interface Product {
  id: string;
  name: string;
  slug?: string;
  category: string;
  categorySlug?: string;
  categoryId?: string;
  server: string;
  serverId?: string;
  price: number;
  originalPrice?: number;
  description: string;
  image: string;
  stock: number;
  rating: number;
  reviewCount: number;
  sales: number;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  sku?: string;
  featured?: boolean;
}

export interface Category {
  id: string;
  name: string;
  slug?: string;
}

export interface Server {
  id: string;
  name: string;
  slug?: string;
}
