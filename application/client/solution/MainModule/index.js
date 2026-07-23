/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const filter = hostApi.runtime.entity.filter.create().byId(id => id.isContainingExactly(string.of("entity_id")));
  const key = string.of("key");
  
  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "key": number.of(0)
    },
    textMap: {
      "isModified": string.of("No")
    }
  })

  hostApi.runtime.registerEffect({
    name: "key-modify-if-par",
    prepare: (context, input) => {
      const output = context.getEntityBy(filter).get(number.of(0)).flatMap(v => {
        return v.getNumber(key)
      })
        .map(v => v.modulo(number.of(3))
          .isEqualTo(number.of(0)));
      return output.orElse(hostApi.runtime.condition.of(false));
    },
    apply:(context, output) => {
      output.ifTrue(() => {
        context.getEntityBy(filter).get(number.of(0))
          .map(v => v.getText(string.of("isModified")).ifPresent(v => {
            v.set(string.of("Yes"))
          }))
      })
    }
  })
  
  hostApi.runtime.registerEffect({
    name: "repeat",
    reoccurAfterMs: (context, executionCount, input, output) => {
      return context.getEntityBy(filter)
        .get(number.of(0))
        .flatMap(elementExpr => elementExpr.getNumber(key))
        .isCondition(value => value.isLessOrEqualTo(number.of(20)))
        .getOnTrueOrFalse(hostApi.runtime.maybe.of(hostApi.runtime.number.of(1)), hostApi.runtime.maybe.none());
    },
    prepare: (context, input) => {
      return context.emitEvent(string.of("key-modify-if-par"), {})
    },
    apply: (context, output) => {
      context.getEntityBy(filter)
        .map(elementExpr => {
          elementExpr.getNumber(key).map(v => v.sum(number.of(1)));
        })
    }
  })

  hostApi.runtime.emitEvent("repeat", {});

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("hover.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("texture.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.ui.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(70),
      bottom: number.of(70),
      left: number.of(70),
      right: number.of(70),
    },
    content: {
      entityId: string.of("entity_id"),
      name: string.of("key"),
      type: "entityNumberValue",
      align: "top",
    },
    hover: {
      texture: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("hover.png")),
      thickness: 5,
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png"))
  })


  hostApi.ui.registerPanel({
    id: "isModifiedPanel",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(100),
      bottom: number.of(100),
      left: number.of(250),
      right: number.of(250),
    },
    content: {
      entityId: string.of("entity_id"),
      name: string.of("isModified"),
      type: "entityTextValue",
      align: "center",
    },
    hover: {
      texture: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("hover.png")),
      thickness: 10,
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png"))
  })
}
