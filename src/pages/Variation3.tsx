import Header from "@/components/lootera/Header";
import CategoryGrid from "@/components/lootera/CategoryGrid";
import ServerCard from "@/components/lootera/ServerCard";
import ServiceCard from "@/components/lootera/ServiceCard";
import FloatingSupport from "@/components/lootera/FloatingSupport";
import heroBanner from "@/assets/hero-banner-v3.png";

const Variation3 = () => {
  return (
    <div className="min-h-screen bg-background">
      {/* Header — V2 fantasy style */}
      <Header variant="fantasy" />

      {/* Hero Banner — V1 structure (contained, rounded) + V2 overlay style + new image */}
      <section className="relative">
        <div className="container mx-auto px-4 pt-4">
          <div className="relative h-[240px] overflow-hidden rounded-xl lg:h-[380px]">
            <img
              src={heroBanner}
              alt="Lootera Marketplace - Tibia"
              className="absolute inset-0 h-full w-full object-cover"
              width={1920}
              height={800}
            />
            <div className="absolute inset-0 bg-gradient-to-t from-background via-background/30 to-brand-purple/20" />
            <div className="absolute bottom-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-brand-gold/40 to-transparent" />
          </div>
        </div>
      </section>

      {/* Categories — V2 fantasy style */}
      <CategoryGrid variant="fantasy" />

      {/* Server & Service Cards — V1 layout (3-col grid) + V2 fantasy style */}
      <section className="pb-10 lg:pb-16">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3 lg:gap-6">
            <ServerCard serverName="NOME DO MUNDO SERVER" variant="fantasy" />
            <ServerCard
              serverName="NOME DO MUNDO SERVER"
              services={["serviço", "serviço", "serviço", "serviço"]}
              variant="fantasy"
            />
            <ServiceCard title="SERVIÇO" variant="fantasy" />
          </div>
        </div>
      </section>

      {/* More cards row */}
      <section className="pb-10 lg:pb-16">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3 lg:gap-6">
            <ServerCard
              serverName="ANTICA"
              services={["Gold Coins", "Power Level", "Quest Service"]}
              variant="fantasy"
            />
            <ServerCard
              serverName="SECURA"
              services={["Gold Coins", "Chars", "Scripts"]}
              variant="fantasy"
            />
            <ServiceCard title="SCRIPTS & MACROS" variant="fantasy" />
          </div>
        </div>
      </section>

      {/* Trust bar — V1 layout + V2 fantasy colors */}
      <section className="border-t border-brand-gold/10 bg-brand-purple/30 py-10">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-6 text-center lg:grid-cols-4">
            {[
              { num: "10k+", label: "Transações" },
              { num: "99%", label: "Satisfação" },
              { num: "24/7", label: "Suporte" },
              { num: "5 min", label: "Entrega média" },
            ].map((s) => (
              <div key={s.label}>
                <p className="text-2xl font-bold text-brand-gold lg:text-3xl">{s.num}</p>
                <p className="mt-1 text-sm text-muted-foreground">{s.label}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <FloatingSupport />
    </div>
  );
};

export default Variation3;
