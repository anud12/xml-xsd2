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
  // ctx.moveTo advances one cell per tick toward the target.
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

  // Readouts bind node-1's column/row number values.
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

  // The 700x500 view of the 10x5 container: the cursor cell under a click
  // resolves from the container's sizeX/sizeY, and the click routes the move.
  hostApi.ui.window("plane-host", {
    panel: {
      x: 10,
      y: 80,
      width: 700,
      height: 500,
    },
  }, [
    hostApi.ui.groupView("plane", {
      group: "grid-1", list: "members",
      panel: {
        width: 700,
        height: 500,
        onClick: (ctx) => {
          ctx.emitAction("move-to-cursor", {
            x: ctx.cursor.getX(),
            y: ctx.cursor.getY(),
          });
        },
      },
    }, entity => hostApi.ui.window(entity.id, {}, [
      hostApi.ui.window(entity.id + "-marker", {
        panel: {
          width: 10,
          height: 10,
        },
      }),
    ]))
  ]);
}