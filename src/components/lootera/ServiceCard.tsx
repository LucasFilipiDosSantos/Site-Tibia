interface ServiceCardProps {
  title: string;
  variant?: "commercial" | "fantasy";
}

const ServiceCard = ({ title, variant = "commercial" }: ServiceCardProps) => {
  const isFantasy = variant === "fantasy";

  return (
    <div
      className={`flex flex-col items-center justify-center rounded-xl p-6 min-h-[220px] lg:min-h-[280px] transition-all
        ${isFantasy
          ? "bg-card border border-brand-gold/15 hover:border-brand-gold/30"
          : "bg-card border border-border hover:border-primary/30"
        }`}
    >
      <h3 className={`font-display text-base font-semibold text-center lg:text-lg ${isFantasy ? "text-brand-gold" : "text-foreground"}`}>
        {title}
      </h3>
    </div>
  );
};

export default ServiceCard;
