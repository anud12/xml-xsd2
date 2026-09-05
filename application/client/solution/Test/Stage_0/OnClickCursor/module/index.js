/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;
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
