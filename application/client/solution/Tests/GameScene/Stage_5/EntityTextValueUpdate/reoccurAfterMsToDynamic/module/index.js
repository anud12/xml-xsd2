/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
 const {number, string} = hostApi.runtime;

  const filter = {
    filters: [(e) => e.id.isContainingExactly(string.of("entity_id"))],
  };
  const key = string.of("key")

  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    numberMap: {
      "key": number.of(0)
    }
  })

  const repeat = hostApi.runtime.setEffect({
    name: string.of("repeat"),
    reoccurAfterMs: (context, executionCount, input, output) => {
      let low = hostApi.runtime.condition.of(false);
      context.getEntityBy(filter).map(elementExpr => {
        low = elementExpr.number_map.get(key)
          .map(v => v.isLessThan(number.of(3)))
          .orElse(hostApi.runtime.condition.of(false));
      });
      let interval = hostApi.runtime.maybe.of(number.of(10));
      low.ifFalse(() => {
        interval = hostApi.runtime.maybe.none();
      });
      return interval;
    },
    apply: (context, output) => {
      context.getMutableEntityBy(filter)
        .map(elementExpr => {
          elementExpr.number_map.put(key, (current) => current.map(v => v.sum(number.of(1))));
        })
    }
  })

  hostApi.runtime.emitEvent(repeat, {});

  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.window("center", {
    panel: {
      width: 100,
      height: 100,
      anchor: "center",
      align: "center",
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.field("content", { entity: entity, map: "number", name: "key", fallback: "0" }, font),
  ])
}