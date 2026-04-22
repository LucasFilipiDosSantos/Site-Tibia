import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import { CartProvider } from "@/contexts/CartContext";
import { AuthProvider } from "@/features/auth/context/AuthContext";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import { AdminRoute } from "@/routes/AdminRoute";

import Home from "./pages/shop/Home";
import Products from "./pages/shop/Products";
import ProductDetail from "./pages/shop/ProductDetail";
import Cart from "./pages/shop/Cart";
import Checkout from "./pages/shop/Checkout";
import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";
import Profile from "./pages/user/Profile";
import OrderHistory from "./pages/user/OrderHistory";
import Dashboard from "./pages/admin/Dashboard";
import AdminProducts from "./pages/admin/AdminProducts";
import AdminOrders from "./pages/admin/AdminOrders";
import AdminUsers from "./pages/admin/AdminUsers";
import AdminInventory from "./pages/admin/AdminInventory";
import AdminSettings from "./pages/admin/AdminSettings";
import NotFound from "./pages/NotFound";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
});

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <AuthProvider>
        <CartProvider>
          <Toaster />
          <Sonner />
          <BrowserRouter basename={import.meta.env.BASE_URL}>
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/produtos" element={<Products />} />
              <Route path="/produto/:slug" element={<ProductDetail />} />
              <Route path="/carrinho" element={<Cart />} />
              <Route
                path="/checkout"
                element={(
                  <ProtectedRoute>
                    <Checkout />
                  </ProtectedRoute>
                )}
              />

              <Route path="/login" element={<Login />} />
              <Route path="/cadastro" element={<Register />} />

              <Route
                path="/perfil"
                element={(
                  <ProtectedRoute>
                    <Profile />
                  </ProtectedRoute>
                )}
              />
              <Route
                path="/pedidos"
                element={(
                  <ProtectedRoute>
                    <OrderHistory />
                  </ProtectedRoute>
                )}
              />

              <Route
                path="/admin"
                element={(
                  <AdminRoute>
                    <Dashboard />
                  </AdminRoute>
                )}
              />
              <Route
                path="/admin/produtos"
                element={(
                  <AdminRoute>
                    <AdminProducts />
                  </AdminRoute>
                )}
              />
              <Route
                path="/admin/pedidos"
                element={(
                  <AdminRoute>
                    <AdminOrders />
                  </AdminRoute>
                )}
              />
              <Route
                path="/admin/usuarios"
                element={(
                  <AdminRoute>
                    <AdminUsers />
                  </AdminRoute>
                )}
              />
              <Route
                path="/admin/estoque"
                element={(
                  <AdminRoute>
                    <AdminInventory />
                  </AdminRoute>
                )}
              />
              <Route
                path="/admin/configuracoes"
                element={(
                  <AdminRoute>
                    <AdminSettings />
                  </AdminRoute>
                )}
              />

              <Route path="*" element={<NotFound />} />
            </Routes>
          </BrowserRouter>
        </CartProvider>
      </AuthProvider>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
