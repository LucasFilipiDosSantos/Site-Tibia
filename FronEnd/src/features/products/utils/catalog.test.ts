import { describe, expect, it } from "vitest";
import { CATEGORY_OPTIONS, getCategoryLabel, removeAureraFromText } from "./catalog";

describe("catalog utils", () => {
  it("maps known category slugs to UI labels", () => {
    expect(getCategoryLabel("coin")).toBe("Coin");
    expect(getCategoryLabel("scripts")).toBe("Scripts");
  });

  it("exposes category options used by navigation", () => {
    expect(CATEGORY_OPTIONS.some((category) => category.slug === "coin")).toBe(true);
    expect(CATEGORY_OPTIONS.some((category) => category.slug === "characters")).toBe(true);
  });

  it("removes only the word Aurera from visible labels", () => {
    expect(removeAureraFromText("Coin Aurera")).toBe("Coin");
    expect(removeAureraFromText("Itens Aurera - Magic Sword")).toBe("Itens - Magic Sword");
  });
});
