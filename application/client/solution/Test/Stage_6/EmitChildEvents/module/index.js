/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const filter = {
    id: id => id.isContainingExactly(string.of("entity_id")),
  };
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
      output.ifFalse(() => {
        context.getEntityBy(filter).get(number.of(0))
          .map(v => v.getText(string.of("isModified")).ifPresent(v => {
            v.set(string.of("No"))
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

  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("center", {
    x: 200,
    y: 450,
    width: 100,
    height: 100,
    anchor: "center",
    align: "center",
    background: hostApi.ui.getAnimation(string.of("texture")),
  }, [
    hostApi.ui.field("centerContent", { entity: "entity_id", map: "number", name: "key", fallback: "0" }),
  ])

  hostApi.ui.panel("isModifiedPanel", {
    x: 700,
    y: 450,
    width: 100,
    height: 100,
    anchor: "center",
    align: "center",
    background: hostApi.ui.getAnimation(string.of("texture")),
  }, [
    hostApi.ui.field("isModifiedContent", { entity: "entity_id", map: "text", name: "isModified", fallback: "No" }),
  ])
}