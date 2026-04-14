import { Star, ShoppingCart, Sword } from "lucide-react";

interface ProductCardProps {
  title: string;
  server?: string;
  price: string;
  originalPrice?: string;
  rating: number;
  image?: string;
  tag?: string;
  variant?: "commercial" | "fantasy";
}

const ProductCard = ({ title, server, price, originalPrice, rating, tag, variant = "commercial" }: ProductCardProps) => {
  const isFantasy = variant === "fantasy";

  return (
    <div className={`group flex flex-col rounded-xl transition-all overflow-hidden
      ${isFantasy
        ? "bg-card border border-brand-gold/10 hover:border-brand-gold/30 shadow-lg shadow-brand-purple/5"
        : "bg-card border border-border hover:border-primary/30 shadow-md shadow-black/20"
      }`}
    >
      <div className={`relative h-36 lg:h-44 ${isFantasy ? "bg-brand-purple/30" : "bg-secondary"}`}>
        <div className="flex h-full items-center justify-center text-muted-foreground">
          <Sword size={40} className="opacity-20" />
        </div>
        {tag && (
          <span className={`absolute left-3 top-3 rounded-md px-2 py-0.5 text-xs font-semibold
            ${isFantasy ? "bg-brand-gold/90 text-background" : "bg-primary text-primary-foreground"}`}>
            {tag}
          </span>
        )}
      </div>

      <div className="flex flex-1 flex-col p-4">
        {server && <span className="mb-1 text-xs text-muted-foreground">{server}</span>}
        <h3 className="mb-2 text-sm font-semibold text-foreground line-clamp-2">{title}</h3>
        <div className="mb-3 flex items-center gap-1">
          {Array.from({ length: 5 }).map((_, i) => (
            <Star key={i} size={12} className={i < rating ? "fill-brand-gold text-brand-gold" : "text-muted-foreground/30"} />
          ))}
          <span className="ml-1 text-xs text-muted-foreground">{rating}.0</span>
        </div>
        <div className="mt-auto flex items-end justify-between">
          <div>
            {originalPrice && <span className="text-xs text-muted-foreground line-through">{originalPrice}</span>}
            <p className={`text-lg font-bold ${isFantasy ? "text-brand-gold" : "text-primary"}`}>{price}</p>
          </div>
          <button className={`flex h-9 w-9 items-center justify-center rounded-lg transition-colors
            ${isFantasy ? "bg-brand-gold/20 text-brand-gold hover:bg-brand-gold/30" : "bg-primary/10 text-primary hover:bg-primary/20"}`}>
            <ShoppingCart size={16} />
          </button>
        </div>
      </div>
    </div>
  );
};

export default ProductCard;
