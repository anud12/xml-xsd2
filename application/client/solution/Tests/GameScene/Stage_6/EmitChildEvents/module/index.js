/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const filter = {
    filters: [(e) => e.id.isContainingExactly(string.of("entity_id"))],
  };
  const key = string.of("key")

  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    numberMap: {
      "key": number.of(0)
    },
    textMap: {
      "isModified": string.of("No")
    }
  })

  const keyModifyIfPar = hostApi.runtime.setEffect({
    name: string.of("key-modify-if-par"),
    prepare: (context, input) => {
      let output = hostApi.runtime.condition.of(false);
      context.getEntityBy(filter).map(elementExpr => {
        output = elementExpr.number_map.get(key)
          .map(v => v.modulo(number.of(2))
            .isEqualTo(number.of(0)))
          .orElse(hostApi.runtime.condition.of(false));
      });
      return output;
    },
    apply:(context, output) => {
      output.ifTrue(() => {
        context.getEntityBy(filter).map(elementExpr => {
          elementExpr.text_map.get(string.of("isModified")).ifPresent(v => {
            v.set(string.of("Yes"))
          })
        })
      })
      output.ifFalse(() => {
        context.getEntityBy(filter).map(elementExpr => {
          elementExpr.text_map.get(string.of("isModified")).ifPresent(v => {
            v.set(string.of("No"))
          })
        })
      })
    }
  })

  const event = hostApi.runtime.setEffect({
    name: string.of("event"),
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.runtime.maybe.of(number.of(10));
    },
    prepare: (context, input) => {
      /** Value must be condition expression, returned by prepare function */
      const value = context.emitEvent(keyModifyIfPar, {});
      return {value};
    },
    apply: (context, output) => {
      /** @type {ConditionReadOnlyExpression} */
      const shouldDouble = output.value;

      context.getEntityBy(filter)
        .map(elementExpr => {
          elementExpr.number_map.get(key).ifPresent(v => {
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

  hostApi.runtime.emitEvent(event, {});

  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.window("center", {
    panel: {
      x: 200,
      y: 450,
      width: 100,
      height: 100,
      anchor: "center",
      align: "center",
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.field("centerContent", { entity: entity, map: "number", name: "key", fallback: "0" }),
  ])

  hostApi.ui.window("isModifiedPanel", {
    panel: {
      x: 700,
      y: 450,
      width: 100,
      height: 100,
      anchor: "center",
      align: "center",
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.field("isModifiedContent", { entity: entity, map: "text", name: "isModified", fallback: "No" }),
  ])
}
