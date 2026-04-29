import { apiClient } from "@/lib/api";
import type { Order } from "../types/order.types";

type OrderListResponse = {
  items: Array<{
    orderId: string;
    orderIntentKey: string;
    createdAtUtc: string;
    statusCode: string;
    statusLabel: string;
    paymentMethod?: string | null;
    totalAmount?: number;
    itemCount?: number;
  }>;
  page: number;
  pageSize: number;
  totalCount: number;
};

const mapOrder = (order: OrderListResponse["items"][number]): Order => ({
  id: order.orderId,
  userId: "",
  items: [],
  total: order.totalAmount ?? 0,
  status: order.statusCode.toLowerCase(),
  statusLabel: order.statusLabel,
  createdAt: order.createdAtUtc,
  paymentMethod: order.paymentMethod,
  orderIntentKey: order.orderIntentKey,
});

export const orderService = {
  async getMyOrders(): Promise<Order[]> {
    const response = await apiClient.get<OrderListResponse>("/orders?page=1&pageSize=100");

    return response.items.map(mapOrder);
  },
};
