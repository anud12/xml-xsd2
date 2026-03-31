# NumberValue

A `NumberValue` is a **leaf component** that displays a `NumberExpression` — typically bound to an entity's `NumberMap` value via UI state — with optional formatting. It has no children.

Created with `hostApi.ui.number(id, options)`. Every `NumberValue` requires a unique `id` — the reconciliation algorithm uses it to match nodes across ticks.

---

## Number format

```ts
type NumberFormat = {
  /**
   * Number of decimal places to display.
   * Default: 0 (integer display).
   * Example: decimals: 2 renders 3 as "3.00"
   */
  decimals?: number;

  /**
   * String prepended to the formatted number.
   * Example: string.of("$") renders 42 as "$42"
   */
  prefix?: StringExpression;

  /**
   * String appended to the formatted number.
   * Example: string.of("%") renders 75 as "75%"
   * Example: string.concat(string.of("/"), entity.numberMap.get("maxHp"))
   *          renders 50 as "50/100"
   */
  suffix?: StringExpression;
}
```

---

## Options shape

```ts
type NumberOptions = {
  /** Size hint consumed by the parent layout. */
  size?: ChildSize;

  /**
   * The number value to display.
   * Accepts any NumberExpression — literals, entity NumberMap lookups,
   * UI state bindings, or computed expressions.
   */
  value: NumberExpression;

  /** Optional formatting applied before display. */
  format?: NumberFormat;
}
```

---

## Usage patterns

### Actor NumberMap binding — integer

```ts
hostApi.ui.number("actor-strength", { value: hostApi.ui.state.actor.numberMap.get("strength") })
```

### Dynamic slash-separated max suffix

```ts
hostApi.ui.number("actor-hp", {
  value: hostApi.ui.state.actor.numberMap.get("hp"),
  format: {
    suffix: hostApi.string.concat(
      hostApi.string.of("/"),
      hostApi.ui.state.actor.numberMap.get("maxHp"),
    ),
  },
})
// renders: "50/100"
```

### Percentage with decimals

```ts
hostApi.ui.number("crit-chance", {
  value: hostApi.ui.state.actor.numberMap.get("critChance"),
  format: { decimals: 1, suffix: hostApi.string.of("%") },
})
// renders: "12.5%"
```

### Declared value binding (with fallback)

```ts
const selection = hostApi.ui.state.declare("selection")

hostApi.ui.number("selection-hp", {
  value: selection.asEntity
    .map(e => e.numberMap.get("hp"))
    .orElse(hostApi.number.of(0)),
  format: { suffix: hostApi.string.of(" HP") },
})
```

---

## Example — stats panel

```ts
export default (hostApi) => {
  hostApi.ui.panel(
    "stats",
    {
      anchor: { x: number.of(1),   y: number.of(0)  },
      pivot:  { x: number.of(1),   y: number.of(0)  },
      offset: { x: number.of(-16), y: number.of(16) },
      size:   { width: number.of(220), height: number.of(120) },
    },
    (state, data) =>
      hostApi.ui.box(
        "stats-grid",
        {
          layout: {
            columns: [
              { min: number.of(80) },
              { scale: number.of(1), align: "end" },
            ],
            gap: { row: number.of(4), column: number.of(8) },
          },
        },
        (state, data) => [
          hostApi.ui.text("hp-label", { value: string.of("HP") }),
          hostApi.ui.number("hp-value", {
            value: state.actor.numberMap.get("hp"),
            format: {
              suffix: hostApi.string.concat(
                hostApi.string.of("/"),
                state.actor.numberMap.get("maxHp"),
              ),
            },
          }),

          hostApi.ui.text("str-label", { value: string.of("Strength") }),
          hostApi.ui.number("str-value", { value: state.actor.numberMap.get("str") }),

          hostApi.ui.text("crit-label", { value: string.of("Crit") }),
          hostApi.ui.number("crit-value", {
            value: state.actor.numberMap.get("critChance"),
            format: { decimals: 1, suffix: hostApi.string.of("%") },
          }),
        ],
      ),
  )
}
```

```
HP          50/100
Strength        15
Crit         12.5%
```

---

## Cross-references

- [`box.md`](./box.md) — `Child`; `SizeConstraint`; `ChildSize`; conditional rendering via exclusion
- [`text-value.md`](./text-value.md) — equivalent component for `TextMap` values
- [`ui-state.md`](./ui-state.md) — UI state values used for bindings
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` used for `value`
- [`stringExpression.md`](../expressions/stringExpression.md) — `StringExpression` used for `prefix` and `suffix`
- [`maybeExpression.md`](../expressions/maybeExpression.md) — narrowing slot values before use
- [`entities.md`](../data-model/entities.md) — `NumberMap` on Entity
