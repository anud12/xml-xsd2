export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "node-1",
    numberMap: {
      column: number.of(0),
      row: number.of(0),
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

  // A 400x200 view of the 10x5 container (cell pitch 40x40) that is user-
  // resizable. The view's marker geometry is fixed at the declared 400x200
  // (the runtime position pass uses the declared viewWidth/viewHeight, which
  // never change), so a click must resolve its cell against that declared
  // extent, not the live (resized) size.
  hostApi.ui.containerView("plane", {
    container: "grid-1",
    width: 400,
    height: 200,
    x: 10,
    y: 10,
    resizable: true,
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
