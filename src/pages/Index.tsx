import { Link } from "react-router-dom";
import logoImg from "@/assets/lootera-logo.png";

const Index = () => {
  return (
    <div className="min-h-screen bg-background flex flex-col items-center justify-center px-4 py-16">
      <img src={logoImg} alt="Lootera" className="h-24 w-auto mb-6 lg:h-32" />
      <p className="text-muted-foreground mb-10 text-center max-w-md">
        Escolha uma das duas variações de layout para a homepage do marketplace.
      </p>

      <div className="grid gap-6 w-full max-w-2xl lg:grid-cols-2">
        <Link
          to="/variation-1"
          className="group flex flex-col items-center rounded-xl border border-border bg-card p-8 text-center transition-all hover:border-primary/50 hover:shadow-lg hover:shadow-primary/10"
        >
          <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-primary/10 text-primary">
            <span className="text-2xl font-bold font-display">1</span>
          </div>
          <h2 className="font-display text-lg font-semibold text-foreground mb-2">Marketplace Gamer Premium</h2>
          <p className="text-sm text-muted-foreground">
            Layout comercial, moderno e confiável. Azul como cor principal, visual limpo.
          </p>
        </Link>

        <Link
          to="/variation-2"
          className="group flex flex-col items-center rounded-xl border border-brand-gold/20 bg-card p-8 text-center transition-all hover:border-brand-gold/50 hover:shadow-lg hover:shadow-brand-gold/10"
        >
          <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-brand-gold/10 text-brand-gold">
            <span className="text-2xl font-bold font-display">2</span>
          </div>
          <h2 className="font-display text-lg font-semibold text-brand-gold mb-2">Fantasy Premium Medieval</h2>
          <p className="text-sm text-muted-foreground">
            Layout autoral com identidade fantasy. Dourado e roxo, visual imersivo.
          </p>
        </Link>
      </div>
    </div>
  );
};

export default Index;
