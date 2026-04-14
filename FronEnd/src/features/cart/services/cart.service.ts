import type { Cart, CartItem } from "../types/cart.types";

const CART_STORAGE_KEY = "lootera_cart";

const safeParseJSON = <T>(value: string | null, fallback: T): T => {
  if (!value) return fallback;
  try {
    return JSON.parse(value);
  } catch {
    return fallback;
  }
};

export const cartService = {
  getCart: (): Cart => {
    const stored = localStorage.getItem(CART_STORAGE_KEY);
    const items: CartItem[] = safeParseJSON(stored, []);
    const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0);
    return { items, total };
  },

  saveCart: (cart: Cart): void => {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(cart.items));
  },

  addItem: (item: CartItem): Cart => {
    const cart = cartService.getCart();
    const existingIndex = cart.items.findIndex(i => i.productId === item.productId);
    if (existingIndex >= 0) {
      cart.items[existingIndex].quantity += item.quantity;
    } else {
      cart.items.push(item);
    }
    cart.total = cart.items.reduce((sum, i) => sum + i.price * i.quantity, 0);
    cartService.saveCart(cart);
    return cart;
  },

  updateQuantity: (productId: string, quantity: number): Cart => {
    const cart = cartService.getCart();
    const index = cart.items.findIndex(i => i.productId === productId);
    if (index >= 0) {
      if (quantity <= 0) {
        cart.items.splice(index, 1);
      } else {
        cart.items[index].quantity = quantity;
      }
    }
    cart.total = cart.items.reduce((sum, i) => sum + i.price * i.quantity, 0);
    cartService.saveCart(cart);
    return cart;
  },

  removeItem: (productId: string): Cart => {
    const cart = cartService.getCart();
    cart.items = cart.items.filter(i => i.productId !== productId);
    cart.total = cart.items.reduce((sum, i) => sum + i.price * i.quantity, 0);
    cartService.saveCart(cart);
    return cart;
  },

  clearCart: (): Cart => {
    const cart = { items: [], total: 0 };
    cartService.saveCart(cart);
    return cart;
  },
};