const WORLD_OPTIONS = ["Eternia", "Em breve"];

const WorldOptions = () => {
  return (
    <div className="mt-2 flex flex-wrap gap-1.5" aria-label="Opcoes de mundo">
      {WORLD_OPTIONS.map((world) => (
        <span
          key={world}
          className={world === "Eternia"
            ? "rounded-md border border-primary/30 bg-primary/10 px-2 py-1 text-[10px] font-medium text-primary"
            : "rounded-md border border-border bg-muted/60 px-2 py-1 text-[10px] font-medium text-muted-foreground"
          }
        >
          Mundo: {world}
        </span>
      ))}
    </div>
  );
};

export default WorldOptions;
