/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const filter = hostApi.runtime.entity.filter.create().byId(id => id.isContainingExactly(string.of("entity_id")));
  const key = string.of("key")

  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "key": number.of(0)
    }
  })

  hostApi.runtime.registerEffect({
    name: "repeat",
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

  hostApi.runtime.emitEvent("repeat", {});

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(50),
      bottom: number.of(50),
      left: number.of(50),
      right: number.of(50),
    },
    content: {
      entityId: string.of("entity_id"),
      name: key,
      type: "entityNumberValue",
      align: "center",
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) })
  })
}