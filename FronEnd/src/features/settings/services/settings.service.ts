import type { Settings } from "../types/settings.types";

const SETTINGS_STORAGE_KEY = "lootera_settings";

const defaultSettings: Settings = {
  siteName: "Lootera",
  taxRate: 0,
  currency: "BRL",
  maintenanceMode: false,
};

const safeParseJSON = <T>(value: string | null, fallback: T): T => {
  if (!value) return fallback;
  try {
    return JSON.parse(value);
  } catch {
    return fallback;
  }
};

export const settingsService = {
  getSettings: (): Settings => {
    const stored = localStorage.getItem(SETTINGS_STORAGE_KEY);
    return safeParseJSON(stored, defaultSettings);
  },

  saveSettings: (settings: Settings): void => {
    localStorage.setItem(SETTINGS_STORAGE_KEY, JSON.stringify(settings));
  },

  updateSettings: (updates: Partial<Settings>): Settings => {
    const current = settingsService.getSettings();
    const updated = { ...current, ...updates };
    settingsService.saveSettings(updated);
    return updated;
  },
};