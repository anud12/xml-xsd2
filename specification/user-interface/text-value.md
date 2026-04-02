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
  font?: FontResource;      // Optional font; platform default if omitted (see rendering.md)
};
```

Text exceeding `width.max` is **truncated silently** (no ellipsis). Text is vertically centered within the component's height.

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

---

## Cross-references

- [`concepts.md`](./concepts.md) — Identity, sizing, state binding
- [`box.md`](./box.md) — Component tree structure
- [`rendering.md`](./rendering.md) — Resource resolution, overflow, truncation
- [`stringExpression.md`](../expressions/stringExpression.md) — `StringExpression` values
