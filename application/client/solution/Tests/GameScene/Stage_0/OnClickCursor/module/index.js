/** @type {ModuleEntrypoint} */
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
      value: number.of(3),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(3),
      outOfBounds: "clamp",
    },
  });

  hostApi.runtime.registerAction({
    name: string.of("move"),
    apply: (ctx) => {
      hostApi.runtime.log(
        "___move fired x=" + ctx.args.x + " y=" + ctx.args.y + "___"
      );
    }
  });

  hostApi.ui.panel("board", {
    width: 300,
    height: 300,
    container: string.of("grid-1"),
    layout: {
      columns: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
      rows: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
    },
    onClick: (ctx) => {
      ctx.emitAction("move", {
        x: ctx.cursor.getX(),
        y: ctx.cursor.getY(),
      });
    },
  }, [])
}
