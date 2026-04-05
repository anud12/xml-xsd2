# Point Actions

Point actions target a specific coordinate within a container (such as positions on a map or grid).

---

## Registration

```ts
hostApi.registerPointAction: (args: RegisterPointActionArgs) => void

type RegisterPointActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: { type: "point"; containerId: UniqueGlobalContainerId; position: ContainerPoint }) => ConditionExpression;
  cooldown?: (context: ActionContext, target: { type: "point"; containerId: UniqueGlobalContainerId; position: ContainerPoint }) => TemporalExpression;
  apply: (context: ActionContext, target: { type: "point"; containerId: UniqueGlobalContainerId; position: ContainerPoint }) => void;
}

// 1D or 2D, matching the target container's declared dimensions
type ContainerPoint =
  | { dimension1: NumberExpression }
  | { dimension1: NumberExpression; dimension2: NumberExpression }
```

### Parameters

- **name** — Unique action name (required). Used to look up the action in the wire message.
- **description** — Optional human-readable description for documentation.
- **cooldownGroup** — Optional cooldown group name. If omitted, defaults to `name`. Actions with the same cooldownGroup share a cooldown timer.
- **guard** — Optional eligibility check. Receives `ActionContext` and the target point (container ID + coordinates). Evaluated before `cooldown`. Must return a `ConditionExpression`.
- **cooldown** — Optional rate limiter. Receives `ActionContext` and the target point. Must return a `TemporalExpression` (duration until the next action can be sent).
- **apply** — Required side-effect function. Receives `ActionContext` and the target point. Should emit events to trigger Effects.

### Target Format

The target includes both the container and the position within it:

```ts
target = {
  type: "point",
  containerId: "container-12345",
  position: { dimension1: 42 }  // 1D coordinate
}

// or for 2D:
target = {
  type: "point",
  containerId: "container-12345",
  position: { dimension1: 10, dimension2: 20 }  // 2D coordinates
}
```

**Validation**: The runtime validates that the dimension count (1D or 2D) matches the container's declared dimension count at step [4] of the runtime flow.

---

## Wire Message

```ts
socket.send({
  actionName: "dig",
  actorEntityId: playerEntityId,
  target: { 
    type: "point",
    containerId: groundContainerId,
    position: { dimension1: 15, dimension2: 42 }
  }
});
```

---

## Examples

### No-input action — detect nearby threats

Even point actions can have minimal target usage:

```ts
hostApi.registerPointAction({
  name: "detectThreats",
  apply: (ctx, target) => {
    // Detect threats at this location; emit results
    ctx.emitEvent("scanArea", {
      containerId: target.containerId,
      center: target.position,
      radius: hostApi.number.of(5),
      scanType: hostApi.string.of("hostile"),
    });
  }
});
```

### Simple action — dig at a position

```ts
hostApi.registerPointAction({
  name: "dig",
  apply: (ctx, target) => {
    ctx.emitEvent("excavate", {
      containerId: target.containerId,
      position: target.position,
    });
  }
});
```

### 1D example — fishing at a location

```ts
hostApi.registerPointAction({
  name: "fish",
  description: "Cast a line into the water at a specific depth",
  guard: (ctx, target) => {
    // Ensure the position is a valid fishing spot
    return ctx.actor.isInWater(target.containerId, target.position);
  },
  cooldown: (_ctx, _target) => hostApi.temporal.seconds(hostApi.number.of(3)),
  apply: (ctx, target) => {
    ctx.emitEvent("castLine", {
      containerId: target.containerId,
      depth: target.position.dimension1,
    });
  }
});
```

### 2D example — place a spell on a grid

```ts
hostApi.registerPointAction({
  name: "placeWard",
  description: "Place a protective ward at a grid coordinate",
  guard: (ctx, target) => {
    // Check if the grid position is empty
    const occupant = ctx.actor.getGridOccupant(
      target.containerId,
      target.position.dimension1,
      target.position.dimension2
    );
    return !occupant;
  },
  apply: (ctx, target) => {
    ctx.emitEvent("placeWardAtCoords", {
      containerId: target.containerId,
      x: target.position.dimension1,
      y: target.position.dimension2,
      wardType: hostApi.string.of("protection"),
    });
  }
});
```

### Area effect — summon creatures at multiple points (client-side batching)

To summon creatures at multiple points, the client sends multiple point actions:

```ts
const summonPoints = [
  { dimension1: 10, dimension2: 10 },
  { dimension1: 15, dimension2: 10 },
  { dimension1: 10, dimension2: 15 },
];

for (const point of summonPoints) {
  socket.send({
    actionName: "summon",
    actorEntityId: casterEntityId,
    target: {
      type: "point",
      containerId: battlefieldId,
      position: point,
    }
  });
}
```

Each point action is processed independently.

### Chain events — complex ritual at a point

```ts
hostApi.registerPointAction({
  name: "ritualSummon",
  apply: (ctx, target) => {
    // Prepare the ground
    ctx.emitEvent("prepareRitualGround", {
      containerId: target.containerId,
      position: target.position,
    });
    
    // Draw the ritual circle
    const circleResult = ctx.emitEvent("drawRitualCircle", {
      containerId: target.containerId,
      centerPosition: target.position,
      radius: hostApi.number.of(3),
    });
    
    // Summon the entity (uses result from circle)
    ctx.emitEvent("summonWithinCircle", {
      circleId: circleResult.circleId,
      creatureType: hostApi.string.of("demon"),
    });
  }
});
```

---

## Dimension Matching

The runtime validates that point targets have the correct dimension count for their container:

- If the container declares `dimension1` only (1D), the position must have exactly `dimension1`.
- If the container declares `dimension1` and `dimension2` (2D), the position must have both fields.
- Mismatches are rejected at runtime step [4] with an error to the client.

---

## Cross-References

- [`actions.md`](./actions.md) — Common concepts, wire protocol, runtime flow
- [`effects.md`](./effects.md) — Event system and Effect chaining
- [`containers.md`](../data-model/containers.md) — Container model and dimension system
- [`conditionExpression.md`](../expressions/conditionExpression.md) — guard expression primitives
