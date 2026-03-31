# TextValue

A `TextValue` is a **leaf component** that displays a `StringExpression` — typically bound to an entity's `TextMap` value via UI state. It has no children.

Created with `hostApi.ui.text(id, options)`. Every `TextValue` requires a unique `id` — the reconciliation algorithm uses it to match nodes across ticks.

---

## Options shape

```ts
type TextOptions = {
  /**
   * Size hint consumed by the parent layout.
   */
  size?: ChildSize;

  /**
   * The string value to display.
   * Accepts any StringExpression — literals, entity TextMap lookups,
   * UI state bindings, or composed expressions.
   */
  value: StringExpression;
}
```

---

## Usage patterns

### Literal string

```ts
hostApi.ui.text("health-label", { value: hostApi.string.of("Health") })
```

### Actor TextMap binding

```ts
hostApi.ui.text("actor-name", { value: hostApi.ui.state.actor.textMap.get("name") })
```

### Declared value binding (with fallback)

```ts
const selection = hostApi.ui.state.declare("selection")

hostApi.ui.text("selection-name", {
  value: selection.asEntity
    .map(e => e.textMap.get("name"))
    .orElse(hostApi.string.of("—")),
})
```

---

## Example — name plate panel

```ts
export default (hostApi) => {
  const selection = hostApi.ui.state.declare("selection")

  hostApi.ui.panel(
    "nameplate",
    {
      anchor:  { x: number.of(0.5), y: number.of(1) },
      pivot:   { x: number.of(0.5), y: number.of(1) },
      offset:  { x: number.of(0),   y: number.of(-16) },
      size:    { width: number.of(200), height: number.of(40) },
      visible: selection.isPresent,
    },
    (state, data) =>
      hostApi.ui.box(
        "nameplate-content",
        { layout: { columns: [{ scale: number.of(1) }] } },
        (state, data) => [
          hostApi.ui.text("name-text", {
            value: state.value("selection").asEntity
              .map(e => e.textMap.get("name"))
              .orElse(hostApi.string.of("—")),
          }),
        ],
      ),
  )
}
```

---

## Cross-references

- [`box.md`](./box.md) — `Child`; `SizeConstraint`; `ChildSize`; conditional rendering via exclusion
- [`number-value.md`](./number-value.md) — equivalent component for `NumberMap` values
- [`ui-state.md`](./ui-state.md) — UI state values used for bindings
- [`stringExpression.md`](../expressions/stringExpression.md) — `StringExpression` used for `value`
- [`maybeExpression.md`](../expressions/maybeExpression.md) — narrowing slot values before use
- [`entities.md`](../data-model/entities.md) — `TextMap` on Entity
