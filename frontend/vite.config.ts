import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

// En desarrollo, /api se reenvía a la API .NET: el navegador ve un solo origen,
// así las cookies SameSite=Strict funcionan igual que en producción.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, ".", "");
  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: { "/api": { target: env.VITE_API_PROXY || "http://localhost:5170", changeOrigin: false } },
    },
  };
});
