# UI State

Per-client, UI-only API for interactive state. Modules use this to bind UI components to per-client values. Scoped under `hostApi.ui.state`; not available outside UI declarations.

State values are evaluated per-client at render time — each client maintains independent state.

See [`concepts.md`](./concepts.md) for shared concepts: **state binding** patterns.

---

## Built-in Value

**`actor`** — The entity owned by the authenticated client. Always present; never declarable.

```ts
hostApi.ui.state.actor   // → EntityExpression
```

Use in bindings directly:
```ts
hostApi.ui.text("name", { value: hostApi.ui.state.actor.textMap.get("name") })
```

---

## Declared Values

Module-declared values are named slots that hold `EntityExpression`, `ContainerExpression`, or are absent. Declare at load time:

```ts
const selection = hostApi.ui.state.declare("selection")
const hover = hostApi.ui.state.declare("hover")
```

Use narrowing accessors to work safely with the value:
```ts
selection.asEntity           // Resolves to EntityExpression or None
selection.asContainer        // Resolves to ContainerExpression or None
selection.isPresent          // Resolves to true/false
```

Binding example:
```ts
hostApi.ui.text("selection-name", {
  value: selection.asEntity
    .map(e => e.textMap.get("name"))
    .orElse(string.of("No selection"))
})
```

---

## UI Actions

Modules can register UI-only actions that mutate per-client state:

```ts
hostApi.ui.action.register({
  name: "on-select",
  effect: { type: "set-value", value: "selection" }
})
```

State mutations are local to the client and are never broadcast to server or other clients.

---

## API

**UiStateApi** — Available in UI callbacks:

```ts
hostApi.ui.state.actor                    // EntityExpression for authenticated client
hostApi.ui.state.declare(name)            // Create/retrieve a named state value
hostApi.ui.state.value(name)              // Reference a value declared by another module
```

See `specification/types/user-interface/ui-state.ts` for full type definitions.

---

## Cross-references

- [`concepts.md`](./concepts.md) — State binding patterns
- [`panel.md`](./panel.md) — Panel callback receives state proxy
- [`box.md`](./box.md) — Box children callback receives state proxy
- [`text-value.md`](./text-value.md) — Example bindings to state values
- [`entities.md`](../data-model/entities.md) — Entity data model with `TextMap`, `NumberMap`
