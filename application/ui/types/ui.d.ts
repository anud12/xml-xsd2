// Canonical .ui type contracts (Phase 0 spine).
// Supersedes application/suite/types/ui (retirement in TASK-2026-08-24-013).

export interface UiDivisionOptions {
  [option: string]: unknown;
}

export interface UiNode {
  id: string;
  kind: 'division' | 'text';
  children: string[];
  options?: UiDivisionOptions;
  value?: string;
}

export interface UiContainerEntity {
  id: string;
  index: number;
}

export type UiTexture = { animation: unknown; fill: { width: 'stretch' | 'fit' | 'tile'; height: 'stretch' | 'fit' | 'tile'; keepAspectRatio?: boolean }; nineSlice?: unknown };

export interface UiFactories {
  /** The only layout node. Returns the node id. */
  div(id: string, options: UiDivisionOptions | undefined, children: string[]): string;
  /** A leaf rendering a string value. Returns the node id. */
  text(id: string, value: string): string;
  /**
   * A layout node expanded at runtime to one item per entity of the named
   * container. The render lambda declares what to render per entity and
   * returns the node id(s) to place as that entity's children. Returns the
   * list node id.
   */
  groupView(name: string, args: { group: string; list: string; panel: { width: number; height: number }; /** Texture for empty cells; omitted cells remain transparent. */ emptyCell?: UiTexture }, render: (member: UiContainerEntity) => string | null): string;
  entityList(name: string, args: { group: string; list: string }, render: (entity: UiContainerEntity) => string | string[]): string;
}

export interface UiTransport {
  registerNode(node: UiNode): void;
  emitDelta(delta: UiDelta): void;
  readClientState(): UiClientState;
  resolveResource(name: string): string;
}

export interface UiClientState {
  clientId: string;
  actor: string | null;
  values: Record<string, unknown>;
}

export interface UiDelta {
  ops: UiDeltaOp[];
}

export type UiDeltaOp =
  | { op: 'add'; node: UiNode }
  | { op: 'update'; node: UiNode }
  | { op: 'remove'; id: string };
