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

  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("center", {
    width: 100,
    height: 100,
    anchor: "center",
    align: "center",
    background: hostApi.ui.getAnimation(string.of("texture")),
  }, [
    hostApi.ui.field("content", { entity: "entity_id", map: "number", name: "key", fallback: "0" }),
  ])
}