/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const node1 = hostApi.runtime.setEntity({
    id: string.of("node-1"),
    numberMap: {
      "key": number.of(0)
    }
  });

  const repeat = hostApi.runtime.setEffect({
    name: string.of("repeat"),
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.runtime.maybe.of(number.of(10));
    },
    apply: (context, output) => {
      context.getEntityBy({
        id: id => id.isContainingExactly(string.of("node-1")),
      })
        .map(elementExpr => {
          elementExpr.getNumber(string.of("key")).map(v => v.sum(number.of(1)));
        })
    }
  });

  hostApi.runtime.emitEvent(repeat, {});

  hostApi.ui.window("badge", {
    panel: {
      width: 60,
      height: 60,
      x: 10,
      y: 10,
      background: "tex1.png",
    },
  }, [
    hostApi.ui.field("content", { entity: node1, map: "number", name: "key", fallback: "0" }),
  ]);
}
