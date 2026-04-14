import type { PlaywrightTestConfig } from "@playwright/test";

const config: PlaywrightTestConfig = {
  timeout: 60000,
  use: {
    headless: true,
    actionTimeout: 0,
    ignoreHTTPSErrors: true,
  },
};

export default config;
