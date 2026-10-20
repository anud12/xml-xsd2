/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const filter = {
    id: id => id.isContainingExactly(string.of("entity_id")),
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
      return context.getEntityBy(filter)
        .get(number.of(0))
        .flatMap(elementExpr => elementExpr.getNumber(key))
        .isCondition(value => value.isLessOrEqualTo(number.of(3)))
        .getOnTrueOrFalse(hostApi.runtime.maybe.of(hostApi.runtime.number.of(10)), hostApi.runtime.maybe.none());
    },
    apply: (context, output) => {
      context.getEntityBy(filter)
        .map(elementExpr => {
          elementExpr.getNumber(key).map(v => v.sum(number.of(1)));
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
    hostApi.ui.field("content", { entity: entity, map: "number", name: "key", fallback: "0" }),
  ])
}
