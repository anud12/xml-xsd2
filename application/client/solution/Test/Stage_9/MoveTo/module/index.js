export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "node-1",
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  hostApi.runtime.registerEntity({
    id: "node-2",
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  // node-3 starts to the east; the negative-x test walks it back to the origin.
  hostApi.runtime.registerEntity({
    id: "node-3",
    numberMap: {
      column: number.of(5),
      row: number.of(0),
    },
  });

  // node-4 starts in the first quadrant; the negative diagonal test walks it
  // toward the origin (negative x AND negative y).
  hostApi.runtime.registerEntity({
    id: "node-4",
    numberMap: {
      column: number.of(5),
      row: number.of(3),
    },
  });

  hostApi.runtime.registerContainer({
    id: "grid-1",
    entities: [
      hostApi.runtime.string.of("node-1"),
      hostApi.runtime.string.of("node-2"),
      hostApi.runtime.string.of("node-3"),
      hostApi.runtime.string.of("node-4"),
    ],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getSpanX: (entity) => number.of(1),
    getSpanY: (entity) => number.of(1),
    sizeX: {
      value: number.of(10),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(10),
      outOfBounds: "clamp",
    },
  });

  // A straight-line move at speed 1: advances one cell per tick, interruptible
  // by default (no denyInterrupt).
  hostApi.runtime.registerAction({
    name: string.of("march-node-1"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 5,
        y: 0,
        speed: 1,
      });
    },
  });

  // A fast move at speed 3: covers three cells per tick.
  hostApi.runtime.registerAction({
    name: string.of("dash-node-1"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 10,
        y: 0,
        speed: 3,
      });
    },
  });

  // A move whose target exceeds the container size: walks to the bound edge
  // and stops there ("try, then stop").
  hostApi.runtime.registerAction({
    name: string.of("march-node-1-out-of-bounds"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 20,
        y: 0,
        speed: 1,
      });
    },
  });

  // A move in the negative-x direction: walks from (5,0) back toward (0,0).
  hostApi.runtime.registerAction({
    name: string.of("march-node-3-west"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-3"),
        x: 0,
        y: 0,
        speed: 1,
      });
    },
  });

  // A diagonal move toward the origin in negative x AND negative y.
  hostApi.runtime.registerAction({
    name: string.of("march-node-4-to-origin"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-4"),
        x: 0,
        y: 0,
        speed: 1,
      });
    },
  });

  // A non-interruptible move: a new action for the actor is rejected mid-move.
  hostApi.runtime.registerAction({
    name: string.of("hold-march-node-1"),
    apply: (ctx) => {
      ctx.denyInterrupt();
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 5,
        y: 0,
        speed: 1,
      });
    },
  });

  // An instant move (speed large enough to finish in one tick) used to verify
  // interruption mid-move of an interruptible plan.
  hostApi.runtime.registerAction({
    name: string.of("relocate-node-1"),
    apply: (ctx) => {
      ctx.teleportTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 6,
        y: 3,
        clamp: true,
      });
    },
  });
}
