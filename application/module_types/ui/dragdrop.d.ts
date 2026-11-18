// ============================================================================
// Module-facing type contract for UI drag & drop.
// Lives at: application/module_types/ui/dragdrop.d.ts
// ============================================================================

import { Expression } from "../Expression";
import { ActionReadOnlyExpression } from "../dataExpression/action/no-input-action";
import { StringReadOnlyExpression } from "../primitives/stringExpression";
import { UiTexture, UiFontReference } from "./Ui";

// ---------------------------------------------------------------------------
// 1. Drag type identity — registered handle
// ---------------------------------------------------------------------------

/**
 * A registered drag type reference. Created via `hostApi.ui.dragAndDropChannel`.
 * Carries a stable name for serialization; matched by name-string equality
 * against a drop-off's `accepts` list.
 *
 * Also exposes two ready-made config factories that pass straight to the panel:
 *  - `draggable(payload?, options?)` → a `DraggableOptions` that emits this type.
 *  - `droppable(options?)`           → a `DroppableOptions` that accepts this type.
 *
 * @example
 * const LAND = hostApi.ui.dragAndDropChannel({ name: "land", onDrop, feedback });
 * // drag source:
 * hostApi.ui.window("shop", {
 *   panel: { draggable: LAND.draggable({ entityId: 1, sourceContainerId: "lands" }) },
 * });
 * // drop target:
 * hostApi.ui.window("inventory", { panel: { droppable: LAND.droppable() } });
 */
export type DragTypeReference<P = DraggablePayload> = Expression & {
  /** The registered name (serializable). */
  readonly name: string;
  /**
   * Produce a `draggable` panel option that emits THIS type. Assign the result
   * to `panel.draggable`.
   * @param payload  Per-source identity — the drag payload. Must be the SAME
   *                 shape `P` that this type's `onDrop` reads from `ctx.item`.
   * @param options  Per-source extras (ghost, requireHold, onDragStart,
   *                 onDragEnd, onCancel). `type` is always set to this handle.
   */
  draggable: (
    payload?: P,
    options?: Omit<DraggableOptions<P>, "type" | "payload">
  ) => DraggableOptions<P>;
  /**
   * Produce a `droppable` panel option that accepts THIS type. Assign the
   * result to `panel.droppable`. The type's `onDrop` / `feedback` are the
   * defaults; the options override them per-target. `accepts` is set to
   * `[this type]`.
   * @param options  Per-target overrides (onDrop, feedback).
   */
  droppable: (
    options?: Pick<DroppableOptions<P>, "onDrop" | "feedback">
  ) => DroppableOptions<P>;
};

/**
 * The centered text label drawn over a feedback-state's overlay.
 *
 * `value` is a {@link StringReadOnlyExpression} — build it with
 * `hostApi.runtime.string.of("…")` (and `concat` / `map` / …). `font` is a
 * {@link UiFontReference} from `ui.setFont` (the same font mechanism
 * `ui.text` / `ui.field` use).
 */
export type DragFeedbackText = {
  /** The label text (a non-empty {@link StringReadOnlyExpression}). */
  value: StringReadOnlyExpression;
  /** The font to draw the label with (from `ui.setFont`). */
  font: UiFontReference;
};

/**
 * One feedback state: the {@link UiTexture} (animation + fill) plus the
 * REQUIRED centered {@link DragFeedbackText} label drawn over it.
 */
export type DragFeedbackState = UiTexture & {
  /** REQUIRED centered label drawn over this state's overlay. */
  text: DragFeedbackText;
};

/**
 * The drop-target feedback affordance, in two states:
 *  - `onDrag`    — the ambient "you can drop here" indicator. Shown on EVERY
 *    compatible drop zone while a compatible drag is in flight (pointer not on
 *    it), so while dragging you can see all the available drop zones.
 *  - `onHover`   — the "release to drop" highlight. Shown on the specific zone
 *    the pointer is currently over (overrides that zone's `onDrag`).
 *
 * Each is a {@link DragFeedbackState} (a {@link UiTexture} plus its own
 * centered {@link DragFeedbackText} label). A per-target `droppable.feedback`
 * may override either field.
 *
 * Each state's label is rendered over the overlay (centered) using its own
 * font, and is visible ONLY while that state's feedback is active — i.e. the
 * `onDrag` label in the Available state, the `onHover` label in the Hovered
 * state, which is exactly while a compatible drag is in flight over this target.
 */
export type DragFeedback = {
  /** Ambient "available drop zone" indicator (all compatible zones, while dragging). */
  onDrag: DragFeedbackState;
  /** "You're over me" highlight (the specific zone under the pointer). */
  onHover: DragFeedbackState;
};

/**
 * Arguments for registering a drag type.
 *
 * The drag type is the single home for its drop semantics: the `onDrop`
 * callback and the `feedback` affordance are declared HERE (once, per drag
 * type) rather than on every droppable. Any window whose `droppable.accepts`
 * includes this type inherits the type's `onDrop` / `feedback`, so a whole
 * family of compatible drop targets shares one drop behavior + affordance.
 */
export type DragAndDropChannelArgs<P = DraggablePayload> = {
  /** Stable name for the drag type (used for matching + serialization). */
  name: string;
  /** Optional human-readable description (for debug tools). */
  description?: string;
  /**
   * Callback invoked when an item of THIS drag type is dropped on a compatible
   * target. Receives a `UiDropContext<P>` — `ctx.item` is the payload `P`.
   * Resolved per-target by the host: for each droppable that accepts this type
   * the callback is stashed behind a `__jsHandler` marker and fired through the
   * drop FFI.
   * (A per-target `droppable.onDrop` still overrides this when present.)
   */
  onDrop: (ctx: UiDropContext<P>) => void;
  /**
    * The drop-target feedback affordance for this drag type. `onDrag` is
   * shown on every compatible drop zone while a drag of THIS type is in flight
   * (so you can see all available drop zones); `onHover` is shown on the zone
   * the pointer is over. (A per-target `droppable.feedback` still overrides
   * either field when present.)
   */
  feedback: DragFeedback;
};

/**
 * Factory: registers (or retrieves) a drag type by name. Idempotent.
 * Exposed as `hostApi.ui.dragAndDropChannel`.
 *
 * Generic over the drag payload `P`. The returned `DragTypeReference<P>`
 * constrains `draggable(payload)` to the SAME `P`, so a drag source's payload
 * must match what the type's `onDrop` reads from `ctx.item`. `P` defaults to
 * `DraggablePayload`; pass it explicitly (e.g. `dragAndDropChannel<MyPayload>(...)`)
 * to narrow both the `onDrop` payload and the `draggable` payload.
 */
export type DragAndDropChannelFunction = <P = DraggablePayload>(
  args: DragAndDropChannelArgs<P>
) => DragTypeReference<P>;

// ---------------------------------------------------------------------------
// 2. Drop contexts
// ---------------------------------------------------------------------------

/**
 * The payload that was dropped: the drag payload `P` tagged by the source,
 * plus a reference back to the drag type. `P` defaults to `DraggablePayload`.
 */
export type DroppedItem<P = DraggablePayload> = P & {
  /** The drag type reference that matched `accepts`. */
  dragType: DragTypeReference<P>;
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
export type UiDropContext<P = DraggablePayload> = {
  /** The item that was dropped (the drag payload `P`). */
  item: DroppedItem<P>;
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
export type UiDragContext<P = DraggablePayload> = {
  /** The drag type reference being dragged. */
  dragType: DragTypeReference<P>;
  /** The payload tagged by the source. */
  item: DroppedItem<P>;
  /** Cursor position at drag start. */
  cursor?: { getX: () => number; getY: () => number };
  /** Emit an action at drag start. */
  emitAction: (action: ActionReadOnlyExpression, args?: Record<string, unknown>) => void;
  [key: string]: unknown;
};

/**
 * Extended drag context for `onDragEnd` (knows if the drag was dropped).
 */
export type UiDragEndContext<P = DraggablePayload> = UiDragContext<P> & {
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
export type DraggableOptions<P = DraggablePayload> = {
  /**
   * The drag type reference this source emits. A drop-off accepts a drag if
   * its `accepts` list contains this reference (many-to-many: a target can
   * accept many types; a source emits one).
   */
  type: DragTypeReference<P>;
  /** Payload tagged onto the drag (the type's `P`). */
  payload?: P;
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
  onDragStart?: (ctx: UiDragContext<P>) => void;
  /** Fired when the drag ends (dropped or cancelled). */
  onDragEnd?: (ctx: UiDragEndContext<P>) => void;
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
export type DroppableOptions<P = DraggablePayload> = {
  /**
   * The drag type references this target ACCEPTS. A drag is accepted if the
   * source's `type` is present here. Omitted/empty = accept any drag.
   * (Many-to-many: a target lists the types it accepts.)
   *
   * The accepted type(s) supply the default `onDrop` + `feedback` (declared
   * on `dragAndDropChannel`); this target inherits whichever type's drop semantics
   * the in-flight drag carries.
   */
  accepts?: DragTypeReference<P> | DragTypeReference<P>[];
  /**
   * Optional per-target drop callback that OVERRIDES the accepted drag
   * type's `onDrop`. Receives a `UiDropContext<P>`. Omit to use the drag type's
   * `onDrop` (the common case). (Callback-only — no declarative plan form.)
   */
  onDrop?: (ctx: UiDropContext<P>) => void;
  /**
   * Optional per-target feedback affordance that OVERRIDES the accepted drag
     * type's `feedback` (either the `onDrag` or the `onHover` field, or
    * both). Omitted = inherit the drag type's `feedback`. Each field is a
    * {@link DragFeedbackState} (a {@link UiTexture} animation + fill, plus its
    * own centered {@link DragFeedbackText} label), shown over the drop area
    * displayed like a panel background — the module controls the look exactly
    * like `panel.background`.
    */
   feedback?: DragFeedback;
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
 *     dragAndDropChannel: DragAndDropChannelFunction;  // <-- new
 *   };
 */
export type UiDragDropApiExtension = {
  dragAndDropChannel: DragAndDropChannelFunction;
};
