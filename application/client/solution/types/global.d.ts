import type { ModuleEntrypoint as _ModuleEntrypoint } from "module-public-api/types/ModuleEntrypoint";

declare global {
  // expose a global type alias named `ModuleEntrypoint`
  type ModuleEntrypoint = _ModuleEntrypoint;
}

// ensure this file is treated as a module (so `import type` above is allowed)
export {};
