import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import Index from "./pages/Index.tsx";
import Variation1 from "./pages/Variation1.tsx";
import Variation2 from "./pages/Variation2.tsx";
import Variation3 from "./pages/Variation3.tsx";
import Variation4 from "./pages/Variation4.tsx";
import NotFound from "./pages/NotFound.tsx";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Index />} />
          <Route path="/variation-1" element={<Variation1 />} />
          <Route path="/variation-2" element={<Variation2 />} />
          <Route path="/variation-3" element={<Variation3 />} />
          <Route path="/variation-4" element={<Variation4 />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
