const WORLD_OPTIONS = ["Eternia"];

const WorldOptions = () => {
  return (
    <div className="mt-2 inline-flex flex-wrap gap-1" aria-label="Opcoes de mundo">
      {WORLD_OPTIONS.map((world) => (
        <span
          key={world}
          className={world === "Eternia"
            ? "rounded border border-primary/30 bg-primary/10 px-1.5 py-0.5 text-[9px] leading-none text-primary"
            : "rounded border border-border bg-muted/60 px-1.5 py-0.5 text-[9px] leading-none text-muted-foreground"
          }
        >
          {world}
        </span>
      ))}
    </div>
  );
};

export default WorldOptions;
