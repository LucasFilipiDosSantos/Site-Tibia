export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: "customer" | "admin";
  createdAt: string;
  updatedAt?: string;
  totalSpent: number;
  ordersCount: number;
  isActive?: boolean;
}