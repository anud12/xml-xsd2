import type { Declarations } from "./types";

function extractNameFromValue(v: unknown): string {
  if (typeof v === "string") return v;
  if (v && typeof v === "object") {
    const obj = v as Record<string, unknown>;
    if (typeof obj.name === "string") return obj.name;
    if (obj.apply && typeof obj.apply === "function" && (obj.apply as Function).name) {
      return (obj.apply as Function).name;
    }
    try {
      return JSON.stringify(obj);
    } catch {
      return String(v);
    }
  }
  return String(v);
}

function extractEntityName(v: unknown): string {
  if (typeof v === "string") return v;
  if (v && typeof v === "object") {
    const obj = v as Record<string, unknown>;
    if (typeof obj.firstName === "string") return obj.firstName;
    try {
      return JSON.stringify(obj);
    } catch {
      return String(v);
    }
  }
  return String(v);
}

export function buildDeclarations(): Declarations {
  const g = globalThis as Record<string, unknown[]> & {
    __registeredEvents?: unknown[];
    __registeredActions?: unknown[];
    __createdEntities?: unknown[];
    __logs?: string[];
    __createdEntitiesFor?: Record<string, string[]>;
    __emitsMap?: Record<string, string[]>;
    __registeredPanels?: string[];
    __entityData?: Record<string, unknown>;
  };

  const out: Declarations = {
    events: (g.__registeredEvents || []).map(extractNameFromValue),
    actions: (g.__registeredActions || []).map(extractNameFromValue),
    functions: [],
    entities: (g.__createdEntities || []).map(extractEntityName),
    creators: (g.__createdEntitiesFor || {}) as Record<string, string[]>,
    emits: (g.__emitsMap || {}) as Record<string, string[]>,
    logs: (g.__logs || []).map(String),
    panels: (g.__registeredPanels || []).map(String),
    entity_data: (g.__entityData || {}) as Record<string, unknown>,
  };

  out.functions = Object.getOwnPropertyNames(g)
    .filter((k) => {
      try {
        return typeof g[k] === "function" && !k.startsWith("_") && k !== "host";
      } catch {
        return false;
      }
    })
    .sort();

  return out;
}

export function serializeDeclarations(): string {
  return JSON.stringify(buildDeclarations());
}
