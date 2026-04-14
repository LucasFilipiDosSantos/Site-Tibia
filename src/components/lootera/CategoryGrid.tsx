import { Coins, Code, Settings, Crown, AlertCircle } from "lucide-react";
import { useNavigate } from "react-router-dom";

interface CategoryGridProps {
  variant?: "commercial" | "fantasy";
}

const categories = [
  { name: "Moedas", icon: Coins },
  { name: "Scripts", icon: Code },
  { name: "Macros", icon: Settings },
  { name: "Personagens", icon: Crown },
  { name: "Quests", icon: AlertCircle },
];

const CategoryGrid = ({ variant = "commercial" }: CategoryGridProps) => {
  const isFantasy = variant === "fantasy";

  return (
    <section className="py-8 lg:py-12">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-center gap-6 overflow-x-auto pb-2 lg:gap-10">
          {categories.map((cat) => (
            <button
              key={cat.name}
              className="group flex flex-col items-center gap-2 transition-all"
            >
              <div
                className={`flex h-16 w-16 items-center justify-center rounded-full transition-all lg:h-20 lg:w-20
                  ${isFantasy
                    ? "bg-brand-purple/60 border border-brand-gold/20 text-brand-gold group-hover:border-brand-gold/50 group-hover:bg-brand-purple/80"
                    : "bg-muted border border-border text-muted-foreground group-hover:border-primary/40 group-hover:text-primary"
                  }`}
              >
                <cat.icon size={28} />
              </div>
              <span className={`text-xs font-medium lg:text-sm ${isFantasy ? "text-brand-gold/80" : "text-muted-foreground"}`}>
                {cat.name}
              </span>
            </button>
          ))}
        </div>
      </div>
    </section>
  );
};

export default CategoryGrid;
