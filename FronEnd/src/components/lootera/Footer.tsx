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
              O marketplace gamer mais confiável para serviços e produtos de Tibia.
            </p>
          </div>
          <div>
            <h4 className="mb-3 font-display text-sm font-semibold text-foreground">Categorias</h4>
            <ul className="space-y-2 text-xs text-muted-foreground">
              <li><Link to="/produtos?cat=Moedas" className="hover:text-primary transition-colors">Moedas</Link></li>
              <li><Link to="/produtos?cat=Scripts" className="hover:text-primary transition-colors">Scripts</Link></li>
              <li><Link to="/produtos?cat=Macros" className="hover:text-primary transition-colors">Macros</Link></li>
              <li><Link to="/produtos?cat=Personagens" className="hover:text-primary transition-colors">Personagens</Link></li>
              <li><Link to="/produtos?cat=Quests" className="hover:text-primary transition-colors">Quests</Link></li>
            </ul>
          </div>
          <div>
            <h4 className="mb-3 font-display text-sm font-semibold text-foreground">Suporte</h4>
            <ul className="space-y-2 text-xs text-muted-foreground">
              <li><a href="#" className="hover:text-primary transition-colors">Central de Ajuda</a></li>
              <li><a href="#" className="hover:text-primary transition-colors">Política de Reembolso</a></li>
              <li><a href="#" className="hover:text-primary transition-colors">Termos de Uso</a></li>
              <li><a href="#" className="hover:text-primary transition-colors">Privacidade</a></li>
            </ul>
          </div>
          <div>
            <h4 className="mb-3 font-display text-sm font-semibold text-foreground">Contato</h4>
            <ul className="space-y-2 text-xs text-muted-foreground">
              <li>suporte@lootera.com</li>
              <li>Discord: lootera.gg</li>
              <li>WhatsApp: (11) 99999-0000</li>
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
