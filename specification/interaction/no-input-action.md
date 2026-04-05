# No-Input Actions

No-input actions are simple actor-only operations that don't interact with a specific target. They're ideal for actions like rest, meditate, or idle — actions that only affect the actor.

---

## Registration

```ts
hostApi.registerAction: (args: RegisterActionArgs) => void

type RegisterActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionExpression;
  cooldown?: (context: ActionContext) => TemporalExpression;
  apply: (context: ActionContext) => void;
}
```

### Parameters

- **name** — Unique action name (required). Used to look up the action in the wire message.
- **description** — Optional human-readable description for documentation.
- **cooldownGroup** — Optional cooldown group name. If omitted, defaults to `name`. Actions with the same cooldownGroup share a cooldown timer.
- **guard** — Optional eligibility check. Receives `ActionContext` only (no target). Must return a `ConditionExpression`.
- **cooldown** — Optional rate limiter. Receives `ActionContext` only. Must return a `TemporalExpression` (duration until the next action can be sent).
- **apply** — Required side-effect function. Receives `ActionContext` only. Should emit events to trigger Effects.

### Key Difference from Targeted Actions

- **No target parameter** in callbacks (guard, cooldown, apply)
- **Wire message omits target field** entirely
- **Simpler runtime flow** — skips target validation step
- **Faster execution** — fewer validation steps

---

## Wire Message

```ts
socket.send({
  actionName: "rest",
  actorEntityId: playerEntityId
  // target field is omitted
});
```

No target field is sent for no-input actions.

---

## Examples

### Simple action — rest

```ts
hostApi.registerAction({
  name: "rest",
  cooldown: (_ctx) => hostApi.temporal.seconds(hostApi.number.of(5)),
  apply: (ctx) => {
    ctx.emitEvent("restActor", { actorId: ctx.actor.id });
  }
});
```

The client sends:

```ts
socket.send({
  actionName: "rest",
  actorEntityId: playerEntityId
});
```

### Conditional action — meditate (only if calm)

```ts
hostApi.registerAction({
  name: "meditate",
  guard: (ctx) => {
    const stress = ctx.actor.getComponentValue("stress");
    return stress < 50;
  },
  cooldown: (_ctx) => hostApi.temporal.seconds(hostApi.number.of(3)),
  apply: (ctx) => {
    ctx.emitEvent("meditateStart", {
      actorId: ctx.actor.id,
    });
  }
});
```

### Grouped cooldown — two actions share one timer

```ts
hostApi.registerAction({
  name: "dodge",
  cooldownGroup: "mobility",
  cooldown: (_ctx) => hostApi.temporal.seconds(hostApi.number.of(1)),
  apply: (ctx) => {
    ctx.emitEvent("executeEvasion", { actorId: ctx.actor.id });
  }
});

hostApi.registerAction({
  name: "dash",
  cooldownGroup: "mobility",
  cooldown: (_ctx) => hostApi.temporal.seconds(hostApi.number.of(1)),
  apply: (ctx) => {
    ctx.emitEvent("executeDash", { actorId: ctx.actor.id });
  }
});
```

Both `dodge` and `dash` share the "mobility" cooldown timer.

### Chained events — complex internal effect

```ts
hostApi.registerAction({
  name: "channelPower",
  apply: (ctx) => {
    // Multiple events in sequence, all within one action
    ctx.emitEvent("startChanneling", { actorId: ctx.actor.id });
    
    const result = ctx.emitEvent("gatherMana", {
      actorId: ctx.actor.id,
      intensity: hostApi.number.of(10),
    });
    
    ctx.emitEvent("releasePower", {
      actorId: ctx.actor.id,
      energy: result.gatheredEnergy,
    });
  }
});
```

---

## When to Use No-Input Actions

- **Self-focused effects**: rest, meditate, meditate, cast (self-cast), channel
- **Simple state changes**: idle, crouch, stand, swim
- **Toggle actions**: toggle shield, enable/disable mode
- **Actor-only operations**: anything that doesn't target something else

---

## When to Use Targeted Actions Instead

If the action interacts with:
- A specific entity → use [`registerEntityAction`](./entity-action.md)
- A specific container → use [`registerContainerAction`](./container-action.md)
- A specific coordinate → use [`registerPointAction`](./point-action.md)

---

## Cross-References

- [`actions.md`](./actions.md) — Common concepts, wire protocol, runtime flow
- [`effects.md`](./effects.md) — Event system and Effect chaining
- [`conditionExpression.md`](../expressions/conditionExpression.md) — guard expression primitives
