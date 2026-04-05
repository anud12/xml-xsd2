# Entity Actions

Entity actions target a single entity and are the most common action type.

---

## Registration

```ts
hostApi.registerEntityAction: (args: RegisterEntityActionArgs) => void

type RegisterEntityActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: { type: "entity"; id: UniqueGlobalEntityId }) => ConditionExpression;
  cooldown?: (context: ActionContext, target: { type: "entity"; id: UniqueGlobalEntityId }) => TemporalExpression;
  apply: (context: ActionContext, target: { type: "entity"; id: UniqueGlobalEntityId }) => void;
}
```

### Parameters

- **name** — Unique action name (required). Used to look up the action in the wire message.
- **description** — Optional human-readable description for documentation.
- **cooldownGroup** — Optional cooldown group name. If omitted, defaults to `name`. Actions with the same cooldownGroup share a cooldown timer.
- **guard** — Optional eligibility check. Receives `ActionContext` and the target entity's ID. Evaluated before `cooldown`. Must return a `ConditionExpression`.
- **cooldown** — Optional rate limiter. Receives `ActionContext` and the target entity's ID. Must return a `TemporalExpression` (duration until the next action can be sent).
- **apply** — Required side-effect function. Receives `ActionContext` and the target entity's ID. Should emit events to trigger Effects.

---

## Wire Message

```ts
socket.send({
  actionName: "pickUp",
  actorEntityId: playerEntityId,
  target: { type: "entity", entityId: targetEntityId }
});
```

---

## Examples

### No-input action — rest (simplest case)

Actions don't always need complex target handling. Some actions primarily affect the actor:

```ts
hostApi.registerEntityAction({
  name: "rest",
  cooldown: (_ctx, _target) => hostApi.temporal.seconds(hostApi.number.of(5)),
  apply: (ctx, target) => {
    // Emit an event; target is the entity being acted upon
    ctx.emitEvent("restActor", { actorId: ctx.actor.id });
  }
});
```

Wire message (target is still required, but doesn't affect the action):

```ts
socket.send({
  actionName: "rest",
  actorEntityId: playerEntityId,
  target: { type: "entity", entityId: playerEntityId }  // can be any entity
});
```

### Simple action — pick up an entity

```ts
hostApi.registerEntityAction({
  name: "pickUp",
  guard: (ctx, target) => ctx.actor.hasClassification(hostApi.string.of("player")),
  cooldown: (_ctx, _target) => hostApi.temporal.seconds(hostApi.number.of(1)),
  apply: (ctx, target) => {
    ctx.emitEvent("transferEntity", { entityId: target.id });
  }
});
```

### Grouped cooldown — two actions share one timer

```ts
hostApi.registerEntityAction({
  name: "attack",
  cooldownGroup: "melee",
  cooldown: (_ctx, _target) => hostApi.temporal.seconds(hostApi.number.of(1)),
  apply: (ctx, target) => {
    ctx.emitEvent("meleeAttack", { targetId: target.id });
  }
});

hostApi.registerEntityAction({
  name: "heavyAttack",
  cooldownGroup: "melee",
  cooldown: (_ctx, _target) => hostApi.temporal.seconds(hostApi.number.of(2)),
  apply: (ctx, target) => {
    ctx.emitEvent("heavyMeleeAttack", { targetId: target.id });
  }
});
```

Both `attack` and `heavyAttack` share the "melee" cooldown timer. Once either action is used, both become unavailable for the cooldown duration.

### Conditional action — attack only enemy units

```ts
hostApi.registerEntityAction({
  name: "dealDamage",
  guard: (ctx, target) => {
    const targetEntity = ctx.actor.getRelated("nearbyEntities")
      .find(e => e.id === target.id);
    return targetEntity && targetEntity.hasClassification(hostApi.string.of("enemy"));
  },
  apply: (ctx, target) => {
    ctx.emitEvent("inflictDamage", {
      targetEntityId: target.id,
      damageAmount: hostApi.number.of(10),
    });
  }
});
```

### Bulk action — client-side batching

To perform an action on multiple entities, the client sends multiple messages:

```ts
for (const targetId of selectedEnemyIds) {
  socket.send({
    actionName: "dealDamage",
    actorEntityId: playerEntityId,
    target: { type: "entity", entityId: targetId }
  });
}
```

Each action message is processed independently. If one fails (e.g., guard returns false), others still proceed.

### Bulk action — server-side event chaining

Alternatively, emit multiple events from a single action:

```ts
hostApi.registerEntityAction({
  name: "areaAttack",
  apply: (ctx, target) => {
    // target is the action origin (e.g., the caster)
    const nearbyEnemies = ctx.actor.getComponentValue("enemies");
    nearbyEnemies.forEach(enemyId => {
      ctx.emitEvent("inflictDamage", {
        targetEntityId: enemyId,
        damageAmount: hostApi.number.of(10),
      });
    });
  }
});
```

The client sends one message; the action emits events for all affected entities.

---

## Cross-References

- [`actions.md`](./actions.md) — Common concepts, wire protocol, runtime flow
- [`effects.md`](./effects.md) — Event system and Effect chaining
- [`conditionExpression.md`](../expressions/conditionExpression.md) — guard expression primitives
