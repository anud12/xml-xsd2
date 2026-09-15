export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerAnimation(string.of("marker"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("marker.png") }],
    duration: number.of(1),
  });

  hostApi.runtime.setEntity(string.of("node-1"), {
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  // A 10x5 container: the 700x500 plane is split into 10 columns of 70 and 5
  // rows of 100, so each container cell maps to exactly one grid cell.
  hostApi.runtime.setContainer(string.of("grid-1"), {
    entities: [string.of("node-1")],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getSpanX: (entity) => number.of(1),
    getSpanY: (entity) => number.of(1),
    sizeX: {
      value: number.of(10),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(5),
      outOfBounds: "clamp",
    },
  });

  // A speed-1 move toward (3,2): advances one cell per tick along a diagonal.
  // The shorter axis (y) settles at 2 and holds; the move keeps going until x
  // also reaches its target — the destination (3,2) — at which point it stops.
  hostApi.runtime.registerAction({
    name: string.of("march-node-1"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 3,
        y: 2,
        speed: 1,
      });
    },
  });

  // Readouts bind node-1's column/row so the rendered labels follow the move.
  hostApi.ui.panel("col", {
    width: 80,
    height: 40,
    x: 10,
    y: 10,
  }, [
    hostApi.ui.field("col-value", { entity: "node-1", map: "number", name: "column", fallback: "0" }),
  ]);

  hostApi.ui.panel("row", {
    width: 80,
    height: 40,
    x: 100,
    y: 10,
  }, [
    hostApi.ui.field("row-value", { entity: "node-1", map: "number", name: "row", fallback: "0" }),
  ]);

  // The 700x500 view of the 10x5 container: each container cell is 70x100, and
  // the view places the entity panel exactly on its cell, re-resolved each tick
  // so the panel tracks the move.
  hostApi.ui.containerView("plane", {
    container: "grid-1",
    width: 700,
    height: 500,
    x: 10,
    y: 80,
  }, entity => hostApi.ui.window(entity.id, {}, [
    hostApi.ui.panel(entity.id + "-marker", {
      background: hostApi.ui.getAnimation(string.of("marker")),
    }),
  ]))
}
