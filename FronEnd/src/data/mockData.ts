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
  { id: "1", name: "100kk Gold Coins", category: "Moedas", server: "Antica", price: 89.90, originalPrice: 110.00, description: "100kk de gold coins no servidor Antica. Entrega imediata via trade seguro no depot.", image: "/placeholder.svg", stock: 50, rating: 4.9, sales: 1240, featured: true },
  { id: "2", name: "50kk Gold Coins", category: "Moedas", server: "Secura", price: 49.90, description: "50kk de gold coins no servidor Secura. Entrega em até 5 minutos.", image: "/placeholder.svg", stock: 30, rating: 4.8, sales: 890 },
  { id: "3", name: "Power Level 1-200", category: "Quests", server: "Antica", price: 299.90, originalPrice: 399.90, description: "Power Level completo de 1 a 200 no servidor Antica. Inclui equipamentos básicos.", image: "/placeholder.svg", stock: 10, rating: 4.7, sales: 320, featured: true },
  { id: "4", name: "Elite Knight LV 500", category: "Personagens", server: "Secura", price: 1599.90, description: "Elite Knight nível 500 full skill. Equipado com best in slot. Transferência segura.", image: "/placeholder.svg", stock: 3, rating: 5.0, sales: 45 },
  { id: "5", name: "Bot Script Premium", category: "Scripts", server: "Todos", price: 39.90, description: "Script premium com proteção anti-kick. Suporte a todas as vocações. Atualizações grátis.", image: "/placeholder.svg", stock: 999, rating: 4.6, sales: 2100, featured: true },
  { id: "6", name: "Macro de Cura Avançado", category: "Macros", server: "Todos", price: 19.90, description: "Macro de cura automática para todas as vocações. Configurável e seguro.", image: "/placeholder.svg", stock: 999, rating: 4.5, sales: 1800 },
  { id: "7", name: "200kk Gold Coins", category: "Moedas", server: "Antica", price: 169.90, originalPrice: 199.90, description: "200kk de gold coins no servidor Antica. Melhor preço do mercado.", image: "/placeholder.svg", stock: 20, rating: 4.9, sales: 670 },
  { id: "8", name: "Quest Service - Annihilator", category: "Quests", server: "Secura", price: 59.90, description: "Serviço completo de quest Annihilator. Time experiente.", image: "/placeholder.svg", stock: 15, rating: 4.8, sales: 530 },
  { id: "9", name: "Master Sorcerer LV 350", category: "Personagens", server: "Antica", price: 899.90, description: "Master Sorcerer nível 350, magic level 95+. Pronto para war.", image: "/placeholder.svg", stock: 5, rating: 4.7, sales: 120 },
  { id: "10", name: "Pack Scripts Completo", category: "Scripts", server: "Todos", price: 79.90, originalPrice: 119.90, description: "Pacote com 10 scripts: hunt, loot, cura, magic training e mais.", image: "/placeholder.svg", stock: 999, rating: 4.8, sales: 950, featured: true },
  { id: "11", name: "10kk Gold Coins", category: "Moedas", server: "Gentebra", price: 12.90, description: "10kk de gold coins no servidor Gentebra.", image: "/placeholder.svg", stock: 100, rating: 4.6, sales: 3200 },
  { id: "12", name: "Macro AFK Training", category: "Macros", server: "Todos", price: 24.90, description: "Macro de treino AFK offline. Funciona com exercise weapons.", image: "/placeholder.svg", stock: 999, rating: 4.4, sales: 1400 },
];

export const orders: Order[] = [
  { id: "ORD-001", userId: "1", items: [{ productId: "1", name: "100kk Gold Coins", quantity: 1, price: 89.90 }], total: 89.90, status: "completed", createdAt: "2026-04-09T14:30:00Z", paymentMethod: "PIX" },
  { id: "ORD-002", userId: "1", items: [{ productId: "5", name: "Bot Script Premium", quantity: 1, price: 39.90 }, { productId: "6", name: "Macro de Cura Avançado", quantity: 2, price: 19.90 }], total: 79.70, status: "processing", createdAt: "2026-04-10T09:15:00Z", paymentMethod: "Cartão" },
  { id: "ORD-003", userId: "2", items: [{ productId: "3", name: "Power Level 1-200", quantity: 1, price: 299.90 }], total: 299.90, status: "pending", createdAt: "2026-04-10T11:00:00Z", paymentMethod: "PIX" },
  { id: "ORD-004", userId: "3", items: [{ productId: "4", name: "Elite Knight LV 500", quantity: 1, price: 1599.90 }], total: 1599.90, status: "completed", createdAt: "2026-04-08T16:45:00Z", paymentMethod: "Cartão" },
  { id: "ORD-005", userId: "2", items: [{ productId: "10", name: "Pack Scripts Completo", quantity: 1, price: 79.90 }], total: 79.90, status: "cancelled", createdAt: "2026-04-07T08:20:00Z", paymentMethod: "PIX" },
];

export const users: User[] = [
  { id: "1", name: "Carlos Silva", email: "carlos@email.com", role: "customer", createdAt: "2026-01-15T10:00:00Z", totalSpent: 169.60, ordersCount: 2 },
  { id: "2", name: "Ana Pereira", email: "ana@email.com", role: "customer", createdAt: "2026-02-20T14:00:00Z", totalSpent: 379.80, ordersCount: 2 },
  { id: "3", name: "Ricardo Santos", email: "ricardo@email.com", role: "customer", createdAt: "2026-03-10T09:00:00Z", totalSpent: 1599.90, ordersCount: 1 },
  { id: "admin", name: "Admin Lootera", email: "admin@lootera.com", role: "admin", createdAt: "2025-12-01T00:00:00Z", totalSpent: 0, ordersCount: 0 },
];

export const categories = ["Moedas", "Scripts", "Macros", "Personagens", "Quests"];
export const servers = ["Antica", "Secura", "Gentebra", "Todos"];
