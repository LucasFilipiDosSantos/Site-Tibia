import { Star } from "lucide-react";
import { useMemo, useState, type MouseEvent } from "react";

type StarRatingProps = {
  rating?: number | null;
  className?: string;
  iconSize?: number;
  showValue?: boolean;
  showFallbackText?: boolean;
};

type StarRatingInputProps = {
  value: number;
  onChange: (rating: number) => void;
  className?: string;
  iconSize?: number;
  max?: number;
  showValue?: boolean;
  allowHalfSteps?: boolean;
};

const STAR_COUNT = 5;

const clampRating = (rating: number) => Math.min(STAR_COUNT, Math.max(0, rating));

const formatRating = (rating: number) =>
  new Intl.NumberFormat("pt-BR", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 3,
  }).format(rating);

const getRoundedDisplayValue = (rating: number, allowHalfSteps: boolean, max: number) => {
  const step = allowHalfSteps ? 0.5 : 1;
  return Math.min(max, Math.max(0, Math.round(rating / step) * step));
};

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

export function StarRatingInput({
  value,
  onChange,
  className = "",
  iconSize = 22,
  max = STAR_COUNT,
  showValue = true,
  allowHalfSteps = false,
}: StarRatingInputProps) {
  const [hoverValue, setHoverValue] = useState<number | null>(null);
  const safeValue = useMemo(() => Math.min(max, Math.max(0, value)), [max, value]);
  const activeValue = hoverValue ?? safeValue;
  const displayValue = getRoundedDisplayValue(activeValue, allowHalfSteps, max);

  const getPointerValue = (event: MouseEvent<HTMLButtonElement>, index: number) => {
    if (!allowHalfSteps) {
      return index + 1;
    }

    const bounds = event.currentTarget.getBoundingClientRect();
    const offsetX = event.clientX - bounds.left;
    const isLeftHalf = offsetX < bounds.width / 2;
    return index + (isLeftHalf ? 0.5 : 1);
  };

  return (
    <div
      className={`flex items-center gap-2 ${className}`.trim()}
      onMouseLeave={() => setHoverValue(null)}
    >
      <div className="flex items-center gap-1">
        {Array.from({ length: max }).map((_, index) => {
          const fillRatio = Math.min(1, Math.max(0, activeValue - index));
          const fillPercent = `${fillRatio * 100}%`;

          return (
            <button
              key={`rating-input-${index + 1}`}
              type="button"
              onMouseMove={(event) => setHoverValue(getPointerValue(event, index))}
              onFocus={() => setHoverValue(index + 1)}
              onBlur={() => setHoverValue(null)}
              onClick={(event) => onChange(getPointerValue(event, index))}
              className="cursor-pointer rounded-md p-1 transition-transform duration-150 hover:scale-110 focus:outline-none focus:ring-2 focus:ring-primary"
              aria-label={`Avaliar com ${allowHalfSteps ? `${index + 0.5} ou ${index + 1}` : index + 1} estrela${index > 0 ? "s" : ""}`}
              aria-pressed={safeValue >= index + 1}
            >
              <span className="relative block text-[#9CA3AF]">
                <Star size={iconSize} className="fill-current" />
                <span
                  className="absolute inset-y-0 left-0 overflow-hidden text-[#FFD700]"
                  style={{ width: fillPercent }}
                  aria-hidden="true"
                >
                  <Star size={iconSize} className="fill-current" />
                </span>
              </span>
            </button>
          );
        })}
      </div>
      {showValue && (
        <span className="text-sm text-muted-foreground">
          {formatRating(displayValue)}/{max}
        </span>
      )}
    </div>
  );
}
