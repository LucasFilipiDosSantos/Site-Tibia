import { orders as mockOrders } from "@/data/mockData";
import type { Order } from "../types/order.types";

export const orderService = {
  getOrders: (): Order[] => {
    return mockOrders;
  },

  getOrderById: (id: string): Order | undefined => {
    return mockOrders.find(o => o.id === id);
  },

  getOrdersByUserId: (userId: string): Order[] => {
    return mockOrders.filter(o => o.userId === userId);
  },

  createOrder: (order: Omit<Order, "id" | "createdAt">): Order => {
    const newOrder: Order = {
      ...order,
      id: `ORD-${Date.now()}`,
      createdAt: new Date().toISOString(),
    };
    // In mock, just return; in real, save to backend
    return newOrder;
  },
};