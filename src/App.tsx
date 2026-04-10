import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import { CartProvider } from "@/contexts/CartContext";
import { AuthProvider } from "@/contexts/AuthContext";

// Layout variations (original)
import Index from "./pages/Index.tsx";
import Variation1 from "./pages/Variation1.tsx";
import Variation2 from "./pages/Variation2.tsx";
import Variation3 from "./pages/Variation3.tsx";
import Variation4 from "./pages/Variation4.tsx";

// Shop pages
import Home from "./pages/shop/Home.tsx";
import Products from "./pages/shop/Products.tsx";
import ProductDetail from "./pages/shop/ProductDetail.tsx";
import Cart from "./pages/shop/Cart.tsx";
import Checkout from "./pages/shop/Checkout.tsx";

// Auth pages
import Login from "./pages/auth/Login.tsx";
import Register from "./pages/auth/Register.tsx";

// User pages
import Profile from "./pages/user/Profile.tsx";
import OrderHistory from "./pages/user/OrderHistory.tsx";

// Admin pages
import Dashboard from "./pages/admin/Dashboard.tsx";
import AdminProducts from "./pages/admin/AdminProducts.tsx";
import AdminOrders from "./pages/admin/AdminOrders.tsx";
import AdminUsers from "./pages/admin/AdminUsers.tsx";
import AdminInventory from "./pages/admin/AdminInventory.tsx";
import AdminSettings from "./pages/admin/AdminSettings.tsx";

import NotFound from "./pages/NotFound.tsx";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <AuthProvider>
        <CartProvider>
          <Toaster />
          <Sonner />
          <BrowserRouter>
            <Routes>
              {/* Layout variations */}
              <Route path="/variacoes" element={<Index />} />
              <Route path="/variation-1" element={<Variation1 />} />
              <Route path="/variation-2" element={<Variation2 />} />
              <Route path="/variation-3" element={<Variation3 />} />
              <Route path="/variation-4" element={<Variation4 />} />

              {/* E-commerce */}
              <Route path="/" element={<Home />} />
              <Route path="/produtos" element={<Products />} />
              <Route path="/produto/:id" element={<ProductDetail />} />
              <Route path="/carrinho" element={<Cart />} />
              <Route path="/checkout" element={<Checkout />} />

              {/* Auth */}
              <Route path="/login" element={<Login />} />
              <Route path="/cadastro" element={<Register />} />

              {/* User */}
              <Route path="/perfil" element={<Profile />} />
              <Route path="/pedidos" element={<OrderHistory />} />

              {/* Admin */}
              <Route path="/admin" element={<Dashboard />} />
              <Route path="/admin/produtos" element={<AdminProducts />} />
              <Route path="/admin/pedidos" element={<AdminOrders />} />
              <Route path="/admin/usuarios" element={<AdminUsers />} />
              <Route path="/admin/estoque" element={<AdminInventory />} />
              <Route path="/admin/configuracoes" element={<AdminSettings />} />

              <Route path="*" element={<NotFound />} />
            </Routes>
          </BrowserRouter>
        </CartProvider>
      </AuthProvider>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
