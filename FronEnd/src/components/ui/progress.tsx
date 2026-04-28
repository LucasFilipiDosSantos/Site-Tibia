import * as React from "react";

import { cn } from "@/lib/utils";

const Progress = React.forwardRef<
  HTMLProgressElement,
  React.ComponentPropsWithoutRef<"progress">
>(({ className, value, ...props }, ref) => (
  <progress
    ref={ref}
    className={cn("h-4 w-full overflow-hidden rounded-full accent-primary", className)}
    max={100}
    value={value ?? 0}
    {...props}
  />
));
Progress.displayName = "Progress";

export { Progress };
