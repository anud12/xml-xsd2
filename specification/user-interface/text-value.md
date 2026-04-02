# TextValue

A `TextValue` displays a `StringExpression` (typically from entity `TextMap` via UI state). Leaf component with no children.

```ts
hostApi.ui.text(id, options)
```

See [`concepts.md`](./concepts.md) for shared concepts: **component identity** and **size constraints**.

---

## Styling & Overflow

TextValue can declare an optional font for rendering:

- **`font`** — Font name resolved by client at render time. If omitted, uses platform default.

If text exceeds the component's `width.max` constraint, it is **truncated silently** (no ellipsis marker). Text is vertically centered within the component's height.

---

## Type

```ts
type TextOptions = {
  value: StringExpression;       // String to display (literals, entity lookups, expressions)
  size?: ChildSize;              // Optional size hint for parent layout
  font?: FontResource;           // Optional font; defaults to platform default if omitted
};
```

---

## Examples

**Literal text:**
```ts
hostApi.ui.text("label", { value: string.of("Health") })
```

**Entity TextMap binding with font:**
```ts
hostApi.ui.text("name", {
  value: state.actor.textMap.get("name"),
  font: { name: string.of("body-font") }
})
```

**State value with fallback:**
```ts
const selection = hostApi.ui.state.declare("selection")
hostApi.ui.text("selected-name", {
  value: selection.asEntity
    .map(e => e.textMap.get("name"))
    .orElse(string.of("—")),
  font: { name: string.of("title-font") }
})
```

**In a label|value grid:**
```ts
hostApi.ui.box("grid", {
  layout: { columns: [{ min: 80 }, { scale: 1 }] }
}, (state, data) => [
  text("actor-name-label", {
    value: string.of("Name"),
    font: { name: string.of("label-font") }
  }),
  text("actor-name", {
    value: state.actor.textMap.get("name"),
    font: { name: string.of("body-font") }
  }),
])
```

---

## Cross-references

- [`concepts.md`](./concepts.md) — Identity, sizing, state binding
- [`box.md`](./box.md) — Component tree structure
- [`rendering.md`](./rendering.md) — Resource resolution, overflow, truncation
- [`stringExpression.md`](../expressions/stringExpression.md) — `StringExpression` values
