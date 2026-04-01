import type { NumberExpression } from '../primitives/numberExpression';
import type { StringExpression } from '../primitives/stringExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';
import type { UiStateApi, UiDataApi } from './ui-state';

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
 *
 * @see box.md — Size constraint
 */
export type ChildSize = {
  width?: SizeConstraint;
  height?: SizeConstraint;
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
};

/**
 * Options for `hostApi.ui.box(options, children)`.
 *
 * @see box.md
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
};

/**
 * Options for `hostApi.ui.text(options)`.
 *
 * @see text-value.md
 */
export type TextOptions = {
  /** Size hint consumed by the parent layout. */
  size?: ChildSize;

  /**
   * The string value to display.
   * Accepts any `StringExpression` — literals, TextMap lookups, or composed expressions.
   */
  value: StringExpression;
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
