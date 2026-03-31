import type { NumberExpression } from '../primitives/numberExpression';
import type { StringExpression } from '../primitives/stringExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';
import type { UiStateApi, UiDataApi } from './ui-state';

/**
 * Minimum / maximum / proportional size hint shared by all child types and
 * column track definitions.
 *
 * @see box.md — Size constraint
 */
export type SizeConstraint = {
  /** Minimum size in logical units. The child is never sized below this. */
  min?: NumberExpression;

  /** Maximum size in logical units. The child is never sized above this. */
  max?: NumberExpression;

  /**
   * Weight for claiming remaining space after min sizes are satisfied.
   * Behaves like CSS `flex-grow` / `fr` units.
   * A child with `scale: 2` claims twice as much remaining space as one with `scale: 1`.
   * Defaults to 0 (sized to min, or to content when no min is set).
   */
  scale?: NumberExpression;
};

/**
 * Per-axis size hints for a child component.
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
   * - `"start"` (default): content aligns to the track start (left for columns, top for rows).
   * - `"end"`: content aligns to the track end (right for columns, bottom for rows).
   */
  align?: 'start' | 'end';
};

/**
 * Grid layout configuration for a Box container.
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
   * Reverses the order in which children are placed into slots.
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

/**
 * Leaf component — displays a `StringExpression`.
 *
 * To conditionally render a TextValue, exclude it from the parent Box's
 * `children` function return value rather than using a visibility flag.
 *
 * @see text-value.md
 */
export type TextValueDeclaration = {
  type: 'text';

  /** Optional identifier. */
  id?: string;

  /** Size hint consumed by the parent layout. */
  size?: ChildSize;

  /**
   * The string value to display.
   * Accepts any `StringExpression` — literals, TextMap lookups, or composed expressions.
   */
  value: StringExpression;
};

/**
 * Optional formatting applied to a `NumberValueDeclaration` before display.
 *
 * @see number-value.md
 */
export type NumberFormat = {
  /**
   * Number of decimal places to display. Default: 0 (integer display).
   * Example: `decimals: 2` renders `3` as `"3.00"`.
   */
  decimals?: number;

  /**
   * String prepended to the formatted number.
   * Example: `string.of("$")` renders `42` as `"$42"`.
   */
  prefix?: StringExpression;

  /**
   * String appended to the formatted number.
   * Example: `string.of("%")` renders `75` as `"75%"`.
   */
  suffix?: StringExpression;
};

/**
 * Leaf component — displays a `NumberExpression` with optional formatting.
 *
 * To conditionally render a NumberValue, exclude it from the parent Box's
 * `children` function return value rather than using a visibility flag.
 *
 * @see number-value.md
 */
export type NumberValueDeclaration = {
  type: 'number';

  /** Optional identifier. */
  id?: string;

  /** Size hint consumed by the parent layout. */
  size?: ChildSize;

  /**
   * The number value to display.
   * Accepts any `NumberExpression` — literals, NumberMap lookups, or computed expressions.
   */
  value: NumberExpression;

  /** Optional formatting applied before display. */
  format?: NumberFormat;
};

/**
 * Union of all valid child component types inside a Box.
 *
 * @see box.md — Child union
 */
export type Child = BoxDeclaration | TextValueDeclaration | NumberValueDeclaration;

/**
 * Layout block — forms the content tree inside a Panel.
 *
 * A Box with a `layout` property is a grid container and must declare
 * a `children` function. A Box without `layout` is a leaf node.
 *
 * ## Conditional rendering
 *
 * There is no `visible` flag. To conditionally render a child, exclude it
 * from the `children` function's return array. Presence in the array equals
 * rendered; absence equals not rendered.
 *
 * @see box.md
 */
export type BoxDeclaration = {
  type: 'box';

  /** Optional identifier. */
  id?: string;

  /**
   * Size hint consumed by the parent layout.
   * - `width` is consumed by the parent's column track allocation.
   * - `height` is consumed by the parent's row sizing.
   */
  size?: ChildSize;

  /**
   * Grid layout for children. When present, this box is a grid container.
   * When absent, this box is a leaf node with no children.
   */
  layout?: GridLayout;

  /**
   * Called once at load time to build this box's children expression DAG.
   *
   * `state` provides per-client UI state; `data` provides world data queries.
   * The returned array is the ordered list of children placed into the grid.
   * To conditionally render a child, exclude it from the returned array.
   *
   * Only valid when `layout` is present.
   */
  children?: (state: UiStateApi, data: UiDataApi) => Child[];
};

/**
 * Top-level UI window declared by a module at load time.
 *
 * ## Positioning
 *
 * `anchor`, `pivot`, `offset`, `size`, and `scale` define the panel's
 * **default** position and size. The runtime uses these for the initial
 * render. The user may reposition the panel at runtime; the runtime stores
 * that override per-client automatically.
 *
 * ## Child function
 *
 * `child` is called **once at load time** with `UiStateApi` and `UiDataApi`
 * proxies. It returns a `Child` tree of expression nodes. The runtime
 * evaluates this expression DAG each tick and reconciles the diff to the client.
 *
 * @see panel.md
 */
export type PanelDeclaration = {
  /** Unique panel id within the module. */
  id: string;

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

  /**
   * Default per-panel scale multiplier. Defaults to `1.0` (uses global UI scale).
   */
  scale?: NumberExpression;

  /**
   * Evaluated each tick. When false, the panel and all its children are not
   * rendered. Default: true.
   */
  visible?: ConditionExpression;

  /**
   * Called once at load time to build the panel's content expression DAG.
   *
   * `state` provides access to per-client UI state values and the actor.
   * `data` provides access to world data queries.
   * The returned `Child` is a tree of expression nodes evaluated by the
   * runtime each tick.
   */
  child: (state: UiStateApi, data: UiDataApi) => Child;
};

/**
 * API surface for declaring UI panels and UI actions.
 *
 * Scoped under `hostApi.ui`.
 *
 * @see overview.md
 */
export type UIApi = {
  /** Declare a panel at module load time. */
  registerPanel: (panel: PanelDeclaration) => void;

  /** Register a UI action (set-value / clear-value). */
  action: UIActionApi;

  /** Per-client UI state: actor and named state values. */
  state: UiStateApi;
};

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
