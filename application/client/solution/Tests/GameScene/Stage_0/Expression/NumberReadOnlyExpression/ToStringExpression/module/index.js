/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
    const {number, string} = hostApi.runtime;

    const entity = hostApi.runtime.setEntity({
      id: string.of("entity_id"),
      numberMap: {
        "value": number.of(42),
        "a": number.of(5),
        "b": number.of(3)
      },
      textMap: {
        "label": string.of("pending"),
        "sum_label": string.of("pending")
      }
    })

    const effect = hostApi.runtime.setEffect({
      name: string.of("convert"),
      reoccurAfterMs: () => hostApi.runtime.maybe.none(),
      apply: (context) => {
        context.getMutableEntityBy({
          filters: [(e) => e.id.isContainingExactly(string.of("entity_id"))]
        }).forEach(el => {
          el.text_map.put("label", () => hostApi.runtime.maybe.of(
            el.number_map.get("value").orElse(number.of(0)).toStringExpression()
          ));
          const sum = el.number_map.get("a").orElse(number.of(0))
            .sum(el.number_map.get("b").orElse(number.of(0)));
          el.text_map.put("sum_label", () => hostApi.runtime.maybe.of(
            sum.toStringExpression()
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
      hostApi.ui.field("content", { entity: entity, map: "text", name: "label", fallback: "?" }, font),
      hostApi.ui.field("sum_content", { entity: entity, map: "text", name: "sum_label", fallback: "?" }, font),
    ])
}
