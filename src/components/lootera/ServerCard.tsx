interface ServerCardProps {
  serverName: string;
  services?: string[];
  variant?: "commercial" | "fantasy";
}

const ServerCard = ({ serverName, services, variant = "commercial" }: ServerCardProps) => {
  const isFantasy = variant === "fantasy";

  return (
    <div
      className={`flex flex-col rounded-xl p-6 min-h-[220px] lg:min-h-[280px] transition-all
        ${isFantasy
          ? "bg-card border border-brand-gold/15 hover:border-brand-gold/30"
          : "bg-card border border-border hover:border-primary/30"
        }`}
    >
      <div className="mb-4">
        <h3 className={`font-display text-base font-semibold text-center lg:text-lg ${isFantasy ? "text-brand-gold" : "text-foreground"}`}>
          {serverName}
        </h3>
      </div>

      {services && services.length > 0 && (
        <div className="mt-auto">
          <div className="flex flex-wrap gap-2 justify-center">
            {services.map((service) => (
              <span
                key={service}
                className={`rounded-md px-3 py-1.5 text-xs font-medium transition-colors cursor-pointer
                  ${isFantasy
                    ? "bg-brand-gold/15 text-brand-gold hover:bg-brand-gold/25 border border-brand-gold/20"
                    : "bg-primary/15 text-primary hover:bg-primary/25 border border-primary/20"
                  }`}
              >
                {service}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default ServerCard;
