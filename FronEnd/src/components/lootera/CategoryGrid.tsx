import { Coins, Code, Settings, Crown, BadgeCent, type LucideIcon } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { CATEGORY_OPTIONS } from "@/features/products/utils/catalog";

interface CategoryGridProps {
  variant?: "commercial" | "fantasy";
}

const iconByCategory: Record<string, LucideIcon> = {
  coin: Coins,
  items: Crown,
  characters: Crown,
  scripts: Code,
  macros: Settings,
  services: BadgeCent,
};

const commercialCards = [
  { slug: "coin", label: "Coin Aurera", icon: Coins },
  { slug: "items", label: "Itens Aurera", icon: Crown },
  { slug: "characters", label: "Personagem", icon: Crown },
  { slug: "services", label: "Servicos", icon: Settings },
  { slug: "scripts", label: "Scripts 100% AFK OTC", icon: Code },
  { slug: "macros", label: "Macros Free", icon: Settings },
];

const CategoryGrid = ({ variant = "commercial" }: CategoryGridProps) => {
  const isFantasy = variant === "fantasy";
  const navigate = useNavigate();
  const cards = isFantasy ? CATEGORY_OPTIONS.map((category) => ({
    ...category,
    icon: iconByCategory[category.slug] ?? BadgeCent,
  })) : commercialCards;

  return (
    <section className="py-8 lg:py-12">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-center gap-6 overflow-x-auto pb-2 lg:gap-10">
          {cards.map((category) => {
            const Icon = category.icon;

            return (
              <button
                key={`${category.slug}-${category.label}`}
                onClick={() => navigate(`/produtos?category=${encodeURIComponent(category.slug)}`)}
                className="group flex flex-col items-center gap-2 transition-all"
              >
                <div
                  className={`flex h-16 w-16 items-center justify-center rounded-full border transition-all lg:h-20 lg:w-20 ${
                    isFantasy
                      ? "border-brand-gold/20 bg-brand-purple/60 text-brand-gold group-hover:border-brand-gold/50 group-hover:bg-brand-purple/80"
                      : "border-border bg-muted text-muted-foreground group-hover:border-primary/40 group-hover:text-primary"
                  }`}
                >
                  <Icon size={28} />
                </div>
                <span className={`text-xs font-medium lg:text-sm ${isFantasy ? "text-brand-gold/80" : "text-muted-foreground"}`}>
                  {category.label}
                </span>
              </button>
            );
          })}
        </div>
      </div>
    </section>
  );
};

export default CategoryGrid;
