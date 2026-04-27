import { ShoppingCart } from "lucide-react";
import { ProductImage } from "@/components/lootera/ProductImage";
import { StarRating } from "@/components/lootera/StarRating";

interface ProductCardProps {
  title: string;
  server?: string;
  price: string;
  originalPrice?: string;
  rating: number;
  reviewCount?: number;
  image?: string;
  tag?: string;
  variant?: "commercial" | "fantasy";
}

const ProductCard = ({ title, server, price, originalPrice, rating, reviewCount = 0, image, tag, variant = "commercial" }: ProductCardProps) => {
  const isFantasy = variant === "fantasy";

  return (
    <div className={`font-body group flex flex-col items-start overflow-hidden rounded-xl text-left transition-all
      ${isFantasy
        ? "bg-card border border-brand-gold/10 hover:border-brand-gold/30 shadow-lg shadow-brand-purple/5"
        : "bg-card border border-border hover:border-primary/30 shadow-md shadow-black/20"
      }`}
    >
      <div className={`relative h-36 lg:h-44 ${isFantasy ? "bg-brand-purple/30" : "bg-secondary"}`}>
        <ProductImage src={image} alt={title} fallbackLabel="Produto" className="h-full rounded-none" />
        {tag && (
          <span className={`absolute left-3 top-3 rounded-md px-2 py-0.5 text-xs font-semibold
            ${isFantasy ? "bg-brand-gold/90 text-background" : "bg-primary text-primary-foreground"}`}>
            {tag}
          </span>
        )}
      </div>

      <div className="flex flex-1 flex-col items-start p-4">
        {server && (
          <span className="mb-2 rounded border border-border bg-muted/40 px-1.5 py-0.5 text-[10px] leading-none text-foreground">
            {server}
          </span>
        )}
        <h3 className="font-body mb-2 line-clamp-2 text-sm text-foreground">{title}</h3>
        <StarRating rating={reviewCount > 0 ? rating : null} className="mb-3" />
      <div className="mt-auto flex w-full items-end justify-between">
          <div>
            {originalPrice && <span className="text-xs text-muted-foreground line-through">{originalPrice}</span>}
            <p className={`text-lg font-semibold ${isFantasy ? "text-brand-gold" : "text-primary"}`}>{price}</p>
          </div>
          <button className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-gold text-background transition-colors hover:bg-brand-gold/90">
            <ShoppingCart size={16} />
          </button>
        </div>
      </div>
    </div>
  );
};

export default ProductCard;
