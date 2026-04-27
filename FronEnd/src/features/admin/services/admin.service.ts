import { apiRequest } from "@/lib/api";
import type { Product } from "@/features/products/types/product.types";
import { getCategoryLabel, removeAureraFromText } from "@/features/products/utils/catalog";

export type AdminOrder = {
  id: string;
  orderIntentKey: string;
  createdAt: string;
  status: "pending" | "paid" | "cancelled" | string;
  statusLabel: string;
  customerName?: string | null;
  customerEmail?: string | null;
  customerDiscord?: string | null;
  paymentMethod?: string | null;
  totalAmount: number;
  itemCount: number;
};

export type AdminOrderInput = {
  id: string;
  customerName: string;
  customerEmail: string;
  customerDiscord?: string | null;
  paymentMethod?: string | null;
  status: string;
};

export type AdminUser = {
  id: string;
  name: string;
  email: string;
  role: "admin" | "customer";
  emailVerified: boolean;
  createdAt: string;
};

export type AdminUserInput = {
  id: string;
  name: string;
  email: string;
  role: "admin" | "customer";
  emailVerified: boolean;
  newPassword?: string;
};

type ProductResponse = {
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

type ProductListResponse = {
  items: ProductResponse[];
};

type OrderListResponse = {
  items: Array<{
    orderId: string;
    orderIntentKey: string;
    createdAtUtc: string;
    statusCode: string;
    statusLabel: string;
    customerName?: string | null;
    customerEmail?: string | null;
    customerDiscord?: string | null;
    paymentMethod?: string | null;
    totalAmount?: number;
    itemCount?: number;
  }>;
};

type UserListResponse = {
  items: Array<{
    id: string;
    name: string;
    email: string;
    role: "admin" | "customer";
    emailVerified: boolean;
    createdAtUtc: string;
  }>;
};

export type AdminProductInput = {
  slug: string;
  name: string;
  description: string;
  price: number;
  categorySlug: string;
  server: string;
  imageUrl?: string | null;
};

export type AdminProductUpdateInput = AdminProductInput & {
  routeSlug: string;
};

const toProduct = (product: ProductResponse): Product => ({
  id: product.id ?? product.slug,
  slug: product.slug,
  name: removeAureraFromText(product.name),
  category: removeAureraFromText(getCategoryLabel(product.categorySlug)),
  categorySlug: product.categorySlug,
  server: product.server ?? "",
  price: product.price,
  description: removeAureraFromText(product.description),
  image: product.imageUrl || "/placeholder.svg",
  stock: product.availableStock ?? 0,
  rating: product.rating ?? 0,
  sales: product.salesCount ?? 0,
});

const toSlug = (value: string) =>
  value
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

export const adminService = {
  async getProducts(): Promise<Product[]> {
    const response = await apiRequest<ProductListResponse>("/products?page=1&pageSize=100", { auth: true });
    return response.items.map(toProduct);
  },

  async createProduct(input: AdminProductInput): Promise<Product> {
    const response = await apiRequest<ProductResponse>("/admin/catalog/products", {
      auth: true,
      method: "POST",
      body: JSON.stringify(input),
    });
    return toProduct(response);
  },

  async updateProduct(input: AdminProductUpdateInput): Promise<Product> {
    const { routeSlug, ...payload } = input;
    const response = await apiRequest<ProductResponse>(`/admin/catalog/products/${encodeURIComponent(routeSlug)}`, {
      auth: true,
      method: "PUT",
      body: JSON.stringify(payload),
    });
    return toProduct(response);
  },

  async deleteProduct(slug: string): Promise<void> {
    await apiRequest(`/admin/catalog/products/${encodeURIComponent(slug)}`, {
      auth: true,
      method: "DELETE",
    });
  },

  async adjustInventory(productId: string, delta: number, reason: string): Promise<void> {
    await apiRequest("/admin/inventory/adjustments", {
      auth: true,
      method: "POST",
      body: JSON.stringify({ productId, delta, reason }),
    });
  },

  async getOrders(): Promise<AdminOrder[]> {
    const response = await apiRequest<OrderListResponse>("/admin/orders?page=1&pageSize=50", { auth: true });
    return response.items.map((order) => ({
      id: order.orderId,
      orderIntentKey: order.orderIntentKey,
      createdAt: order.createdAtUtc,
      status: order.statusCode.toLowerCase(),
      statusLabel: order.statusLabel,
      customerName: order.customerName,
      customerEmail: order.customerEmail,
      customerDiscord: order.customerDiscord,
      paymentMethod: order.paymentMethod,
      totalAmount: order.totalAmount ?? 0,
      itemCount: order.itemCount ?? 0,
    }));
  },

  async updateOrder(input: AdminOrderInput): Promise<AdminOrder> {
    const response = await apiRequest<OrderListResponse["items"][number]>(`/admin/orders/${encodeURIComponent(input.id)}`, {
      auth: true,
      method: "PUT",
      body: JSON.stringify({
        customerName: input.customerName,
        customerEmail: input.customerEmail,
        customerDiscord: input.customerDiscord || null,
        paymentMethod: input.paymentMethod || null,
        status: input.status,
      }),
    });

    return {
      id: response.orderId,
      orderIntentKey: response.orderIntentKey,
      createdAt: response.createdAtUtc,
      status: response.statusCode.toLowerCase(),
      statusLabel: response.statusLabel,
      customerName: response.customerName,
      customerEmail: response.customerEmail,
      customerDiscord: response.customerDiscord,
      paymentMethod: response.paymentMethod,
      totalAmount: response.totalAmount ?? 0,
      itemCount: response.itemCount ?? 0,
    };
  },

  async deleteOrder(orderId: string): Promise<void> {
    await apiRequest(`/admin/orders/${encodeURIComponent(orderId)}`, {
      auth: true,
      method: "DELETE",
    });
  },

  async getUsers(): Promise<AdminUser[]> {
    const response = await apiRequest<UserListResponse>("/admin/users?page=1&pageSize=100", { auth: true });
    return response.items.map((user) => ({
      id: user.id,
      name: user.name,
      email: user.email,
      role: user.role,
      emailVerified: user.emailVerified,
      createdAt: user.createdAtUtc,
    }));
  },

  async updateUser(input: AdminUserInput): Promise<AdminUser> {
    const user = await apiRequest<UserListResponse["items"][number]>(`/admin/users/${encodeURIComponent(input.id)}`, {
      auth: true,
      method: "PUT",
      body: JSON.stringify({
        name: input.name,
        email: input.email,
        role: input.role,
        emailVerified: input.emailVerified,
        newPassword: input.newPassword || null,
      }),
    });

    return {
      id: user.id,
      name: user.name,
      email: user.email,
      role: user.role,
      emailVerified: user.emailVerified,
      createdAt: user.createdAtUtc,
    };
  },

  buildSlug: toSlug,
};
