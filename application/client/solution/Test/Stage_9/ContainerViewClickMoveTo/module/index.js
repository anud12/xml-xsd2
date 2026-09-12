export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "node-1",
    numberMap: {
      column: number.of(2),
      row: number.of(1),
    },
  });

  hostApi.runtime.registerContainer({
    id: "grid-1",
    entities: [
      hostApi.runtime.string.of("node-1"),
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
      value: number.of(5),
      outOfBounds: "clamp",
    },
  });

  // The click handler forwards the cursor cell as the move destination.
  // ctx.moveTo advances one cell per tick toward the target.
  hostApi.runtime.registerAction({
    name: string.of("move-to-cursor"),
    apply: (ctx) => {
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: ctx.args.x,
        y: ctx.args.y,
        speed: 1,
      });
    },
  });

  // Readouts bind node-1's column/row number values.
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

  // The 700x500 view of the 10x5 container: the cursor cell under a click
  // resolves from the container's sizeX/sizeY, and the click routes the move.
  hostApi.ui.containerView("plane", {
    container: "grid-1",
    width: 700,
    height: 500,
    x: 10,
    y: 80,
    onClick: (ctx) => {
      ctx.emitAction("move-to-cursor", {
        x: ctx.cursor.getX(),
        y: ctx.cursor.getY(),
      });
    },
  }, entity => hostApi.ui.window(entity.id, {}, [
    hostApi.ui.panel(entity.id + "-marker", {
      width: 10,
      height: 10,
    }),
  ]))
}
