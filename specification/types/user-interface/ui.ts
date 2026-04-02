import type { NumberExpression } from '../primitives/numberExpression';
import type { StringExpression } from '../primitives/stringExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';
import type { UiStateApi, UiDataApi } from './ui-state';

// ── Rendering primitives ──────────────────────────────────────────────────────

/**
 * Texture stretching mode controlling how a texture fills a component.
 *
 * - `"fill"`: stretch/squash texture to exactly fit component bounds (ignores aspect ratio)
 * - `"fit"`: scale texture to fit within bounds while preserving aspect ratio
 * - `"tile"`: repeat texture across component bounds
 * - `"center"`: place texture at center without scaling (clip if texture exceeds bounds)
 *
 * @see rendering.md — Texture stretching
 */
export type TextureStretch = 'fill' | 'fit' | 'tile' | 'center';

/**
 * Named reference to a texture asset resolved by client at render time.
 * Missing textures fall back to platform default; a warning is logged.
 *
 * @see rendering.md — Resource resolution
 */
export type TextureResource = {
  /** Texture name. Can be a StringExpression for dynamic resolution. */
  name: StringExpression;

  /**
   * How the texture fills the component.
   * Default: `"fill"` (stretch to fit).
   */
  stretch?: TextureStretch;
};

/**
 * Named reference to a font asset resolved by client at render time.
 * Missing fonts fall back to platform default; a warning is logged.
 *
 * Font size, color, weight, and style are determined by the font asset itself
 * (not configurable per-component). The asset name must resolve to a complete font spec.
 *
 * @see rendering.md — Resource resolution
 */
export type FontResource = {
  /** Font name (e.g., "body-font", "title-font"). Can be a StringExpression for dynamic resolution. */
  name: StringExpression;
};

// ── Layout primitives ─────────────────────────────────────────────────────────

/**
 * Minimum / maximum / proportional size hint shared by all component options
 * and column track definitions.
 *
 * @see box.md — Size constraint
 */
export type SizeConstraint = {
  /** Minimum size in logical units. The component is never sized below this. */
  min?: NumberExpression;

  /** Maximum size in logical units. The component is never sized above this. */
  max?: NumberExpression;

  /**
   * Weight for claiming remaining space after min sizes are satisfied.
   * Behaves like CSS `flex-grow` / `fr` units.
   * A component with `scale: 2` claims twice as much remaining space as one with `scale: 1`.
   * Defaults to 0 (sized to min, or to content when no min is set).
   */
  scale?: NumberExpression;
};

/**
 * Per-axis size hints passed to any component.
 *
 * `width` is consumed by the parent's column track allocation.
 * `height` is consumed by the parent's row sizing.
 * `anchor` positions the component within its cell and controls growth direction.
 *
 * @see box.md — Size constraint; Anchor positioning
 */
export type ChildSize = {
  width?: SizeConstraint;
  height?: SizeConstraint;

  /**
   * Cell-local anchor point controlling component positioning and growth direction.
   * Values 0–1 normalized per axis.
   *
   * - `0` (start/top): anchors to edge; content grows away (rightward/downward)
   * - `0.5` (center): anchors to center; content grows symmetrically
   * - `1` (end/bottom): anchors to edge; content grows away (leftward/upward)
   *
   * Per-axis independent. Omitted axes default to `0.5` (center).
   *
   * Values outside [0, 1] are clamped. Growth direction biases natural content overflow
   * (text wrapping, intrinsic sizing). When content exceeds min/max size constraints,
   * it clamps to limits and anchor repositions to maintain growth intent.
   *
   * @see panel.md — Panel anchor is different (screen-space positioning)
   * @see box.md — Anchor positioning and growth direction semantics
   */
  anchor?: {
    /** Horizontal anchor (0=left, 0.5=center, 1=right). Default: 0.5. */
    x?: NumberExpression;
    /** Vertical anchor (0=top, 0.5=center, 1=bottom). Default: 0.5. */
    y?: NumberExpression;
  };
};

/**
 * Column track definition. Extends `SizeConstraint` with content alignment.
 *
 * @see box.md — Track definition
 */
export type TrackDefinition = SizeConstraint & {
  /**
   * Alignment of content within this track.
   * - `"start"` (default): aligns to the track start (left for columns, top for rows).
   * - `"end"`: aligns to the track end (right for columns, bottom for rows).
   */
  align?: 'start' | 'end';
};

/**
 * Grid layout configuration for a Box.
 *
 * All layout is expressed as a grid. Flex-row and flex-column are degenerate
 * cases — see box.md for examples.
 *
 * @see box.md — Layout
 */
export type GridLayout = {
  /**
   * Column track definitions. The number of entries determines the number of
   * columns. All children in the same column share the same track width,
   * synchronizing column widths across rows.
   */
  columns: TrackDefinition[];

  /**
   * Auto-flow direction.
   * - `true` (default): row-first — children fill left→right, then wrap to the next row.
   * - `false`: column-first — children fill top→bottom, then move to the next column.
   */
  rowFirst?: boolean;

  /**
   * When `true`, children are placed last-to-first rather than first-to-last.
   * Default: `false`.
   */
  reverse?: boolean;

  /** Space between cells in logical units. */
  gap?: {
    row?: NumberExpression;
    column?: NumberExpression;
  };
};

// ── Component options ─────────────────────────────────────────────────────────

/**
 * Options for `hostApi.ui.panel(id, options, child)`.
 *
 * All positioning properties define the panel's **default** state. The user
 * may reposition/resize the panel at runtime; the runtime stores overrides
 * per-client automatically.
 *
 * @see panel.md
 * @see rendering.md — Resource resolution
 */
export type PanelOptions = {
  /**
   * Default normalised screen-space point where the panel attaches.
   * `(0,0)` = top-left, `(1,1)` = bottom-right.
   */
  anchor: { x: NumberExpression; y: NumberExpression };

  /**
   * Default normalised panel-space point that aligns with the anchor.
   * `(0,0)` = panel top-left, `(1,1)` = panel bottom-right.
   */
  pivot: { x: NumberExpression; y: NumberExpression };

  /** Default displacement from the aligned anchor/pivot point, in logical units. */
  offset: { x: NumberExpression; y: NumberExpression };

  /** Default panel dimensions in logical units. */
  size: { width: NumberExpression; height: NumberExpression };

  /** Default per-panel scale multiplier. Defaults to `1.0` (uses global UI scale). */
  scale?: NumberExpression;

  /**
   * Evaluated each tick. When false, the panel and all its children are not
   * rendered. Default: true.
   */
  visible?: ConditionExpression;

  /** Optional background texture for the panel. */
  background?: TextureResource;

  /** Optional border texture for the panel. */
  border?: TextureResource;
};

/**
 * Options for `hostApi.ui.box(options, children)`.
 *
 * @see box.md
 * @see rendering.md — Resource resolution
 */
export type BoxOptions = {
  /**
   * Size hint consumed by the parent layout.
   * - `width` is consumed by the parent's column track allocation.
   * - `height` is consumed by the parent's row sizing.
   */
  size?: ChildSize;

  /**
   * Grid layout for children. When present, this box is a grid container.
   * When absent, this box is a leaf node.
   */
  layout?: GridLayout;

  /** Optional background texture for the box. */
  background?: TextureResource;

  /** Optional border texture for the box. */
  border?: TextureResource;
};

/**
 * Options for `hostApi.ui.text(options)`.
 *
 * @see text-value.md
 * @see rendering.md — Resource resolution
 */
export type TextOptions = {
  /** Size hint consumed by the parent layout. */
  size?: ChildSize;

  /**
   * The string value to display.
   * Accepts any `StringExpression` — literals, TextMap lookups, or composed expressions.
   */
  value: StringExpression;

  /** Optional font for rendering the text. If omitted, uses platform default. */
  font?: FontResource;
};
// ── Child ─────────────────────────────────────────────────────────────────────

/**
 * An opaque UI component node returned by the factory functions
 * (`ui.box`, `ui.text`, `ui.number`).
 *
 * Do not construct directly — always use the factory functions on `UIApi`.
 *
 * @see box.md
 */
export type Child = { readonly _childBrand: unique symbol };

// ── UIApi ─────────────────────────────────────────────────────────────────────

/**
 * API surface for declaring UI panels and components.
 *
 * Scoped under `hostApi.ui`.
 *
 * ## Factory pattern
 *
 * All UI is declared through factory functions rather than plain objects.
 * Each function captures its arguments at module load time to build an
 * expression DAG. The runtime evaluates the DAG each tick and reconciles
 * the diff to the client.
 *
 * ```ts
 * hostApi.ui.panel("hud", { anchor: ..., size: ... }, (state, data) =>
 *   hostApi.ui.box({ layout: { columns: [{ scale: number.of(1) }] } }, (state, data) => [
 *     hostApi.ui.text({ value: state.actor.textMap.get("name") }),
 *   ])
 * );
 * ```
 *
 * @see overview.md
 * @see panel.md
 * @see box.md
 */
export type UIApi = {
  /**
   * Per-client UI state: actor and named state values.
   *
   * @see ui-state.md
   */
  state: UiStateApi;

  /**
   * UI action registration (set-value / clear-value).
   *
   * @see ui-state.md — Updating values
   */
  action: UIActionApi;

  /**
   * Declare a top-level panel at module load time.
   *
   * `child` is called once at load time with `UiStateApi` and `UiDataApi`
   * proxies and must return a single `Child` node (typically a Box).
   *
   * @param id     - Unique panel id within the module.
   * @param options - Default positioning, size, scale, and visibility.
   * @param child  - Called once at load time; returns the root Child node.
   *
   * @see panel.md
   */
  panel: (
    id: string,
    options: PanelOptions,
    child: (state: UiStateApi, data: UiDataApi) => Child,
  ) => void;

  /**
   * Create a Box layout container.
   *
   * `children` is called once at load time and returns the ordered array of
   * child nodes placed into the grid. To conditionally render a child, exclude
   * it from the returned array.
   *
   * Every Box requires a unique `id` within its parent's `children` array.
   * The runtime reconciliation algorithm uses `id` to match nodes across ticks.
   *
   * @param id       - Unique identifier within the parent's children array.
   * @param options  - Layout configuration and size hint.
   * @param children - Called once at load time; returns the children array.
   *
   * @see box.md
   */
  box: (
    id: string,
    options: BoxOptions,
    children: (state: UiStateApi, data: UiDataApi) => Child[],
  ) => Child;

  /**
   * Create a text leaf component that displays a `StringExpression`.
   *
   * Every TextValue requires a unique `id` within its parent's `children` array.
   * The runtime reconciliation algorithm uses `id` to match nodes across ticks.
   *
   * To conditionally render, exclude the result from the parent `box` children
   * array rather than using a visibility flag.
   *
   * @param id      - Unique identifier within the parent's children array.
   * @param options - Value and optional size hint.
   *
   * @see text-value.md
   */
  text: (id: string, options: TextOptions) => Child;
};

// ── UIActionApi ───────────────────────────────────────────────────────────────

/**
 * API for registering UI-only actions that mutate per-client state values.
 *
 * @see ui-state.md — Updating values
 */
export type UIActionApi = {
  register: (args: UIActionArgs) => void;
};

/**
 * Arguments for a UI action declaration.
 *
 * @see ui-state.md — Updating values
 */
export type UIActionArgs = {
  /** Unique action name within the module. */
  name: string;

  /** The effect to apply when this UI action is triggered. */
  effect:
    | { type: 'set-value'; value: string }
    | { type: 'clear-value'; value: string };
};
