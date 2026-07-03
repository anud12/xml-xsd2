# Container Actions

Container actions target a single container (such as chests, rooms, or zones).

---

## Registration

```ts
hostApi.registerContainerAction: (args: RegisterContainerActionArgs) => void

type RegisterContainerActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: { type: "container"; id: UniqueGlobalContainerId }) => ConditionExpression;
  cooldown?: (context: ActionContext, target: { type: "container"; id: UniqueGlobalContainerId }) => TemporalExpression;
  apply: (context: ActionContext, target: { type: "container"; id: UniqueGlobalContainerId }) => void;
}
```

### Parameters

- **name** — Unique action name (required). Used to look up the action in the wire message.
- **description** — Optional human-readable description for documentation.
- **cooldownGroup** — Optional cooldown group name. If omitted, defaults to `name`. Actions with the same cooldownGroup share a cooldown timer.
- **guard** — Optional eligibility check. Receives `ActionContext` and the target container's ID. Evaluated before `cooldown`. Must return a `ConditionExpression`.
- **cooldown** — Optional rate limiter. Receives `ActionContext` and the target container's ID. Must return a `TemporalExpression` (duration until the next action can be sent).
- **apply** — Required side-effect function. Receives `ActionContext` and the target container's ID. Should emit events to trigger Effects.

---

## Wire Message

```ts
socket.send({
  actionName: "loot",
  actorEntityId: playerEntityId,
  target: { type: "container", containerId: chestContainerId }
});
```

---

## Examples

### No-input action — inspect surroundings

Some actions use the container just as a context reference:

```ts
hostApi.registerContainerAction({
  name: "inspect",
  apply: (ctx, target) => {
    ctx.emitEvent("inspectContainer", {
      containerId: target.id,
      inspectorId: ctx.actor.id,
    });
  }
});
```

### Simple action — open and loot a container

```ts
hostApi.registerContainerAction({
  name: "loot",
  apply: (ctx, target) => {
    ctx.emitEvent("openContainer", { containerId: target.id });
    ctx.emitEvent("resolveAndSpawnLoot", { containerId: target.id });
  }
});
```

### Chained events — result from one event used by another

```ts
hostApi.registerContainerAction({
  name: "advancedLoot",
  apply: (ctx, target) => {
    // First event resolves loot table and returns the result
    const lootResult = ctx.emitEvent("resolveLootTable", {
      containerId: target.id,
      rarity: hostApi.string.of("epic"),
    });
    
    // Second event uses the resolved items from the first
    ctx.emitEvent("spawnLootItems", {
      containerId: target.id,
      items: lootResult.resolvedItems,
    });
  }
});
```

Multiple events in a chain can reference results from previous events, allowing complex logic within a single action.

### Conditional action — only loot if unlocked

```ts
hostApi.registerContainerAction({
  name: "safeLoot",
  guard: (ctx, target) => {
    const container = ctx.actor.getComponentValue("containers")
      .find(c => c.id === target.id);
    return container && !container.isLocked;
  },
  apply: (ctx, target) => {
    ctx.emitEvent("loot", { containerId: target.id });
  }
});
```

### Zone action — apply effect to entire room

```ts
hostApi.registerContainerAction({
  name: "fillRoomWithFire",
  description: "Fill a room container with fire, damaging all entities within",
  apply: (ctx, target) => {
    // Emit a single event for the room
    ctx.emitEvent("createFireZone", {
      containerId: target.id,
      intensity: hostApi.number.of(50),
    });
  }
});
```

---

## Cross-References

- [`actions.md`](./actions.md) — Common concepts, wire protocol, runtime flow
- [`effects.md`](./effects.md) — Event system and Effect chaining
- [`containers.md`](../data-model/containers.md) — Container model and size system
- [`conditionExpression.md`](../expressions/conditionExpression.md) — guard expression primitives
