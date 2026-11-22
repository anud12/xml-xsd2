/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  const node_1 = hostApi.runtime.setEntity({
    id: string.of("node-1"),
    numberMap: { column: number.of(0), row: number.of(0) },
    stringMap: { xKey: "column", yKey: "row" },
  });

  const grid_1 = hostApi.runtime.setEntity({
    id: string.of("grid-1"),
    entityListMap: { members: [node_1] },
    numberMap: { sizeX: number.of(3), sizeY: number.of(3) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  const action = hostApi.runtime.setAction({
    name: string.of("move"),
    apply: (ctx) => {
      hostApi.runtime.log(
        "___move fired x=" + ctx.args.x + " y=" + ctx.args.y + "___"
      );
    }
  });

  hostApi.ui.window("board", {
    panel: {
      width: 300,
      height: 300,
      layout: {
        columns: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
        rows: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
        group: grid_1,
      },
      onClick: (ctx) => {
        if (!ctx.cursor) return;
        ctx.emitAction(action, {
          x: ctx.cursor.getX(),
          y: ctx.cursor.getY(),
        });
      },
    },
  }, [])
}
