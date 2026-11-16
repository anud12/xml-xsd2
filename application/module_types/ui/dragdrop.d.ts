// ============================================================================
// Module-facing type contract for UI drag & drop.
// Lives at: application/module_types/ui/dragdrop.d.ts
// ============================================================================

import { Expression } from "../Expression";
import { ActionReadOnlyExpression } from "../dataExpression/action/no-input-action";
import { UiTexture } from "./Ui";

// ---------------------------------------------------------------------------
// 1. Drag type identity — registered handle
// ---------------------------------------------------------------------------

/**
 * A registered drag type reference. Created via `hostApi.ui.defineDragType`.
 * Carries a stable name for serialization; matched by name-string equality
 * against a drop-off's `accepts` list.
 *
 * @example
 * const LAND = hostApi.ui.defineDragType({ name: "land" });
 * const STONE = hostApi.ui.defineDragType({ name: "stone" });
 */
export type DragTypeReference = Expression & {
  /** The registered name (serializable). */
  readonly name: string;
};

/**
 * Arguments for registering a drag type.
 */
export type DefineDragTypeArgs = {
  /** Stable name for the drag type (used for matching + serialization). */
  name: string;
  /** Optional human-readable description (for debug tools). */
  description?: string;
};

/**
 * Factory: registers (or retrieves) a drag type by name. Idempotent.
 * Exposed as `hostApi.ui.defineDragType`.
 */
export type DefineDragTypeFunction = (args: DefineDragTypeArgs) => DragTypeReference;

// ---------------------------------------------------------------------------
// 2. Drop contexts
// ---------------------------------------------------------------------------

/**
 * The payload that was dropped.
 */
export type DroppedItem = {
  /** The drag type reference that matched `accepts`. */
  dragType: DragTypeReference;
  /** The dragged entity's id (if the source tagged one). */
  entityId?: string | number;
  /** The container the entity came from (if tagged). */
  sourceContainerId?: string;
  /** Arbitrary extra payload the source tagged. */
  data?: Record<string, unknown>;
};

/**
 * Cursor / hit info at the drop point (mirrors `UiClickContext.cursor`).
 */
export type DropCursor = {
  /** Pixel position within the drop target's local space. */
  getX: () => number;
  getY: () => number;
  /** Grid cell column (grid targets); -1 otherwise. */
  getCellX: () => number;
  /** Grid cell row (grid targets); -1 otherwise. */
  getCellY: () => number;
};

/**
 * Context handed to a drop-off's `onDrop` callback.
 * Parallel in shape to `UiClickContext`.
 */
export type UiDropContext = {
  /** The item that was dropped. */
  item: DroppedItem;
  /** Cursor / hit info at the drop point. */
  cursor?: DropCursor;
  /** Emit an action for this drop. */
  emitAction: (action: ActionReadOnlyExpression, args?: Record<string, unknown>) => void;
  /** The drop-off's own node id. */
  targetId: string;
  [key: string]: unknown;
};

/**
 * Context handed to a drag source's `onDragStart` / `onDragEnd` callbacks.
 */
export type UiDragContext = {
  /** The drag type reference being dragged. */
  dragType: DragTypeReference;
  /** The payload tagged by the source. */
  item: DroppedItem;
  /** Cursor position at drag start. */
  cursor?: { getX: () => number; getY: () => number };
  /** Emit an action at drag start. */
  emitAction: (action: ActionReadOnlyExpression, args?: Record<string, unknown>) => void;
  [key: string]: unknown;
};

/**
 * Extended drag context for `onDragEnd` (knows if the drag was dropped).
 */
export type UiDragEndContext = UiDragContext & {
  /** True if the drag was successfully dropped on a compatible target. */
  dropped: boolean;
  /** The target node id if dropped; undefined if cancelled. */
  targetId?: string;
};

// ---------------------------------------------------------------------------
// 3. Drag source — the `draggable` panel option
// ---------------------------------------------------------------------------

/**
 * Payload the drag source tags onto the drag.
 */
export type DraggablePayload = {
  /** The dragged entity's id (plain id). */
  entityId?: string | number;
  /** The container the entity currently lives in. */
  sourceContainerId?: string;
  /** Arbitrary extra data forwarded to the drop handler. */
  data?: Record<string, unknown>;
};

/**
 * Options that mark a panel as a DRAG SOURCE. Add as `panel.draggable`.
 */
export type DraggableOptions = {
  /**
   * The drag type reference this source emits. A drop-off accepts a drag if
   * its `accepts` list contains this reference (many-to-many: a target can
   * accept many types; a source emits one).
   */
  type: DragTypeReference;
  /** Payload tagged onto the drag. */
  payload?: DraggablePayload;
  /**
   * Texture shown as the in-flight drag "ghost" that follows the pointer.
   * Displayed like a panel background (uses `UiTexture` fill semantics).
   * Optional — without it, a default pointer-sized ghost is shown.
   */
  ghost?: UiTexture;
  /**
   * Require a hold (ms) before drag begins (default false = immediate).
   * Useful on touch so a tap still triggers onClick.
   */
  requireHold?: boolean | number;
  /** Fired when the drag begins. */
  onDragStart?: (ctx: UiDragContext) => void;
  /** Fired when the drag ends (dropped or cancelled). */
  onDragEnd?: (ctx: UiDragEndContext) => void;
  /** Action emitted on cancel (dropped on nothing). Optional. */
  onCancel?: ActionReadOnlyExpression;
  [key: string]: unknown;
};

// ---------------------------------------------------------------------------
// 4. Drop target — the `droppable` panel option
// ---------------------------------------------------------------------------

/**
 * Options that mark a panel as a DROP-OFF (drop target). Add as `panel.droppable`.
 */
export type DroppableOptions = {
  /**
   * The drag type references this target ACCEPTS. A drag is accepted if the
   * source's `type` is present here. Omitted/empty = accept any drag.
   * (Many-to-many: a target lists the types it accepts.)
   */
  accepts?: DragTypeReference | DragTypeReference[];
  /**
   * Callback invoked on a compatible drop. Receives a `UiDropContext`.
   * (Callback-only — no declarative plan form.)
   */
  onDrop: (ctx: UiDropContext) => void;
  /**
   * A single texture shown over the drop area (displayed like a panel
   * background) while a *compatible* drag hovers it. This is the drop-area
   * "outline"/highlight affordance. Omitted = no feedback texture.
   *
   * Uses `UiTexture`'s existing fill semantics, so it can be a nine-slice
   * border frame (`fill: { width: "nine-slice", ... }`) or a full-area
   * overlay (`fill: { width: "stretch", height: "stretch" }`) — the module
   * controls the look, exactly like `panel.background`.
   */
  feedback?: UiTexture;
  [key: string]: unknown;
};

// ---------------------------------------------------------------------------
// 5. Panel options integration
// ---------------------------------------------------------------------------
/**
 * The fields ADDED to `UiPanelOptions.panel` in Ui.d.ts:
 *
 *   export type UiPanelOptions = {
 *     panel: {
 *       ...existing...
 *       draggable?: DraggableOptions;   // <-- new
 *       droppable?: DroppableOptions;   // <-- new
 *     };
 *   };
 *
 * A single panel may be BOTH draggable and droppable (e.g. an inventory cell
 * that holds a draggable item but also accepts new drops).
 */
export type UiDragDropPanelExtensions = {
  draggable?: DraggableOptions;
  droppable?: DroppableOptions;
};

/**
 * The new factory on `hostApi.ui` for registering drag types:
 *
 *   export type UiApi = {
 *     ...existing...
 *     defineDragType: DefineDragTypeFunction;  // <-- new
 *   };
 */
export type UiDragDropApiExtension = {
  defineDragType: DefineDragTypeFunction;
};
