import { products as mockProducts } from "@/data/mockData";
import type { Product } from "../types/product.types";

export const productService = {
  getProducts: (): Product[] => {
    return mockProducts;
  },

  getProductById: (id: string): Product | undefined => {
    return mockProducts.find(p => p.id === id);
  },

  getFeaturedProducts: (): Product[] => {
    return mockProducts.filter(p => p.featured);
  },

  getProductsByCategory: (category: string): Product[] => {
    return mockProducts.filter(p => p.category === category);
  },

  getProductsByServer: (server: string): Product[] => {
    return mockProducts.filter(p => p.server === server);
  },
};