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

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
    ],
  });
  hostApi.ui.panel("center", {
    x: 70,
    y: 70,
    width: 100,
    height: 100,
    onHover: {
      texture: "hover.png",
      thickness: 5,
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(5), loop:true }),
  }, [
    hostApi.ui.field("centerContent", { entity: "entity_id", map: "number", name: "key", fallback: "0", align: "top" }),
  ])


  hostApi.ui.panel("isModifiedPanel", {
    x: 250,
    y: 100,
    width: 100,
    height: 100,
    onHover: {
      texture: "hover.png",
      thickness: 10,
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(30), loop:true }),
  }, [
    hostApi.ui.field("isModifiedContent", { entity: "entity_id", map: "text", name: "isModified", fallback: "No", align: "center" }),
  ])
}
