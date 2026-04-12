import { defineConfig } from "vite";

export default defineConfig({
  server: {
    host: true,
    port: 4200,
    hmr: {
      host: "monitex.local",
      protocol: "ws",
      port: 80
    }
  }
});
