import { createHostApi } from "./host_api";
import { serializeDeclarations as _serialize } from "./extraction";

const host = createHostApi();

globalThis.Bridge = {
  install() {
    const g = globalThis as Record<string, unknown>;
    g.host = host;
    g.string = host.string;
    g.number = host.number;
    g.createEntity = host.createEntity;
    return host;
  },
  serializeDeclarations() {
    return _serialize();
  },
};

export { host };
export type { Declarations, PendingEffect, CreatedEntity } from "./types";
export type { HostApi } from "./host_api";
