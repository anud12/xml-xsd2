# UI State

The UI state is a **per-client, UI-only API** that modules use to bind UI components to named values holding the current client's interactive state. It is scoped under `hostApi.ui.state` and is not available outside UI declarations.

UI state values are evaluated **per-client** at render time — each connected client maintains its own independent values. They are never part of shared world state.

---

## Built-in value: `actor`

The only fixed value. Resolves to the entity owned by this client (the authenticated actor). Always present — never absent, never declarable.

```ts
hostApi.ui.state.actor   // → EntityExpression
```

---

## Declared values

All other values are **module-declared**. Modules call `declare` at load time to register a named value and receive an expression handle. Any number of values may be declared.

```ts
const selection  = hostApi.ui.state.declare("selection")
const hover      = hostApi.ui.state.declare("hover")
const inspecting = hostApi.ui.state.declare("inspecting")
```

Each value holds either an `EntityExpression`, a `ContainerExpression`, or is absent. Use the narrowing accessors to work with the current value.

---

## API

```ts
export type UIApi = {
  registerPanel:  (panel: PanelDeclaration) => void;
  action:         UIActionApi;
  state:          UiStateApi;
}

export type UiStateApi = {
  /**
   * The entity owned by this client (authenticated actor).
   * Always present — no narrowing needed.
   */
  actor: EntityExpression;

  /**
   * Declares a named UI state value at module load time.
   * Returns an expression handle usable in UI bindings.
   * Only one module should call declare for a given name;
   * others should use value() for cross-module access.
   */
  declare: (name: string) => UiValueExpression;

  /**
   * References a previously declared value by name.
   * Intended for cross-module access where another module owns the declaration.
   */
  value: (name: string) => UiValueExpression;
}

type UiValueExpression = {
  /**
   * Narrows to EntityExpression.
   * Resolves to None if the value holds a container or is absent.
   */
  asEntity: MaybeExpression<EntityExpression>;

  /**
   * Narrows to ContainerExpression.
   * Resolves to None if the value holds an entity or is absent.
   */
  asContainer: MaybeExpression<ContainerExpression>;

  /**
   * Evaluates to true if the value is currently populated (entity or container).
   */
  isPresent: ConditionExpression;
}
```

---

## Updating values — UI actions

Values are updated by UI actions. A `set-value` action binds a trigger (e.g. a click on a component) to a named value:

```ts
hostApi.ui.action.register({
  name: "selectEntity",
  effect: { type: "set-value", value: "selection" },
})

hostApi.ui.action.register({
  name: "clearSelection",
  effect: { type: "clear-value", value: "selection" },
})
```

When the action is triggered, the runtime places the interaction target into the named value (for `set-value`) or clears it (for `clear-value`).

---

## Usage patterns

### Actor — always an entity

```ts
// Actor's name
hostApi.ui.state.actor.textMap.get("name")     // → StringExpression

// Actor's HP
hostApi.ui.state.actor.numberMap.get("hp")     // → NumberExpression
```

### Declared value — narrow before use

```ts
const selection = hostApi.ui.state.declare("selection")

// Selected entity's name (None if value holds a container or is absent)
selection.asEntity.map(e => e.textMap.get("name"))  // → MaybeExpression<StringExpression>

// Selected container's label
selection.asContainer.map(c => c.textMap.get("label"))  // → MaybeExpression<StringExpression>

// Panel visible only when value is populated
hostApi.ui.registerPanel({
  id: "inspector",
  // ...
  visible: selection.isPresent,
  child: (state, data) => ({ /* ... */ }),
})
```

### Cross-module value reference

```ts
// Module B references a value declared by Module A
const selection = hostApi.ui.state.value("selection")
```

---

## Example — target frame panel

```ts
export default (hostApi) => {
  const target = hostApi.ui.state.declare("target")

  hostApi.ui.action.register({
    name: "setTarget",
    effect: { type: "set-value", value: "target" },
  })

  hostApi.ui.action.register({
    name: "clearTarget",
    effect: { type: "clear-value", value: "target" },
  })

  hostApi.ui.registerPanel({
    id: "target-frame",
    anchor: { x: hostApi.number.of(0.5), y: hostApi.number.of(0) },
    pivot:  { x: hostApi.number.of(0.5), y: hostApi.number.of(0) },
    offset: { x: hostApi.number.of(0),   y: hostApi.number.of(8)  },
    size:   { width: hostApi.number.of(200), height: hostApi.number.of(40) },
    visible: target.isPresent,
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
        { type: "text", value: hostApi.string.of("HP") },
        {
          type: "number",
          value: state.value("target")
            .asEntity
            .map(e => e.numberMap.get("hp"))
            .orElse(hostApi.number.of(0)),
        },
      ],
    }),
  })
}
```

---

## Cross-references

- [`overview.md`](./overview.md) — UI system entry point; UI actions
- [`panel.md`](./panel.md) — `render` fn receives `state` as first argument
- [`text-value.md`](./text-value.md) — `value` bound to state entity text
- [`number-value.md`](./number-value.md) — `value` bound to state entity numbers
- [`maybeExpression.md`](../expressions/maybeExpression.md) — `MaybeExpression` used for optional state values
- [`entities.md`](../data-model/entities.md) — `EntityExpression` — `textMap`, `numberMap`
- [`containers.md`](../data-model/containers.md) — `ContainerExpression`

