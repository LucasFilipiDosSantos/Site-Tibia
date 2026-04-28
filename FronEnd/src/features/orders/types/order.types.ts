export interface Order {
  id: string;
  userId: string;
  items: OrderItem[];
  total: number;
  status: OrderStatus;
  statusLabel?: string;
  createdAt: string;
  updatedAt?: string;
  paymentMethod?: string | null;
  orderIntentKey?: string;
}

export interface OrderItem {
  productId: string;
  name: string;
  quantity: number;
  price: number;
}

export type OrderStatus = "pending" | "paid" | "cancelled" | string;
