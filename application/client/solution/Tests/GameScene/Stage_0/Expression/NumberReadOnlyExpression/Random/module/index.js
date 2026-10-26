/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
    const {number, string} = hostApi.runtime;

    const entity = hostApi.runtime.setEntity({
      id: string.of("entity_id"),
      numberMap: {
        "seed": number.of(0)
      },
      textMap: {
        "result": string.of("pending"),
        "chained": string.of("pending")
      }
    })

    const effect = hostApi.runtime.setEffect({
      name: string.of("roll"),
      reoccurAfterMs: () => hostApi.runtime.maybe.none(),
      apply: (context) => {
        context.getMutableEntityBy({
          filters: [(e) => e.id.isContainingExactly(string.of("entity_id"))]
        }).map(el => {
          const roll = number.random(number.of(5), number.of(5));
          el.text_map.put("result", () => hostApi.runtime.maybe.of(
            roll.toStringExpression()
          ));
          const chained = number.random(number.of(10), number.of(10)).sum(number.of(5));
          el.text_map.put("chained", () => hostApi.runtime.maybe.of(
            chained.toStringExpression()
          ));
        });
      }
    })

    hostApi.runtime.emitEvent(effect, {});

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
      hostApi.ui.field("content", { entity: entity, map: "text", name: "result", fallback: "?" }, font),
      hostApi.ui.field("chained_content", { entity: entity, map: "text", name: "chained", fallback: "?" }, font),
    ])
}
