export const CATEGORY_LABELS: Record<string, string> = {
  coin: "Coin Aurera",
  items: "Itens Aurera",
  characters: "Personagens",
  scripts: "Scripts",
  macros: "Macros",
  services: "Servicos",
};

export const CATEGORY_OPTIONS = Object.entries(CATEGORY_LABELS).map(([slug, label]) => ({
  slug,
  label,
}));

export const getCategoryLabel = (slug: string): string => CATEGORY_LABELS[slug] ?? slug;
