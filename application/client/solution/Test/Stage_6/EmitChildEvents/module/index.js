/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const filter = hostApi.runtime.entity.filter.create().byId(id => id.isContainingExactly(string.of("entity_id")));
  const key = string.of("key")

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
        .map(v => v.modulo(number.of(2))
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
    name: "event",
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.runtime.maybe.of(number.of(10));
    },
    prepare: (context, input) => {
      /** Value must be condition expression, returned by prepare function */
      const value = context.emitEvent(string.of("key-modify-if-par"), {});
      return {value};
    },
    apply: (context, output) => {
      /** @type {ConditionExpression} */
      const shouldDouble = output.value;
      
      context.getEntityBy(filter)
        .map(elementExpr => {
          elementExpr.getNumber(key).map(v => {
            shouldDouble.ifFalse(() => {
              v.sum(number.of(1))
            })
            shouldDouble.ifTrue(() => {
              v.sum(number.of(3))
            })
            
          });
        })
    }
  })

  hostApi.runtime.emitEvent("event", {});

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
      name: key,
      type: "entityNumberValue",
      align: "center",
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture"))
  })

  hostApi.ui.registerPanel({
    id: "isModifiedPanel",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(50),
      bottom: number.of(50),
      left: number.of(50),
      right: number.of(150),
    },
    content: {
      entityId: string.of("entity_id"),
      name: string.of("isModified"),
      type: "entityTextValue",
      align: "center",
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture"))
  })
}