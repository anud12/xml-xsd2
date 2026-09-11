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

  // node-5 starts at the origin; the odd-angle test walks it to (10,3) —
  // atan2(3,10) ≈ 16.7°, neither 45° nor axis-aligned.
  hostApi.runtime.registerEntity({
    id: "node-5",
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  hostApi.runtime.registerContainer({
    id: "grid-1",
    entities: [
      hostApi.runtime.string.of("node-1"),
      hostApi.runtime.string.of("node-2"),
      hostApi.runtime.string.of("node-3"),
      hostApi.runtime.string.of("node-4"),
      hostApi.runtime.string.of("node-5"),
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

  // A high-speed diagonal move: (0,0) -> (10,10) at speed 10. Without the
  // remaining-path clamp the final tick overshoots the destination and the
  // move never lands; with it the actor stops exactly at (10,10).
  hostApi.runtime.registerAction({
    name: string.of("blitz-node-1"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 10,
        y: 10,
        speed: 10,
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

  // An odd-angle move: (0,0) -> (10,3) at speed 1. The straight-line
  // direction is pooled per tick (Q16.16 fixed-point), so x and y each advance
  // whole units only when their fraction has accumulated to 1 — the actor
  // walks a straight ~16.7° line, not a 45°-then-axis staircase, and lands
  // on the target exactly.
  hostApi.runtime.registerAction({
    name: string.of("march-node-5-odd-angle"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-5"),
        x: 10,
        y: 3,
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

  // Readouts bind each mover's column/row number values: the labels
  // re-resolve from the entity store every frame, so they follow the moves.
  hostApi.ui.panel("col-1", {
    width: 80,
    height: 40,
    x: 10,
    y: 10,
  }, [
    hostApi.ui.field("col-1-value", { entity: "node-1", map: "number", name: "column", fallback: "0" }),
  ]);

  hostApi.ui.panel("row-1", {
    width: 80,
    height: 40,
    x: 100,
    y: 10,
  }, [
    hostApi.ui.field("row-1-value", { entity: "node-1", map: "number", name: "row", fallback: "0" }),
  ]);

  hostApi.ui.panel("col-3", {
    width: 80,
    height: 40,
    x: 190,
    y: 10,
  }, [
    hostApi.ui.field("col-3-value", { entity: "node-3", map: "number", name: "column", fallback: "0" }),
  ]);

  hostApi.ui.panel("row-3", {
    width: 80,
    height: 40,
    x: 280,
    y: 10,
  }, [
    hostApi.ui.field("row-3-value", { entity: "node-3", map: "number", name: "row", fallback: "0" }),
  ]);

  hostApi.ui.panel("col-4", {
    width: 80,
    height: 40,
    x: 370,
    y: 10,
  }, [
    hostApi.ui.field("col-4-value", { entity: "node-4", map: "number", name: "column", fallback: "0" }),
  ]);

  hostApi.ui.panel("row-4", {
    width: 80,
    height: 40,
    x: 460,
    y: 10,
  }, [
    hostApi.ui.field("row-4-value", { entity: "node-4", map: "number", name: "row", fallback: "0" }),
  ]);

  hostApi.ui.panel("col-5", {
    width: 80,
    height: 40,
    x: 550,
    y: 10,
  }, [
    hostApi.ui.field("col-5-value", { entity: "node-5", map: "number", name: "column", fallback: "0" }),
  ]);

  hostApi.ui.panel("row-5", {
    width: 80,
    height: 40,
    x: 640,
    y: 10,
  }, [
    hostApi.ui.field("row-5-value", { entity: "node-5", map: "number", name: "row", fallback: "0" }),
  ]);
}
