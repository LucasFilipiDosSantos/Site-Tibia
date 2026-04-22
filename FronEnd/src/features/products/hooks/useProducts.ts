import { useQuery } from "@tanstack/react-query";
import { productService, type ProductListFilters } from "../services/product.service";

export const useProducts = (filters: ProductListFilters = {}) =>
  useQuery({
    queryKey: ["products", filters],
    queryFn: () => productService.getProducts(filters),
  });

export const useProduct = (slug: string) =>
  useQuery({
    queryKey: ["product", slug],
    queryFn: () => productService.getProductBySlug(slug),
    enabled: Boolean(slug),
  });
