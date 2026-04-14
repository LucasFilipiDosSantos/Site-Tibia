import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from "react";
import { toast } from "sonner";
import { cartService } from "@/features/cart/services/cart.service";

export interface CartItem {
  productId: string;
  name: string;
  price: number;
  quantity: number;
  server: string;
  image: string;
}

interface CartContextType {
  items: CartItem[];
  addItem: (item: Omit<CartItem, "quantity">, qty?: number) => void;
  removeItem: (productId: string) => void;
  updateQuantity: (productId: string, quantity: number) => void;
  clearCart: () => void;
  total: number;
  itemCount: number;
}

const CartContext = createContext<CartContextType | undefined>(undefined);

export const CartProvider = ({ children }: { children: ReactNode }) => {
  const [cart, setCart] = useState(() => cartService.getCart());

  useEffect(() => {
    cartService.saveCart(cart);
  }, [cart]);

  const addItem = useCallback(
    (item: Omit<CartItem, "quantity">, qty = 1) => {
      const newCart = cartService.addItem({ ...item, quantity: qty }, cart);
      setCart(newCart);
      toast.success(`${item.name} adicionado ao carrinho`);
    },
    [cart]
  );

  const removeItem = useCallback(
    (productId: string) => {
      const newCart = cartService.removeItem(productId, cart);
      setCart(newCart);
      toast.info("Item removido do carrinho");
    },
    [cart]
  );

  const updateQuantity = useCallback(
    (productId: string, quantity: number) => {
      const newCart = cartService.updateQuantity(productId, quantity, cart);
      setCart(newCart);
    },
    [cart]
  );

  const clearCart = useCallback(() => {
    const newCart = cartService.clearCart();
    setCart(newCart);
    toast.info("Carrinho limpo");
  }, []);

  const total = cart.total;
  const itemCount = cart.items.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <CartContext.Provider
      value={{
        items: cart.items,
        addItem,
        removeItem,
        updateQuantity,
        clearCart,
        total,
        itemCount,
      }}
    >
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => {
  const ctx = useContext(CartContext);

  if (!ctx) {
    throw new Error("useCart must be used within CartProvider");
  }

  return ctx;
};