import Header from "@/components/lootera/Header";
import CategoryGrid from "@/components/lootera/CategoryGrid";
import ServerCard from "@/components/lootera/ServerCard";
import ServiceCard from "@/components/lootera/ServiceCard";
import FloatingSupport from "@/components/lootera/FloatingSupport";
import heroBanner from "@/assets/hero-banner-v1.jpg";

const Variation1 = () => {
  return (
    <div className="min-h-screen bg-background">
      <Header variant="commercial" />

      {/* Hero Banner - Full width, no text overlay, clean commercial look */}
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
            <div className="absolute inset-0 bg-gradient-to-t from-background/40 to-transparent" />
          </div>
        </div>
      </section>

      {/* Categories - Circular icons */}
      <CategoryGrid variant="commercial" />

      {/* Server & Service Cards */}
      <section className="pb-10 lg:pb-16">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3 lg:gap-6">
            <ServerCard
              serverName="NOME DO MUNDO SERVER"
              variant="commercial"
            />
            <ServerCard
              serverName="NOME DO MUNDO SERVER"
              services={["serviço", "serviço", "serviço", "serviço"]}
              variant="commercial"
            />
            <ServiceCard title="SERVIÇO" variant="commercial" />
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
              variant="commercial"
            />
            <ServerCard
              serverName="SECURA"
              services={["Gold Coins", "Chars", "Scripts"]}
              variant="commercial"
            />
            <ServiceCard title="SCRIPTS & MACROS" variant="commercial" />
          </div>
        </div>
      </section>

      {/* Trust bar */}
      <section className="border-t border-border bg-secondary py-10">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-6 text-center lg:grid-cols-4">
            {[
              { num: "10k+", label: "Transações" },
              { num: "99%", label: "Satisfação" },
              { num: "24/7", label: "Suporte" },
              { num: "5 min", label: "Entrega média" },
            ].map((s) => (
              <div key={s.label}>
                <p className="text-2xl font-bold text-primary lg:text-3xl">{s.num}</p>
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

export default Variation1;
