import * as esbuild from "esbuild";

esbuild.build({
  entryPoints: ["src/index.ts"],
  bundle: true,
  format: "iife",
  target: "es2020",
  outfile: "dist/bridge.js",
  platform: "browser",
  minify: false,
  sourcemap: false,
}).catch(() => process.exit(1));
