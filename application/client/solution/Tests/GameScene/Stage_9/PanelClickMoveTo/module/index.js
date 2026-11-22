export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
 const { number, string } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("node-1"),
    numberMap: { column: number.of(2), row: number.of(1) },
    stringMap: { xKey: "column", yKey: "row" },
  });

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("grid-1"),
    entityListMap: { members: [
      hostApi.runtime.string.of("node-1"),
    ] },
    numberMap: { sizeX: number.of(10), sizeY: number.of(5) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  // The click handler forwards the cursor cell as the move destination.
  // Unlike teleport, ctx.moveTo advances one cell per tick toward the target.
  hostApi.runtime.setAction({
    name: string.of("move-to-cursor"),
    apply: (ctx) => {
      ctx.moveTo({
        groupId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: ctx.args.x,
        y: ctx.args.y,
        speed: 1,
      });
    },
  });

  // Readouts bind node-1's column/row number values: the labels re-resolve
  // from the entity store every frame, so they follow the move.
  hostApi.ui.window("col", {
    panel: {
      width: 80,
      height: 40,
      x: 10,
      y: 10,
    },
  }, [
    hostApi.ui.field("col-value", { entity: "node-1", map: "number", name: "column", fallback: "0" }, font),
  ]);

  hostApi.ui.window("row", {
    panel: {
      width: 80,
      height: 40,
      x: 100,
      y: 10,
    },
  }, [
    hostApi.ui.field("row-value", { entity: "node-1", map: "number", name: "row", fallback: "0" }, font),
  ]);

  // The panel represents the grid-1 container: the cursor cell under a click
  // resolves from the container's sizeX/sizeY, not the layout tracks.
  hostApi.ui.window("board", {
    panel: {
      width: 300,
      height: 300,
      layout: {
        columns: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
        rows: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
        group: string.of("grid-1"),
      },
      onClick: (ctx) => {
        ctx.emitAction("move-to-cursor", {
          x: ctx.cursor.getX(),
          y: ctx.cursor.getY(),
        });
      },
    },
  }, [])
}