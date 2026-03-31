# TextValue

A `TextValue` is a **leaf component** that displays a `StringExpression` — typically bound to an entity's `TextMap` value via UI state. It has no children.

---

## Declaration shape

```ts
type TextValueDeclaration = {
  type: "text";

  /** Optional identifier. */
  id?: string;

  /**
   * This component's own size hint. Used by the parent layout to allocate space.
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
{ type: "text", value: hostApi.string.of("Health") }
```

### Actor TextMap binding

```ts
{ type: "text", value: hostApi.ui.state.actor.textMap.get("name") }
```

### Declared value binding (with fallback)

```ts
const selection = hostApi.ui.state.declare("selection")

{
  type: "text",
  value: selection.asEntity
    .map(e => e.textMap.get("name"))
    .orElse(hostApi.string.of("—")),
}
```

---

## Example — name plate panel

```ts
hostApi.ui.registerPanel({
  id: "nameplate",
  anchor: { x: hostApi.number.of(0.5), y: hostApi.number.of(1) },
  pivot:  { x: hostApi.number.of(0.5), y: hostApi.number.of(1) },
  offset: { x: hostApi.number.of(0),   y: hostApi.number.of(-16) },
  size:   { width: hostApi.number.of(200), height: hostApi.number.of(40) },
  visible: selection.isPresent,
  child: (state, data) => ({
    type: "box",
    layout: { columns: [{ scale: hostApi.number.of(1), align: "center" }] },
    children: [
      {
        type: "text",
        value: state.value("selection").asEntity
          .map(e => e.textMap.get("name"))
          .orElse(hostApi.string.of("—")),
      },
    ],
  }),
})
```

---

## Cross-references

- [`box.md`](./box.md) — `Child` union; `SizeConstraint`; `ChildSize`; conditional rendering via exclusion
- [`number-value.md`](./number-value.md) — equivalent component for `NumberMap` values
- [`ui-state.md`](./ui-state.md) — UI state values used for bindings
- [`stringExpression.md`](../expressions/stringExpression.md) — `StringExpression` used for `value`
- [`maybeExpression.md`](../expressions/maybeExpression.md) — narrowing slot values before use
- [`conditionExpression.md`](../expressions/conditionExpression.md) — `ConditionExpression` for visibility
- [`entities.md`](../data-model/entities.md) — `TextMap` on Entity
