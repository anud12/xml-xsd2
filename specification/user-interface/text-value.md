# TextValue

A `TextValue` displays a `StringExpression` (typically from entity `TextMap` via UI state). Leaf component with no children.

```ts
hostApi.ui.text(id, options)
```

See [`concepts.md`](./concepts.md) for shared concepts: **component identity** and **size constraints**.

---

## Type

```ts
type TextOptions = {
  value: StringExpression;  // String to display (literals, entity lookups, expressions)
  size?: ChildSize;         // Optional size hint for parent layout
};
```

---

## Examples

**Literal text:**
```ts
hostApi.ui.text("label", { value: string.of("Health") })
```

**Entity TextMap binding:**
```ts
hostApi.ui.text("name", { value: state.actor.textMap.get("name") })
```

**State value with fallback:**
```ts
const selection = hostApi.ui.state.declare("selection")
hostApi.ui.text("selected-name", {
  value: selection.asEntity
    .map(e => e.textMap.get("name"))
    .orElse(string.of("—"))
})
```

**In a label|value grid:**
```ts
hostApi.ui.box("grid", {
  layout: { columns: [{ min: 80 }, { scale: 1 }] }
}, (state, data) => [
  text("actor-name-label", { value: string.of("Name") }),
  text("actor-name", { value: state.actor.textMap.get("name") }),
])
```

---

## Cross-references

- [`concepts.md`](./concepts.md) — Identity, sizing, state binding
- [`box.md`](./box.md) — Component tree structure
- [`stringExpression.md`](../expressions/stringExpression.md) — `StringExpression` values
