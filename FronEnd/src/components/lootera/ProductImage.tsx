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

const ProductImage = ({ src, alt, fallbackLabel, className, imgClassName }: ProductImageProps) => {
  return (
    <div className={cn("flex items-center justify-center overflow-hidden rounded-lg bg-muted", className)}>
      {hasProductImage(src) ? (
        <img src={src ?? ""} alt={alt} className={cn("h-full w-full object-cover", imgClassName)} loading="lazy" />
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
