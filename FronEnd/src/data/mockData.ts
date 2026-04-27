export interface Product {
  id: string;
  name: string;
  category: string;
  server: string;
  price: number;
  originalPrice?: number;
  description: string;
  image: string;
  stock: number;
  rating: number;
  sales: number;
  featured?: boolean;
}

export interface Order {
  id: string;
  userId: string;
  items: { productId: string; name: string; quantity: number; price: number }[];
  total: number;
  status: "pending" | "processing" | "completed" | "cancelled";
  createdAt: string;
  paymentMethod: string;
}

export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: "customer" | "admin";
  createdAt: string;
  totalSpent: number;
  ordersCount: number;
}

export const products: Product[] = [
  { id: "1", name: "Coin 100kk", category: "Coin", server: "", price: 89.90, originalPrice: 110.00, description: "100kk de coin no servidor. Entrega imediata via trade seguro no depot.", image: "/placeholder.svg", stock: 50, rating: 4.9, sales: 1240, featured: true },
  { id: "2", name: "Coin 50kk", category: "Coin", server: "", price: 49.90, description: "50kk de coin no servidor. Entrega em ate 5 minutos.", image: "/placeholder.svg", stock: 30, rating: 4.8, sales: 890 },
  { id: "3", name: "Itens - Magic Sword", category: "Itens", server: "", price: 49.90, description: "Magic Sword no servidor para hunts e colecao.", image: "/placeholder.svg", stock: 10, rating: 4.7, sales: 320, featured: true },
  { id: "5", name: "Personagem - Elite Knight LV 500", category: "Personagens", server: "", price: 1599.90, description: "Elite Knight nivel 500 no mundo. Transferencia segura.", image: "/placeholder.svg", stock: 3, rating: 5.0, sales: 45 },
  { id: "6", name: "Personagem - Master Sorcerer LV 350", category: "Personagens", server: "", price: 899.90, description: "Master Sorcerer nivel 350 no servidor, magic level 95+.", image: "/placeholder.svg", stock: 5, rating: 4.7, sales: 120 },
  { id: "7", name: "Servico - Annihilator", category: "Quests", server: "", price: 59.90, description: "Servico completo de quest Annihilator no servidor.", image: "/placeholder.svg", stock: 15, rating: 4.8, sales: 530 },
  { id: "8", name: "Servico - Acessos", category: "Quests", server: "", price: 79.90, description: "Servico de quests e acessos no servidor.", image: "/placeholder.svg", stock: 15, rating: 4.8, sales: 530 },
  { id: "9", name: "Encomenda de Script 100% AFK OTC", category: "Scripts", server: "", price: 79.90, originalPrice: 119.90, description: "Script sob encomenda para OTC com fluxo 100% AFK.", image: "/placeholder.svg", stock: 999, rating: 4.8, sales: 950, featured: true },
  { id: "10", name: "Encomenda de Macro", category: "Macros", server: "", price: 19.90, description: "Macro personalizada conforme sua necessidade.", image: "/placeholder.svg", stock: 999, rating: 4.5, sales: 1800 },
  { id: "11", name: "Macro Free", category: "Macros", server: "", price: 0, description: "Macro gratuito para gameplay assistida.", image: "/placeholder.svg", stock: 999, rating: 4.4, sales: 1400 },
  { id: "12", name: "Dashboard BotManager", category: "Servicos", server: "", price: 0, description: "Dashboard/app desenvolvido para acompanhamento e gestao.", image: "/placeholder.svg", stock: 999, rating: 4.6, sales: 3200 },
];

export const orders: Order[] = [
  { id: "ORD-001", userId: "1", items: [{ productId: "1", name: "Coin 100kk", quantity: 1, price: 89.90 }], total: 89.90, status: "completed", createdAt: "2026-04-09T14:30:00Z", paymentMethod: "PIX" },
  { id: "ORD-002", userId: "1", items: [{ productId: "9", name: "Encomenda de Script 100% AFK OTC", quantity: 1, price: 79.90 }], total: 79.90, status: "processing", createdAt: "2026-04-10T09:15:00Z", paymentMethod: "Cartao" },
  { id: "ORD-003", userId: "2", items: [{ productId: "7", name: "Servico - Annihilator", quantity: 1, price: 59.90 }], total: 59.90, status: "pending", createdAt: "2026-04-10T11:00:00Z", paymentMethod: "PIX" },
  { id: "ORD-004", userId: "3", items: [{ productId: "5", name: "Personagem - Elite Knight LV 500", quantity: 1, price: 1599.90 }], total: 1599.90, status: "completed", createdAt: "2026-04-08T16:45:00Z", paymentMethod: "Cartao" },
  { id: "ORD-005", userId: "2", items: [{ productId: "10", name: "Encomenda de Macro", quantity: 1, price: 19.90 }], total: 19.90, status: "cancelled", createdAt: "2026-04-07T08:20:00Z", paymentMethod: "PIX" },
];

export const users: User[] = [
  { id: "1", name: "Carlos Silva", email: "carlos@email.com", role: "customer", createdAt: "2026-01-15T10:00:00Z", totalSpent: 169.60, ordersCount: 2 },
  { id: "2", name: "Ana Pereira", email: "ana@email.com", role: "customer", createdAt: "2026-02-20T14:00:00Z", totalSpent: 379.80, ordersCount: 2 },
  { id: "3", name: "Ricardo Santos", email: "ricardo@email.com", role: "customer", createdAt: "2026-03-10T09:00:00Z", totalSpent: 1599.90, ordersCount: 1 },
  { id: "admin", name: "Admin Lootera", email: "admin@lootera.com", role: "admin", createdAt: "2025-12-01T00:00:00Z", totalSpent: 0, ordersCount: 0 },
];

export const categories = ["Coin", "Itens", "Scripts", "Macros", "Personagens", "Quests", "Servicos"];
export const servers = [""];
