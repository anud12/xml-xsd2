# NumberValue

A `NumberValue` is a **leaf component** that displays a `NumberExpression` — typically bound to an entity's `NumberMap` value via UI state — with optional formatting. It has no children.

---

## Number format

```ts
type NumberFormat = {
  /**
   * Number of decimal places to display.
   * Default: 0 (integer display).
   * Example: decimals 2 renders 3 as "3.00"
   */
  decimals?: number;

  /**
   * String prepended to the formatted number.
   * Accepts a StringExpression so the prefix can reference dynamic values.
   * Example: string.of("$") renders 42 as "$42"
   */
  prefix?: StringExpression;

  /**
   * String appended to the formatted number.
   * Accepts a StringExpression so the suffix can reference dynamic values.
   * Example: string.of("%") renders 75 as "75%"
   * Example: string.concat(string.of("/"), entity.numberMap.get("maxHp"))
   *          renders 50 as "50/100"
   */
  suffix?: StringExpression;
}
```

---

## Declaration shape

```ts
type NumberValueDeclaration = {
  type: "number";

  /** Optional identifier. */
  id?: string;

  /**
   * This component's own size hint. Used by the parent layout to allocate space.
   */
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
{ type: "number", value: hostApi.ui.state.actor.numberMap.get("strength") }
```

### Dynamic slash-separated max suffix

```ts
{
  type: "number",
  value: hostApi.ui.state.actor.numberMap.get("hp"),
  format: {
    suffix: hostApi.string.concat(
      hostApi.string.of("/"),
      hostApi.ui.state.actor.numberMap.get("maxHp"),
    ),
  },
}
// renders: "50/100"
```

### Percentage with decimals

```ts
{
  type: "number",
  value: hostApi.ui.state.actor.numberMap.get("critChance"),
  format: { decimals: 1, suffix: hostApi.string.of("%") },
}
// renders: "12.5%"
```

### Declared value binding (with fallback)

```ts
const selection = hostApi.ui.state.declare("selection")

{
  type: "number",
  value: selection.asEntity
    .map(e => e.numberMap.get("hp"))
    .orElse(hostApi.number.of(0)),
  format: { suffix: hostApi.string.of(" HP") },
}
```

---

## Example — stats panel

```ts
hostApi.ui.registerPanel({
  id: "stats",
  anchor: { x: hostApi.number.of(1),   y: hostApi.number.of(0) },
  pivot:  { x: hostApi.number.of(1),   y: hostApi.number.of(0) },
  offset: { x: hostApi.number.of(-16), y: hostApi.number.of(16) },
  size:   { width: hostApi.number.of(220), height: hostApi.number.of(120) },
  child: (state, data) => ({
    type: "box",
    layout: {
      columns: [
        { min: hostApi.number.of(80) },
        { scale: hostApi.number.of(1), align: "end" },
      ],
      gap: { row: hostApi.number.of(4), column: hostApi.number.of(8) },
    },
    children: [
      { type: "text",   value: hostApi.string.of("HP") },
      {
        type: "number",
        value: state.actor.numberMap.get("hp"),
        format: {
          suffix: hostApi.string.concat(
            hostApi.string.of("/"),
            state.actor.numberMap.get("maxHp"),
          ),
        },
      },

      { type: "text",   value: hostApi.string.of("Strength") },
      { type: "number", value: state.actor.numberMap.get("str") },

      { type: "text",   value: hostApi.string.of("Crit") },
      {
        type: "number",
        value: state.actor.numberMap.get("critChance"),
        format: { decimals: 1, suffix: hostApi.string.of("%") },
      },
    ],
  }),
})
```

```
HP          50/100
Strength        15
Crit         12.5%
```

---

## Cross-references

- [`box.md`](./box.md) — `Child` union; `SizeConstraint`; `ChildSize`; conditional rendering via exclusion
- [`text-value.md`](./text-value.md) — equivalent component for `TextMap` values
- [`ui-state.md`](./ui-state.md) — UI state values used for bindings
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` used for `value`
- [`stringExpression.md`](../expressions/stringExpression.md) — `StringExpression` used for `prefix` and `suffix`
- [`maybeExpression.md`](../expressions/maybeExpression.md) — narrowing slot values before use
- [`conditionExpression.md`](../expressions/conditionExpression.md) — `ConditionExpression` for visibility
- [`entities.md`](../data-model/entities.md) — `NumberMap` on Entity
