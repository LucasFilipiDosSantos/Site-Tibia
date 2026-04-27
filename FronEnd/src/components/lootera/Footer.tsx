import { Link } from "react-router-dom";
import logoImg from "@/assets/lootera-logo.png";

const Footer = () => {
  return (
    <footer className="border-t border-border bg-secondary">
      <div className="container mx-auto px-4 py-10">
        <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
          <div>
            <img src={logoImg} alt="Lootera" className="mb-4 h-8 w-auto" />
            <p className="text-xs text-muted-foreground">
              O marketplace gamer mais confiavel para servicos e produtos de Tibia.
            </p>
          </div>
          <div>
            <h4 className="mb-3 font-display text-sm font-semibold text-foreground">Categorias</h4>
            <ul className="space-y-2 text-xs text-muted-foreground">
              <li><Link to="/produtos?category=coin" className="transition-colors hover:text-primary">Coin</Link></li>
              <li><Link to="/produtos?category=items" className="transition-colors hover:text-primary">Itens</Link></li>
              <li><Link to="/produtos?category=scripts" className="transition-colors hover:text-primary">Scripts 100% AFK OTC</Link></li>
              <li><Link to="/produtos?category=macros" className="transition-colors hover:text-primary">Macros</Link></li>
              <li><Link to="/produtos?category=characters" className="transition-colors hover:text-primary">Personagens</Link></li>
              <li><Link to="/produtos?category=services" className="transition-colors hover:text-primary">Servicos</Link></li>
            </ul>
          </div>
          <div>
            <h4 className="mb-3 font-display text-sm font-semibold text-foreground">Suporte</h4>
            <ul className="space-y-2 text-xs text-muted-foreground">
              <li><a href="#" className="transition-colors hover:text-primary">Central de Ajuda</a></li>
              <li><a href="#" className="transition-colors hover:text-primary">Politica de Reembolso</a></li>
              <li><a href="#" className="transition-colors hover:text-primary">Termos de Uso</a></li>
              <li><a href="#" className="transition-colors hover:text-primary">Privacidade</a></li>
            </ul>
          </div>
          <div>
            <h4 className="mb-3 font-display text-sm font-semibold text-foreground">Contato</h4>
            <ul className="space-y-2 text-xs text-muted-foreground">
              <li>
                <a href="mailto:looteratibia@gmail.com" className="transition-colors hover:text-primary">
                  looteratibia@gmail.com
                </a>
              </li>
              <li>Discord: lootera.gg</li>
              <li>
                <a href="tel:+5582999749180" className="transition-colors hover:text-primary">
                  WhatsApp: 82 99974-9180
                </a>
              </li>
            </ul>
          </div>
        </div>
        <div className="mt-8 border-t border-border pt-6 text-center text-xs text-muted-foreground">
          © 2026 Lootera. Todos os direitos reservados.
        </div>
      </div>
    </footer>
  );
};

export default Footer;
