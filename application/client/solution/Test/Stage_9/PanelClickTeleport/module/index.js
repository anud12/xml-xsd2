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

  // The click handler forwards the cursor cell as the teleport destination.
  hostApi.runtime.registerAction({
    name: string.of("teleport-to-cursor"),
    apply: (ctx) => {
      ctx.teleportTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: ctx.args.x,
        y: ctx.args.y,
        clamp: true,
      });
    },
  });

  // The panel represents the grid-1 container: the cursor cell under a click
  // resolves from the container's sizeX/sizeY, not the layout tracks.
  hostApi.ui.panel("board", {
    width: 300,
    height: 300,
    container: string.of("grid-1"),
    layout: {
      columns: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
      rows: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
    },
    onClick: (ctx) => {
      ctx.emitAction("teleport-to-cursor", {
        x: ctx.cursor.getX(),
        y: ctx.cursor.getY(),
      });
    },
  }, [])
}
