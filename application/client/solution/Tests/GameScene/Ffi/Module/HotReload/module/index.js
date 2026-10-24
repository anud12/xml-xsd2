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
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("node-1"))],
      })
        .map(elementExpr => {
          elementExpr.number_map.put("key", (current) => current.map(v => v.sum(number.of(1))));
        })
    }
  });

  hostApi.runtime.emitEvent(repeat, {});

  // A still image is a single-frame animation.
  const tex1 = hostApi.runtime.setAnimation(string.of("tex1"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("tex1.png") }],
    duration: number.of(1),
  });

  hostApi.ui.window("badge", {
    panel: {
      width: 60,
      height: 60,
      x: 10,
      y: 10,
      background: { animation: tex1, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.field("content", { entity: node1, map: "number", name: "key", fallback: "0" }),
  ]);
}
