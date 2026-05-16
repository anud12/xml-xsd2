import type { PendingEffect, CreatedEntity } from "./types";

type EventHandler = { name?: string; apply?: (...args: unknown[]) => void } | string;
type ActionHandler = { name?: string; apply?: (...args: unknown[]) => void } | string;
type PanelDef = Record<string, unknown> | string;

export type HostApi = {
  emitEvent(name: string | { name?: string }): void;
  registerEvent(ev: EventHandler): void;
  registerAction(ev: ActionHandler): void;
  registerEffect(ev: EventHandler): void;
  registerPanel(p: PanelDef): void;
  createEntity(obj: CreatedEntity | string): void;
  setEntity(id: string, data: Record<string, unknown>): void;
  log(msg: string): void;
  number: { of: (n: number) => number };
  string: { of: (s: string) => string };
  texture: { of: (t: unknown) => unknown };
};

type GlobalState = {
  __pendingEffects?: PendingEffect[];
  __registeredEvents?: unknown[];
  __registeredActions?: unknown[];
  __registeredEffects?: unknown[];
  __registeredPanels?: string[];
  __createdEntities?: unknown[];
  __entityData?: Record<string, unknown>;
  __logs?: string[];
  __createdEntitiesFor?: Record<string, string[]>;
  __emitsMap?: Record<string, string[]>;
  __entityStore?: unknown[];
};

const gs = globalThis as GlobalState;

function extractName(ev: EventHandler): string {
  if (typeof ev === "string") return ev;
  if (ev.name) return ev.name;
  if (ev.apply && typeof ev.apply === "function" && ev.apply.name) return ev.apply.name;
  return "unknown";
}

function safeStringify(val: unknown): string {
  try {
    return JSON.stringify(val);
  } catch {
    return String(val);
  }
}

export function createHostApi(): HostApi {
  return {
    emitEvent(name) {
      gs.__pendingEffects = gs.__pendingEffects || [];
      const eventName =
        name && typeof name === "object" && typeof name.name === "string"
          ? name.name
          : String(name);
      gs.__pendingEffects.push({ name: eventName, payload: {} });
      gs.__logs = gs.__logs || [];
      gs.__logs.push("DEBUG: emitEvent called");
      gs.__logs.push(`event: ${eventName}`);
    },

    registerEvent(ev) {
      const n = extractName(ev);
      gs.__registeredEvents = gs.__registeredEvents || [];
      gs.__registeredEvents.push(ev);
      gs.__logs = gs.__logs || [];
      gs.__logs.push(`Events registered: ${n}`);
    },

    registerAction(ev) {
      const n = extractName(ev);
      gs.__registeredActions = gs.__registeredActions || [];
      gs.__registeredActions.push(ev);
      gs.__logs = gs.__logs || [];
      gs.__logs.push(`Actions registered: ${n}`);
    },

    registerEffect(ev) {
      const n = extractName(ev);
      gs.__registeredEvents = gs.__registeredEvents || [];
      gs.__registeredEvents.push(ev);
      gs.__logs = gs.__logs || [];
      gs.__logs.push(`Effects registered: ${n}`);
    },

    registerPanel(p) {
      let toPush: string;
      if (p && typeof p === "object") {
        toPush = JSON.stringify(p);
      } else if (typeof p === "string") {
        toPush = JSON.stringify({ id: p });
      } else {
        toPush = JSON.stringify({ id: String(p) });
      }
      gs.__registeredPanels = gs.__registeredPanels || [];
      gs.__registeredPanels.push(toPush);
    },

    createEntity(obj) {
      gs.__createdEntities = gs.__createdEntities || [];
      if (obj && typeof obj === "object" && typeof obj.firstName === "string") {
        gs.__createdEntities.push({ firstName: obj.firstName });
        gs.__logs = gs.__logs || [];
        gs.__logs.push(`entity created: ${obj.firstName}`);
      } else if (typeof obj === "string") {
        gs.__createdEntities.push({ firstName: obj });
        gs.__logs = gs.__logs || [];
        gs.__logs.push(`entity created: ${obj}`);
      } else {
        gs.__createdEntities.push(obj);
        gs.__logs = gs.__logs || [];
        gs.__logs.push(`entity created: ${safeStringify(obj)}`);
      }
    },

    setEntity(id, data) {
      gs.__entityData = gs.__entityData || {};
      gs.__entityData[id] = data;
    },

    log(msg) {
      gs.__logs = gs.__logs || [];
      gs.__logs.push(String(msg));
    },

    number: { of: (n) => n },
    string: { of: (s) => s },
    texture: { of: (t) => t },
  };
}
