export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("node-1"),
    numberMap: { column: number.of(0), row: number.of(0) },
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

  // A 400x200 view of the 10x5 container (cell pitch 40x40) that is user-
  // resizable. The view's marker geometry is fixed at the declared 400x200
  // (the runtime position pass uses the declared viewWidth/viewHeight, which
  // never change), so a click must resolve its cell against that declared
  // extent, not the live (resized) size.
  hostApi.ui.window("plane-host", {
    panel: {
      x: 10,
      y: 10,
      width: 400,
      height: 200,
      resizable: { horizontal: true, vertical: true },
    },
  }, [
    hostApi.ui.groupView("plane", {
      group: "grid-1", list: "members",
      panel: {
        width: 400,
        height: 200,
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
