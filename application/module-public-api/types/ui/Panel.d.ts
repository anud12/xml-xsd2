import {NumberExpression} from "../primitives/numberExpression";
import {TextureResource} from "../texture/TextureResource";
import {StringExpression} from "../primitives/stringExpression";
import {Entity, EntityExpression} from "module-public-api/types/Entity";

/**
 * Function type used to register a panel with the UI system.
 *
 * Called at module load time.  Accepts a fully configured `PanelOptions`
 * object; registration is side-effect based (the framework stores the
 * panel definition internally and returns nothing).
 *
 * @see specification/user-interface/panel.md
 */
export type RegisterPanelFunction = (panelOptions: PanelOptions) => {}

/**
 * Full configuration for a single UI panel.
 *
 * A Panel is a top-level, positioned UI surface declared at module load
 * time.  It renders a background texture, optional inline content, and
 * optionally arranges child panels via a grid layout.  All sizes are in
 * **logical units**; the client translates them to device pixels using
 * a global UI scale factor.
 *
 * **Required fields:** `id`, `size`, `background`.
 * **Optional fields** fall back to framework defaults (listed per-field).
 *
 * Positioning is a three-step process:
 *   1. `anchor` picks a normalised point on the screen (0–1 per axis).
 *   2. The panel's own anchor point (determined by the panel's internal
 *      pivot — not exposed here) is aligned to that screen point.
 *   3. `offset` nudges the result in logical units.
 *
 * All positioning properties define the panel's **default** state.
 * Users may reposition or resize panels at runtime; the client stores
 * those overrides automatically.
 *
 * @see specification/user-interface/panel.md
 * @see specification/user-interface/rendering.md
 * @example
 *   hostApi.registerPanel({
 *     id: "hud",
 *     anchor: { x: number.of(0), y: number.of(0) },
 *     offset: { top: number.of(16), bottom: number.of(0),
 *              left: number.of(16), right: number.of(0) },
 *     size: { width: number.of(200), height: number.of(120) },
 *     background: hostApi.texture.of("ui-panel-bg.exr"),
 *   });
 */
export type PanelOptions = {
  /**
   * Unique identifier for the panel within the module.
   *
   * Used by the runtime reconciliation algorithm to diff successive
   * evaluations — matching same-id panels across ticks so that only
   * changed state is sent to the client.
   *
   * Must be unique among all registered panels.  Ids are declared at
   * load time and should always be constant strings (never dynamic).
   * If two panels share the same id, the later registration overwrites
   * the earlier one.
   *
   * @required
   * @see specification/user-interface/concepts.md — Component Identity
   * @example "status-bar"
   */
  id: string;

  /**
   * Normalised screen-space point where the panel attaches.
   *
   * Each axis is a value in the range [0, 1]:
   * - `0`   → start edge  (left for X, top for Y)
   * - `0.5` → centre
   * - `1`   → end edge    (right for X, bottom for Y)
   *
   * The anchor is **re-computed each frame**, so panel position stays
   * correct when the screen resizes.  Values outside [0, 1] are clamped.
   *
   * Both `x` and `y` are `NumberExpression`, so anchor position can be
   * driven dynamically (e.g. bound to a rule or entity value).
   *
   * @default { x: 0, y: 0 }  (top-left corner of screen)
   * @see specification/user-interface/panel.md — Positioning
   * @example Pin to centre:  { x: number.of(0.5), y: number.of(0.5) }
   */
  anchor?: { x: NumberExpression; y: NumberExpression };

  /**
   * Per-side displacement in logical units after anchor alignment.
   *
   * Each side (top / bottom / left / right) is independent and
   * optional.  Specifying only the sides you need is sufficient.
   *
   * | Side   | Direction of positive offset          |
   * |--------|---------------------------------------|
   * | top    | moves panel **down** from pivot Y     |
   * | bottom | moves panel **up**   from anchor Y    |
   * | left   | moves panel **right** from pivot X    |
   * | right  | moves panel **left**  from anchor X   |
   *
   * This four-sided model lets you push the panel away from a
   * particular screen edge.  For example, to inset a top-left panel
   * by 16 units on both axes:
   * ```ts
   * offset: { top: number.of(16), left: number.of(16) }
   * ```
   *
   * @default { top: 0, bottom: 0, left: 0, right: 0 }
   * @see specification/user-interface/panel.md — Positioning
   */
  offset?: {
    /** Vertical offset from pivot Y. Positive values shift the panel downward. */
    top: NumberExpression;
    /** Vertical offset from anchor Y. Positive values shift the panel upward. */
    bottom: NumberExpression;
    /** Horizontal offset from pivot X. Positive values shift the panel rightward. */
    left: NumberExpression;
    /** Horizontal offset from anchor X. Positive values shift the panel leftward. */
    right: NumberExpression;
  };

  /**
   * Width and height of the panel in logical units.
   *
   * Both values are `NumberExpression`, allowing sizes to be computed
   * from rules, entity bindings, or arithmetic on other expressions.
   * The client translates logical units to device pixels using the
   * global UI scale factor.
   *
   * @required
   * @see specification/user-interface/rendering.md — Coordinate System
   * @example { width: number.of(200), height: number.of(100) }
   */
  size: { width: NumberExpression; height: NumberExpression };

  /**
   * Background texture drawn behind the panel's content and children.
   *
   * Obtained via `hostApi.texture.of(path)`, which returns a
   * `TextureResource` handle.  The client resolves the texture at
   * render time.  If the texture path is invalid or missing, the
   * client falls back to a platform default and logs a warning.
   *
   * All panel content (inline content and children) is **clipped**
   * to panel bounds — nothing overflows.
   *
   * @required
   * @see specification/user-interface/rendering.md — Resource Resolution
   * @example hostApi.texture.of("ui-panel-bg.exr")
   */
  background: TextureResource;

  /**
    * Inline content component rendered inside the panel, behind any
    * child panels.
    *
    * One of four component types (see `PanelContent`):
    * - `EntityTextValueComponent`   — displays a text attribute from an entity
    * - `ConstantTextComponent`      — displays a static or expression-driven string
    * - `EntityNumberValueComponent`  — evaluates a value lambda against an entity
    * - `ConstantNumberComponent`     — displays a static or expression-driven number
    *
    * Every content component carries an `align` property that selects
    * one of nine positions inside the panel (from `"top-left"` to
    * `"bottom-right"`; see `PanelContent` for the full table).
    *
    * If both `content` and `children` are present, content is rendered
    * **first** (behind children).  Omit if the panel has no inline
    * content.
    *
    * @default undefined  (no inline content rendered)
    * @see specification/user-interface/text-value.md
    * @example
    *   content: {
    *     type: "constant",
    *     value: string.of("Hello"),
    *     align: "center"
    *   }
    */
   content?: PanelContent;

  /**
   * Click handler for the panel region.
   *
   * Currently only the `"emitAction"` handler type is supported:
   * clicking the panel dispatches a named action event through the
   * framework's event bus.  Listeners registered via `registerAction`
   * for that `actionName` will receive the event.
   *
   * Clicks are resolved by the client using visible bounds only —
   * clipped or truncated content does not register clicks.  When
   * panels overlap, the highest z-index panel (front-most) captures
   * the click.
   *
   * @default undefined  (panel is not clickable)
   * @see specification/user-interface/rendering.md — Hit Detection
   * @example
   *   onClick: {
   *     type: "emitAction",
   *     actionName: string.of("open-inventory")
   *   }
   */
  onClick?: PanelOnClickHandler;

  /**
   * Grid-layout configuration for arranging child panels.
   *
   * When provided, `children` are placed into a grid whose column
   * count and sizing are derived from the `columns` track definitions.
   * The layout algorithm respects `rowFirst`, `reverse`, and `gap`
   * settings (see `GridLayout` for details).
   *
   * If `layout` is absent, child panels fall back to **free-positioning**
   * mode: each child positions itself via its own `anchor` / `offset`
   * values within the parent panel's coordinate space.
   *
   * Content exceeding grid bounds is **clipped silently** — there is
   * no scrolling or overflow indicator.
   *
   * @default undefined  (free-positioning mode)
   * @see specification/user-interface/box.md — Grid Layout
   * @example Flex row (2 equal children):
   *   layout: { columns: [{ weight: number.of(1) }, { weight: number.of(1) }] }
   */
  layout?: GridLayout;

  /**
   * Array of child panel definitions nested inside this panel.
   *
   * Each child is a full `PanelOptions` object and may itself contain
   * further children, enabling arbitrary nesting depth.  Children
   * share the parent panel's coordinate space and are clipped to the
   * parent's bounds.
   *
   * Children are subject to the parent's `layout` configuration if
   * present; otherwise they position independently via their own
   * `anchor` and `offset` values.
   *
   * Every child must have a unique `id` within its parent's
   * `children` array (the runtime reconciliation algorithm uses
   * these ids to diff across ticks).
   *
   * @default undefined  (no children)
   * @see specification/user-interface/concepts.md — Component Identity
   */
  children?: PanelOptions[];
};

/**
 * Union type for inline content components rendered inside a panel.
 *
 * Each variant carries an `align` property that selects one of nine
 * positions inside the panel:
 *
 * | Value            | Horizontal    | Vertical     |
 * |------------------|---------------|--------------|
 * | `"top"`          | centred       | top edge     |
 * | `"top-left"`     | left edge     | top edge     |
 * | `"top-right"`    | right edge    | top edge     |
 * | `"center"`       | centred       | centred      |
 * | `"center-left"`  | left edge     | centred      |
 * | `"center-right"` | right edge    | centred      |
 * | `"bottom"`       | centred       | bottom edge  |
 * | `"bottom-left"`  | left edge     | bottom edge  |
 * | `"bottom-right"` | right edge    | bottom edge  |
 *
 * @see specification/user-interface/text-value.md
 */
export type PanelContent = (EntityTextValueComponent
  | ConstantTextComponent
  | EntityNumberValueComponent
  | ConstantNumberComponent
  ) & {
  /**
   * Position of the content inside the panel.
   *
   * Selects one of nine alignment positions (see type declaration for
   * full table).  Text content is vertically centred within its
   * allocated region.  Text exceeding panel width is **truncated
   * silently** — no ellipsis is shown.
   *
   * @default "center"
   */
  align: "top"
    | "top-left"
    | "top-right"
    | "center"
    | "center-left"
    | "center-right"
    | "bottom"
    | "bottom-left"
    | "bottom-right";
};

/**
 * Displays a textual attribute from an entity.
 *
 * The framework looks up the entity by `entityId`, reads the
 * attribute named by `name` from the entity's text map, and
 * renders the resulting string.
 *
 * - `type`: literal `"entityTextValue"` (discriminant).
 * - `name`: `StringExpression` resolving to the entity attribute key.
 * - `entityId`: optional `StringExpression` resolving to the entity
 *   identifier.  When omitted the framework uses the current context
 *   entity (typically the entity selected by the view scope).
 *
 * @default entityId: current context entity
 * @see specification/user-interface/concepts.md — State Binding
 * @example
 *   {
 *     type: "entityTextValue",
 *     entityId: string.of("player"),
 *     name: string.of("characterName"),
 *     align: "center"
 *   }
 */
export type EntityTextValueComponent = {
  /**
    * Discriminant value — must be the literal string `"entityTextValue"`.
    * Tells the framework to read a text attribute from an entity.
    */
  type: "entityTextValue";
  value?: (entity: Entity) =>  StringExpression;
  /**
    * Expression that resolves to the attribute key name to read from
    * the entity's text map.  Used as an alternative to the `value`
    * lambda for simple text-attribute display.
    */
  name?: StringExpression;
  /**
    * Expression that resolves to the entity ID whose attribute should
    * be read.  If not supplied the framework resolves the attribute
    * from the current context entity (the one in focus or selection
    * scope).
    *
    * @default current context entity
    */
  entityId?: StringExpression;
};

/**
 * Displays a fixed or expression-driven string.
 *
 * The evaluated string is rendered as-is.  Unlike entity-based
 * components, this type does not depend on entity state.
 *
 * - `type`: literal `"constant"` (discriminant).
 * - `value`: `StringExpression` whose evaluated string is rendered.
 *
 * @see specification/user-interface/text-value.md
 * @example
 *   {
 *     type: "constant",
 *     value: string.of("Health: 100"),
 *     align: "top-left"
 *   }
 */
export type ConstantTextComponent = {
  /**
   * Discriminant value — must be the literal string `"constant"`.
   * Tells the framework to render a static text value.
   */
  type: "constant";
  /**
   * Expression whose evaluated string is rendered inside the panel.
   * May be a literal, a composed expression, or a rule reference.
   */
  value: StringExpression;
};

/**
 * Displays a numeric attribute from an entity.
 *
 * The framework looks up the entity by `entityId`, evaluates the
 * `value` lambda against the entity, and renders the resulting
 * string.  The lambda receives the entity instance and should
 * return a `StringExpression` (e.g. via `entity.getNumber(key).orElse(fallback)`).
 *
 * The value lambda is compiled into an AST at module load time.
 * The AST captures the entity key lookup chain and any fallback
 * values, so the runtime evaluates the expression structurally
 * without parsing JavaScript source code.
 *
 * - `type`: literal `"entityNumberValue"` (discriminant).
 * - `value`: `(entity: Entity) => StringExpression` that extracts
 *   and formats the displayed value from the entity.
 * - `entityId`: optional `StringExpression` resolving to the entity
 *   identifier.  When omitted the framework uses the current context
 *   entity.
 *
 * @default entityId: current context entity
 * @see specification/user-interface/concepts.md — State Binding
 * @example
 *   {
 *     type: "entityNumberValue",
 *     entityId: string.of("player"),
 *     value: (entity) => entity.getNumber("hp").orElse("None"),
 *     align: "center"
 *   }
 */
export type EntityNumberValueComponent = {
  /**
   * Discriminant value — must be the literal string `"entityNumberValue"`.
   * Tells the framework to evaluate a value expression against an entity.
   */
  type: "entityNumberValue";
  value: (entity: Entity) =>  NumberExpression;
  /**
   * Expression that resolves to the entity ID whose attribute should
   * be read.  If not supplied the framework resolves the attribute
   * from the current context entity.
   *
   * @default current context entity
   */
  entityId?: StringExpression;
};

/**
 * Displays a fixed or expression-driven number.
 *
 * The evaluated number is rendered as-is.  Unlike entity-based
 * components, this type does not depend on entity state.
 *
 * - `type`: literal `"constantNumber"` (discriminant).
 * - `value`: `NumberExpression` whose evaluated number is rendered.
 *
 * @example
 *   {
 *     type: "constantNumber",
 *     value: number.of(42),
 *     align: "bottom-right"
 *   }
 */
export type ConstantNumberComponent = {
  /**
   * Discriminant value — must be the literal string `"constantNumber"`.
   * Tells the framework to render a static numeric value.
   */
  type: "constantNumber";
  /**
   * Expression whose evaluated number is rendered inside the panel.
   * May be a literal, a computed expression, or a rule reference.
   */
  value: NumberExpression;
};

/**
 * Click-handler definition for a panel.
 *
 * Only one handler type is currently supported:
 *
 * - `"emitAction"` — dispatches a named action on the framework
 *   event bus.  Listeners registered via `registerAction` for the
 *   matching `actionName` will receive the event payload (which
 *   includes the panel `id` and click coordinates).
 *
 * @see specification/interaction/actions.md
 */
export type PanelOnClickHandler = {
  /**
   * Discriminant — currently only `"emitAction"` is supported.
   * Indicates that clicking the panel fires an action event through
   * the framework's event system.
   */
  type: "emitAction";
  /**
   * `StringExpression` that resolves to the registered action name.
   * The framework broadcasts this action name on the event bus each
   * time the panel is clicked.  The action must have been previously
   * registered via `hostApi.registerAction`.
   */
  actionName: StringExpression;
};

/**
 * Grid-layout configuration applied to a panel's children.
 *
 * When a parent panel specifies `layout`, all direct children are
 * placed into grid cells according to these rules:
 *
 * 1. **columns** — array of `TrackDefinition` objects.  The array
 *    length determines how many columns the grid has.  Each element
 *    describes the track's sizing constraints (`min`, `max`, `weight`)
 *    and its inner alignment (`align`).
 *
 * 2. **rowFirst** — fill direction.
 *    - `true`  (default): children fill left → right, then wrap to
 *      the next row (standard row-major order).
 *    - `false` : children fill top → bottom, then wrap to the next
 *      column (column-major order).
 *
 * 3. **reverse** — placement reversal.
 *    - `false` (default): children are placed in declaration order.
 *    - `true`  : children are placed in reverse declaration order
 *      (last child first).
 *
 * 4. **gap** — spacing between adjacent cells.
 *    - `row`    : vertical gap (between rows) in logical units.
 *    - `column` : horizontal gap (between columns) in logical units.
 *    Both are optional; unspecified gaps default to `0`.
 *
 * **Common patterns:**
 * - Flex row:     `columns: [{ weight: 1 }, { weight: 1 }, ...]`
 * - Flex column:  `columns: [{ weight: 1 }]` (single column, children stack)
 * - Label|value:  `columns: [{ min: 80 }, { weight: 1, align: "end" }]`
 * - Fixed grid:   `columns: [{ min: 48 }, { min: 48 }, ...]`
 *
 * If `layout` is absent on the parent, children are positioned
 * independently via their own `anchor` and `offset` values
 * (free-positioning mode).
 *
 * @default rowFirst: true
 * @default reverse: false
 * @default gap: { row: 0, column: 0 }
 * @see specification/user-interface/box.md — Grid Layout
 * @see specification/user-interface/concepts.md — Size Constraints
 */
export type GridLayout = {
  /**
   * Column track definitions.  The number of elements in this array
   * sets the grid's column count.  Each element defines sizing
   * constraints (`min`, `max`, `weight`) and an optional inner
   * alignment (`align`).  All children in the same column share the
   * same track width, synchronizing column widths across rows.
   *
   * When omitted the grid infers equal-width columns that divide
   * the parent panel's width evenly.
   *
   * @default equal-width columns inferred from child count
   */
  columns?: TrackDefinition[];
  /**
   * Fill order flag.  `true` fills row by row (left → right, then
   * wrap down).  `false` fills column by column (top → bottom,
   * then wrap right).
   *
   * @default true
   */
  rowFirst?: boolean;
  /**
   * Reversal flag.  `true` places children in reverse declaration
   * order (last defined child appears first in the grid).
   *
   * @default false
   */
  reverse?: boolean;
  /**
   * Cell spacing.  `row` controls vertical gap between rows;
   * `column` controls horizontal gap between columns.  Either or
   * both may be omitted — missing values default to `0`.
   *
   * @default { row: 0, column: 0 }
   */
  gap?: { row?: NumberExpression; column?: NumberExpression };
};

/**
 * Definition for a single grid track (column).
 *
 * Extends `SizeConstraint` with an `align` property that controls
 * how child content is positioned inside the track cell.
 *
 * @see specification/user-interface/concepts.md — Anchor Positioning
 * @see specification/user-interface/box.md — Track Definition
 */
export type TrackDefinition = SizeConstraint & {
  /**
   * Inner alignment of child content within the track cell.
   *
   * - `"start"` (default): content is flushed to the leading edge
   *   of the track (left for columns, top for rows).
   * - `"end"`  : content is flushed to the trailing edge
   *   (right for columns, bottom for rows).
   *
   * @default "start"
   */
  align?: "start" | "end";
};

/**
 * Sizing constraints applied to a grid track or panel dimension.
 *
 * The layout engine resolves actual track sizes by:
 * 1. Satisfying every track's `min` floor.
 * 2. Distributing remaining free space proportionally by `weight`
 *    (analogous to CSS `flex-grow` / `fr` units).
 * 3. Clamping the result to the track's `max` ceiling.
 *
 * All three fields are optional; when omitted the framework picks
 * sensible defaults based on available space and sibling constraints.
 *
 * @see specification/user-interface/concepts.md — Size Constraints
 * @see specification/user-interface/box.md — Size Constraint
 */
export type SizeConstraint = {
  /**
   * Minimum size floor in logical units.
   * The track will never shrink below this value, even if space is
   * scarce.  Omit to allow the track to shrink to zero.
   *
   * @default 0
   */
  min?: NumberExpression;
  /**
   * Maximum size ceiling in logical units.
   * The track will never grow beyond this value, even if excess
   * space is available.  Omit to allow unlimited growth.
   *
   * @default Infinity
   */
  max?: NumberExpression;
  /**
   * Proportional weight used to divide remaining space after all
   * `min` constraints are satisfied.  A track with `weight: 2`
   * receives twice the leftover space of a sibling with
   * `weight: 1`.  Behaves like CSS `flex-grow` or `fr` units.
   *
   * Tracks with no explicit weight are treated as `weight: 0`
   * (they keep only their minimum size and do not participate in
   * the flexible space distribution).
   *
   * @default 0
   */
  weight?: NumberExpression;
};
