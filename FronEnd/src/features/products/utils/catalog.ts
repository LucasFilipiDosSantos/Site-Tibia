export const CATEGORY_LABELS: Record<string, string> = {
  coin: "Coin",
  items: "Itens",
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

export const removeAureraFromText = (value: string): string =>
  value
    .replace(/\bAurera\b/g, "")
    .replace(/\s{2,}/g, " ")
    .replace(/\s+([,.])/g, "$1")
    .trim();
