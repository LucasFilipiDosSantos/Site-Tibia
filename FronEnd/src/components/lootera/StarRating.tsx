import { Star } from "lucide-react";

type StarRatingProps = {
  rating?: number | null;
  className?: string;
  iconSize?: number;
  showValue?: boolean;
  showFallbackText?: boolean;
};

const STAR_COUNT = 5;

const clampRating = (rating: number) => Math.min(STAR_COUNT, Math.max(0, rating));

const formatRating = (rating: number) =>
  new Intl.NumberFormat("pt-BR", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 3,
  }).format(rating);

export function StarRating({
  rating,
  className = "",
  iconSize = 12,
  showValue = false,
  showFallbackText = false,
}: StarRatingProps) {
  const hasRating = typeof rating === "number" && Number.isFinite(rating);
  const safeRating = hasRating ? clampRating(rating) : 0;
  const percent = (safeRating / STAR_COUNT) * 100;
  const label = hasRating ? `${formatRating(safeRating)} de 5 estrelas` : "Sem avaliacao";

  return (
    <div className={`flex items-center gap-2 ${className}`.trim()} aria-label={label}>
      <div className="relative inline-flex">
        <div className="flex items-center gap-0.5 text-brand-gold/25">
          {Array.from({ length: STAR_COUNT }).map((_, index) => (
            <Star key={`empty-${index}`} size={iconSize} className="fill-current" />
          ))}
        </div>
        <div className="absolute inset-y-0 left-0 overflow-hidden" style={{ width: `${percent}%` }}>
          <div className="flex items-center gap-0.5 text-brand-gold">
            {Array.from({ length: STAR_COUNT }).map((_, index) => (
              <Star key={`filled-${index}`} size={iconSize} className="fill-current" />
            ))}
          </div>
        </div>
      </div>
      {hasRating && showValue && <span className="text-xs text-muted-foreground">{formatRating(safeRating)}</span>}
      {!hasRating && showFallbackText && <span className="text-xs text-muted-foreground">Sem avaliacao</span>}
    </div>
  );
}
