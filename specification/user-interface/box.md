# Box

A `Box` is a **layout block** — the building block of all panel content. Boxes form a tree rooted at a `Panel`'s `child` function. Each Box is both a **section** (a grouping of content, analogous to `<div>` in HTML) and a **layout manager** (it arranges its children using a grid model).

---

## Child union

A `Box` child can be a container (`Box`) or a leaf content component:

```ts
type Child = BoxDeclaration | TextValueDeclaration | NumberValueDeclaration
```

See [`text-value.md`](./text-value.md) and [`number-value.md`](./number-value.md) for leaf component declarations.

---

## Conditional rendering

There is no `visible` flag on any component. To conditionally render a child, **exclude it from the returned array**. Presence in the array equals rendered; absence equals not rendered.

```ts
// Show a "buffs" row only when the actor has buffs
children: (state, data) => [
  { type: "text", value: string.of("HP") },
  { type: "number", value: state.actor.numberMap.get("hp") },
  ...(someCondition ? [buffsRow] : []),
]
```

Since `children` is called once at load time with proxy objects, conditional inclusion must be expressed using expression handles — not runtime booleans. Use `maybe` and `list` expressions to produce conditional content from proxy values.

---

## Size constraint

All child types share `SizeConstraint` — a size hint read by the parent layout to distribute space:

```ts
type SizeConstraint = {
  /** Minimum size in logical units. Never sized below this. */
  min?: NumberExpression;

  /** Maximum size in logical units. Never sized above this. */
  max?: NumberExpression;

  /**
   * Weight for claiming remaining space after min sizes are satisfied.
   * Behaves like CSS flex-grow / fr units.
   * A child with scale 2 claims twice as much remaining space as one with scale 1.
   * Defaults to 0 (sized to min, or to content if no min).
   */
  scale?: NumberExpression;
}

type ChildSize = {
  width?:  SizeConstraint;
  height?: SizeConstraint;
}
```

---

## Layout

A Box with a `layout` property acts as a **grid container** for its children.

### Track definition

Column tracks extend `SizeConstraint` with an `align` property that controls how content within the track is positioned:

```ts
type TrackDefinition = {
  min?:   NumberExpression;
  max?:   NumberExpression;
  scale?: NumberExpression;

  /**
   * Alignment of content within this track.
   * "start" (default): content aligns to the track start (left for columns, top for rows).
   * "end": content aligns to the track end (right for columns, bottom for rows).
   */
  align?: "start" | "end";
}
```

### Grid model

All layout is expressed as a grid. Flex-row and flex-column are degenerate cases:

| Intent | How to express it |
|---|---|
| Flex row (equal columns) | `columns: [{ scale:1 }, { scale:1 }, ...]` |
| Flex column | `columns: [{ scale:1 }]` — one column, children stack in rows |
| Fixed label column + stretchy value column | `columns: [{ min:80 }, { scale:1 }]` |
| Synchronized 3-column table | `columns: [{ min:60 }, { scale:1 }, { min:60 }]` |

```ts
type GridLayout = {
  /**
   * Column track definitions. Each entry defines one column's sizing and alignment.
   * The number of entries determines the number of columns.
   * All children in the same column share the same track width —
   * this synchronizes column widths across rows.
   */
  columns: TrackDefinition[];

  /**
   * Auto-flow direction.
   * true (default): row-first — children fill left→right, then wrap to the next row.
   * false: column-first — children fill top→bottom, then move to the next column.
   */
  rowFirst?: boolean;

  /**
   * Reverses the order in which children are placed into slots.
   * When true, children are placed last-to-first rather than first-to-last.
   * Default: false.
   */
  reverse?: boolean;

  /** Space between cells in logical units. */
  gap?: {
    row?:    NumberExpression;
    column?: NumberExpression;
  };
}
```

### Auto-placement

Children are placed automatically in array order, following the `rowFirst` direction.

`rowFirst: true` (default) — row-major:

```
columns: [A, B, C]

  col 0   col 1   col 2
  [ 0 ]   [ 1 ]   [ 2 ]
  [ 3 ]   [ 4 ]   [ 5 ]
  [ 6 ]   ...
```

`rowFirst: false` — column-major:

```
columns: [A, B]

  col 0   col 1
  [ 0 ]   [ 2 ]
  [ 1 ]   [ 3 ]
```

### Track `align`

```
columns: [{ scale:1 }, { scale:1, align:"end" }]
children returns: [A, B, C, D]

  col 0     col 1
  [ A  ]   [  B ]   ← B end-aligned within col 1
  [ C  ]   [  D ]   ← D end-aligned within col 1
```

### Layout `reverse`

```
columns: [{ scale:1 }, { scale:1 }]   reverse: true
children returns: [A, B, C, D, E]

  col 0   col 1
  [ E ]   [ D ]
  [ C ]   [ B ]
  [ A ]
```

---

## Declaration shape

```ts
type BoxDeclaration = {
  type: "box";

  /** Optional identifier. */
  id?: string;

  /**
   * This box's own size hint. Used by the parent layout to allocate space.
   * - width  is consumed by the parent's column track allocation.
   * - height is consumed by the parent's row sizing.
   */
  size?: ChildSize;

  /**
   * Grid layout for children. When present, this box is a grid container.
   * When absent, this box is a leaf node with no children.
   */
  layout?: GridLayout;

  /**
   * Called once at load time to build this box's children expression DAG.
   * Returns the ordered list of children placed into the grid.
   * To conditionally render a child, exclude it from the returned array.
   * Only valid when layout is present.
   */
  children?: (state: UiStateApi, data: UiDataApi) => Child[];
}
```

---

## Examples

### Flex row — three equal columns

```ts
{
  type: "box",
  layout: {
    columns: [{ scale: number.of(1) }, { scale: number.of(1) }, { scale: number.of(1) }],
    gap: { column: number.of(8) },
  },
  children: (state, data) => [ /* A */, /* B */, /* C */ ],
}
```

```
[ A | B | C ]
```

---

### Flex column — single column, children stack vertically

```ts
{
  type: "box",
  layout: {
    columns: [{ scale: number.of(1) }],
    gap: { row: number.of(4) },
  },
  children: (state, data) => [ /* A */, /* B */, /* C */ ],
}
```

```
[ A ]
[ B ]
[ C ]
```

---

### Stats panel — label | value grid

```ts
{
  type: "box",
  layout: {
    columns: [
      { min: number.of(80) },                  // labels — fixed min width
      { scale: number.of(1), align: "end" },   // values — stretch, end-aligned
    ],
    gap: { row: number.of(4), column: number.of(8) },
  },
  children: (state, data) => [
    { type: "text",   value: string.of("Health") },
    { type: "text",   value: string.of("100/100") },
    { type: "text",   value: string.of("Mana") },
    { type: "text",   value: string.of("50/100") },
  ],
}
```

```
Health    100/100
Mana       50/100
```

---

### Conditional child — only shown when target is present

```ts
{
  type: "box",
  layout: { columns: [{ scale: number.of(1) }] },
  children: (state, data) => [
    { type: "text", value: string.of("Actor HP") },
    { type: "number", value: state.actor.numberMap.get("hp") },
    // target row only included when the value is declared
    ...state.value("target").asEntity.map(e => ([
      { type: "text", value: string.of("Target HP") },
      { type: "number", value: e.numberMap.get("hp") },
    ])).orElse([]),
  ],
}
```

---

### Inventory grid — 4 columns, row-first

```ts
{
  type: "box",
  layout: {
    columns: Array(4).fill({ min: number.of(48) }),
    gap: { row: number.of(4), column: number.of(4) },
  },
  children: (state, data) => [
    /* slot 0 */, /* slot 1 */, /* slot 2 */, /* slot 3 */,
    /* slot 4 */, /* slot 5 */, /* slot 6 */, /* slot 7 */,
  ],
}
```

```
[ 0 ][ 1 ][ 2 ][ 3 ]
[ 4 ][ 5 ][ 6 ][ 7 ]
```

---

### Column-first flow

```ts
{
  type: "box",
  layout: {
    columns: [{ scale: number.of(1) }, { scale: number.of(1) }],
    rowFirst: false,
    gap: { column: number.of(8) },
  },
  children: (state, data) => [ /* A */, /* B */, /* C */, /* D */, /* E */ ],
}
```

```
[ A ][ C ][ E ]
[ B ][ D ][   ]
```

---

## Cross-references

- [`panel.md`](./panel.md) — Panel declaration; Box is the `child` of a Panel
- [`text-value.md`](./text-value.md) — `TextValueDeclaration` leaf component
- [`number-value.md`](./number-value.md) — `NumberValueDeclaration` leaf component
- [`overview.md`](./overview.md) — UI system entry point
- [`ui-state.md`](./ui-state.md) — `UiStateApi` and `UiDataApi` passed to `children`
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` for size and gap values
- [`maybeExpression.md`](../expressions/maybeExpression.md) — used for conditional child inclusion


All child types share `SizeConstraint` — a size hint read by the parent layout to distribute space:

```ts
type SizeConstraint = {
  /** Minimum size in logical units. Never sized below this. */
  min?: NumberExpression;

  /** Maximum size in logical units. Never sized above this. */
  max?: NumberExpression;

  /**
   * Weight for claiming remaining space after min sizes are satisfied.
   * Behaves like CSS flex-grow / fr units.
   * A child with scale 2 claims twice as much remaining space as one with scale 1.
   * Defaults to 0 (sized to min, or to content if no min).
   */
  scale?: NumberExpression;
}

type ChildSize = {
  width?:  SizeConstraint;
  height?: SizeConstraint;
}
```

---

## Layout

A Box with a `layout` property acts as a **grid container** for its children.

### Track definition

Column tracks extend `SizeConstraint` with an `align` property that controls how content within the track is positioned:

```ts
type TrackDefinition = {
  min?:   NumberExpression;
  max?:   NumberExpression;
  scale?: NumberExpression;

  /**
   * Alignment of content within this track.
   * "start" (default): content aligns to the track start (left for columns, top for rows).
   * "end": content aligns to the track end (right for columns, bottom for rows).
   */
  align?: "start" | "end";
}
```

### Grid model

All layout is expressed as a grid. Flex-row and flex-column are degenerate cases:

| Intent | How to express it |
|---|---|
| Flex row (equal columns) | `columns: [{ scale:1 }, { scale:1 }, ...]` |
| Flex column | `columns: [{ scale:1 }]` — one column, children stack in rows |
| Fixed label column + stretchy value column | `columns: [{ min:80 }, { scale:1 }]` |
| Synchronized 3-column table | `columns: [{ min:60 }, { scale:1 }, { min:60 }]` |

```ts
type GridLayout = {
  /**
   * Column track definitions. Each entry defines one column's sizing and alignment.
   * The number of entries determines the number of columns.
   * All children in the same column share the same track width —
   * this synchronizes column widths across rows.
   */
  columns: TrackDefinition[];

  /**
   * Auto-flow direction.
   * true (default): row-first — children fill left→right, then wrap to the next row.
   * false: column-first — children fill top→bottom, then move to the next column.
   */
  rowFirst?: boolean;

  /**
   * Reverses the order in which children are placed into slots.
   * When true, children are placed last-to-first rather than first-to-last.
   * Default: false.
   */
  reverse?: boolean;

  /** Space between cells in logical units. */
  gap?: {
    row?:    NumberExpression;
    column?: NumberExpression;
  };
}
```

### Auto-placement

Children are placed automatically in declaration order, following the `rowFirst` direction.

`rowFirst: true` (default) — row-major:

```
columns: [A, B, C]

  col 0   col 1   col 2
  [ 0 ]   [ 1 ]   [ 2 ]
  [ 3 ]   [ 4 ]   [ 5 ]
  [ 6 ]   ...
```

`rowFirst: false` — column-major:

```
columns: [A, B]

  col 0   col 1
  [ 0 ]   [ 2 ]
  [ 1 ]   [ 3 ]
```

### Track `align`

```
columns: [{ scale:1 }, { scale:1, align:"end" }]
children: [A, B, C, D]

  col 0     col 1
  [ A  ]   [  B ]   ← B end-aligned within col 1
  [ C  ]   [  D ]   ← D end-aligned within col 1
```

### Layout `reverse`

```
columns: [{ scale:1 }, { scale:1 }]   reverse: true
children: [A, B, C, D, E]

  col 0   col 1
  [ E ]   [ D ]
  [ C ]   [ B ]
  [ A ]
```

---

## Declaration shape

```ts
type BoxDeclaration = {
  type: "box";

  /** Optional identifier. */
  id?: string;

  /**
   * Visibility condition. Evaluated each tick.
   * When false, this box and its entire subtree are not rendered.
   * Default: true.
   */
  visible?: ConditionExpression;

  /**
   * This box's own size hint. Used by the parent layout to allocate space.
   * - width  is consumed by the parent's column track allocation.
   * - height is consumed by the parent's row sizing.
   */
  size?: ChildSize;

  /**
   * Grid layout for children. When present, this box is a grid container.
   * When absent, this box is a leaf node with no children.
   */
  layout?: GridLayout;

  /** Child components. Only valid when layout is present. */
  children?: Child[];
}
```

---

## Examples

### Flex row — three equal columns

```ts
{
  type: "box",
  layout: {
    columns: [{ scale: number.of(1) }, { scale: number.of(1) }, { scale: number.of(1) }],
    gap: { column: number.of(8) },
  },
  children: [ /* A */, /* B */, /* C */ ],
}
```

```
[ A | B | C ]
```

---

### Stats panel — label | value grid

```ts
{
  type: "box",
  layout: {
    columns: [
      { min: number.of(80) },                  // labels — fixed min width
      { scale: number.of(1), align: "end" },   // values — stretch, end-aligned
    ],
    gap: { row: number.of(4), column: number.of(8) },
  },
  children: [
    { type: "text",   value: string.of("Health")   },
    { type: "text",   value: string.of("100/100")  },
    { type: "text",   value: string.of("Mana")     },
    { type: "text",   value: string.of("50/100")   },
  ],
}
```

```
Health    100/100
Mana       50/100
```

---

### Inventory grid — 4 columns, row-first

```ts
{
  type: "box",
  layout: {
    columns: Array(4).fill({ min: number.of(48) }),
    gap: { row: number.of(4), column: number.of(4) },
  },
  children: [ /* slot 0 */, /* slot 1 */, /* slot 2 */, /* slot 3 */,
              /* slot 4 */, /* slot 5 */, /* slot 6 */, /* slot 7 */ ],
}
```

```
[ 0 ][ 1 ][ 2 ][ 3 ]
[ 4 ][ 5 ][ 6 ][ 7 ]
```

---

## Cross-references

- [`panel.md`](./panel.md) — Panel declaration; Box is the `child` of a Panel
- [`text-value.md`](./text-value.md) — `TextValueDeclaration` leaf component
- [`number-value.md`](./number-value.md) — `NumberValueDeclaration` leaf component
- [`overview.md`](./overview.md) — UI system entry point
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` for size and gap values
- [`conditionExpression.md`](../expressions/conditionExpression.md) — `ConditionExpression` for visibility

---

## Layout

A Box with a `layout` property acts as a **grid container** for its children. A Box without `layout` is a leaf (no children to arrange).

### Grid model

All layout is expressed as a grid. Flex-row and flex-column are degenerate cases:

| Intent | How to express it |
|---|---|
| Flex row (equal columns) | `columns: [{ scale:1 }, { scale:1 }, ...]` |
| Flex column | `columns: [{ scale:1 }]` — one column, children stack in rows |
| Fixed label column + stretchy value column | `columns: [{ min:80 }, { scale:1 }]` |
| Synchronized 3-column table | `columns: [{ min:60 }, { scale:1 }, { min:60 }]` |

```ts
type GridLayout = {
  /**
   * Column track definitions. Each entry defines one column's sizing behaviour.
   * The number of entries determines the number of columns.
   * All children in the same column share the same track width —
   * this synchronizes column widths across rows.
   */
  columns: SizeConstraint[];

  /**
   * Auto-flow direction.
   * true (default): row-first — children fill left→right, then wrap to the next row.
   * false: column-first — children fill top→bottom, then move to the next column.
   */
  rowFirst?: boolean;

  /**
   * Reverses the order in which children are placed into slots.
   * When true, children are placed last-to-first rather than first-to-last.
   * Default: false.
   */
  reverse?: boolean;

  /** Space between cells in logical units. */
  gap?: {
    row?: NumberExpression;
    column?: NumberExpression;
  };
}
```

### Auto-placement

Children are placed automatically in declaration order, following the `rowFirst` direction.

`rowFirst: true` (default) — row-major:

```
columns: [A, B, C]

  col 0   col 1   col 2
  [ 0 ]   [ 1 ]   [ 2 ]
  [ 3 ]   [ 4 ]   [ 5 ]
  [ 6 ]   ...
```

`rowFirst: false` — column-major:

```
columns: [A, B]

  col 0   col 1
  [ 0 ]   [ 2 ]
  [ 1 ]   [ 3 ]
```

### Track `align`

Each column track declares how content within it is aligned:

```
columns: [{ scale:1 }, { scale:1, align:"end" }]
children: [A, B, C, D]

  col 0     col 1
  [ A  ]   [  B ]   ← B right-aligned within col 1
  [ C  ]   [  D ]   ← D right-aligned within col 1
```

### Layout `reverse`

When `reverse: true` on the layout, children are placed in reverse declaration order:

```
columns: [{ scale:1 }, { scale:1, reverse:true }]
children: [A, B, C, D, E]   (5 children, 2 columns, rowFirst)

Normal:          reverse on col 1:
  col0  col1       col0  col1
   A     B          A     -
   C     D          C     D
   E     -          E     B
```

---

## Declaration shape

```ts
type BoxDeclaration = {
  /** Optional identifier. */
  id?: string;

  /**
   * Visibility condition. Evaluated each tick.
   * When false, this box and its entire subtree are not rendered.
   * Default: true.
   */
  visible?: ConditionExpression;

  /**
   * This box's own size hint. Used by the parent layout to allocate space.
   * - width  is consumed by the parent's column track allocation.
   * - height is consumed by the parent's row sizing.
   */
  size?: BoxSize;

  /**
   * Grid layout for children. When present, this box is a grid container.
   * When absent, this box is a leaf node.
   */
  layout?: GridLayout;

  /** Child boxes. Only valid when layout is present. */
  children?: BoxDeclaration[];
}
```

---

## Examples

### Flex row — three equal columns

```ts
{
  layout: {
    columns: [
      { scale: number.of(1) },
      { scale: number.of(1) },
      { scale: number.of(1) },
    ],
    gap: { column: number.of(8) },
  },
  children: [ /* A */, /* B */, /* C */ ],
}
```

```
[ A | B | C ]
```

---

### Flex column — single column, children stack vertically

```ts
{
  layout: {
    columns: [{ scale: number.of(1) }],
    gap: { row: number.of(4) },
  },
  children: [ /* A */, /* B */, /* C */ ],
}
```

```
[ A ]
[ B ]
[ C ]
```

---

### Stats panel — label | value grid

```ts
{
  layout: {
    columns: [
      { min: number.of(80) },                   // labels — fixed min width
      { scale: number.of(1), align: "end" },    // values — stretch, end-aligned
    ],
    gap: { row: number.of(4), column: number.of(8) },
  },
  children: [
    { /* "Health"   */ }, { /* "100/100" */ },
    { /* "Mana"     */ }, { /* "50/100"  */ },
    { /* "Strength" */ }, { /* "15"      */ },
  ],
}
```

```
Health    100/100
Mana       50/100
Strength       15
```

---

### Inventory grid — 4 columns, row-first

```ts
{
  layout: {
    columns: [
      { min: number.of(48) },
      { min: number.of(48) },
      { min: number.of(48) },
      { min: number.of(48) },
    ],
    gap: { row: number.of(4), column: number.of(4) },
  },
  children: [ /* 0 */, /* 1 */, /* 2 */, /* 3 */,
              /* 4 */, /* 5 */, /* 6 */, /* 7 */ ],
}
```

```
[ 0 ][ 1 ][ 2 ][ 3 ]
[ 4 ][ 5 ][ 6 ][ 7 ]
```

---

### Column-first flow

```ts
{
  layout: {
    columns: [{ scale: number.of(1) }, { scale: number.of(1) }],
    rowFirst: false,
    gap: { column: number.of(8) },
  },
  children: [ /* A */, /* B */, /* C */, /* D */, /* E */ ],
}
```

```
[ A ][ C ][ E ]
[ B ][ D ][   ]
```

---

## Cross-references

- [`panel.md`](./panel.md) — Panel declaration; Box is the `child` of a Panel
- [`overview.md`](./overview.md) — UI system entry point
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` for size and gap values
- [`conditionExpression.md`](../expressions/conditionExpression.md) — `ConditionExpression` for visibility
