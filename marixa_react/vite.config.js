import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

import { resolveFrontendEnvironment } from "./config/environment.mjs";

export default defineConfig(({ mode }) => {
  const env = { ...loadEnv(mode, process.cwd(), ""), ...process.env };
  const runtime = resolveFrontendEnvironment(env, { mode, apiPath: "/api", defaultProxy: "http://localhost:57432" });

  return {
    plugins: [react()],
    define: { "import.meta.env.VITE_API_BASE_URL": JSON.stringify(runtime.apiBaseUrl) },
    optimizeDeps: { include: ["exceljs"] },
    server: {
      proxy: {
        "/api": { target: runtime.proxyTarget, changeOrigin: true },
        "/uploads": { target: runtime.proxyTarget, changeOrigin: true },
      },
    },
  };
});
