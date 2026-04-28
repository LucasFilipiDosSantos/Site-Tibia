import { useEffect, useState } from "react";
import { ImageIcon } from "lucide-react";
import { cn } from "@/lib/utils";

type ProductImageProps = {
  src?: string | null;
  alt: string;
  fallbackLabel: string;
  className?: string;
  imgClassName?: string;
};

const hasProductImage = (src?: string | null) => Boolean(src && src !== "/placeholder.svg");
const PLACEHOLDER_IMAGE = "/placeholder.svg";

const ProductImage = ({ src, alt, fallbackLabel, className, imgClassName }: ProductImageProps) => {
  const [hasLoadError, setHasLoadError] = useState(false);
  const shouldRenderProductImage = hasProductImage(src) && !hasLoadError;
  const shouldRenderFallbackImage = hasProductImage(src) && hasLoadError;
  const imageSrc = shouldRenderProductImage ? src : PLACEHOLDER_IMAGE;

  useEffect(() => {
    setHasLoadError(false);
  }, [src]);

  return (
    <div className={cn("flex w-full items-center justify-center overflow-hidden rounded-lg bg-muted", className)}>
      {shouldRenderProductImage || shouldRenderFallbackImage ? (
        <img
          src={imageSrc ?? PLACEHOLDER_IMAGE}
          alt={alt}
          className={cn("h-full w-full object-cover", imgClassName)}
          loading="lazy"
          onError={() => setHasLoadError(true)}
        />
      ) : (
        <div className="flex h-full w-full flex-col items-center justify-center gap-2 px-3 text-center text-muted-foreground">
          <ImageIcon size={20} className="opacity-60" />
          <span className="text-[10px] font-medium uppercase tracking-[0.24em]">{fallbackLabel}</span>
        </div>
      )}
    </div>
  );
};

export { ProductImage, hasProductImage };
