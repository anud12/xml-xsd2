/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "key": number.of(0)
    }
  })
  
  hostApi.runtime.registerEffect({
    name:"repeat",
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.runtime.maybe.none();
    },
    apply:(context, output) => {
      context.getEntityBy(hostApi.runtime.entity.filter.create().byId(id => id.isContainingExactly(string.of("entity_id"))))
        .map(elementExpr => {
          elementExpr.getNumber(string.of("key")).map(v => v.sum(number.of(1)));
        })
    }
  })
  
  hostApi.runtime.emitEvent("repeat", {});

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png"), gtu: hostApi.runtime.number.of(10) },
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
      name: string.of("key"),
      type: "entityNumberValue",
      align: "center",
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"))
  })
}