/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  const node_1 = hostApi.runtime.setEntity({
    id: "node-1",
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  const grid_1 = hostApi.runtime.setContainer({
    id: "grid-1",
    entities: [node_1],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getSpanX: (entity) => number.of(1),
    getSpanY: (entity) => number.of(1),
    sizeX: {
      value: number.of(3),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(3),
      outOfBounds: "clamp",
    },
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
        container: grid_1,
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
