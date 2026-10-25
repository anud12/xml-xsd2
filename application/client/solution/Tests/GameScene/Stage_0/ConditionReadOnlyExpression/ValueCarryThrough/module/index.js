/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
    const {number, string} = hostApi.runtime;

    const filter = {
      filters: [(e) => e.id.isContainingExactly(string.of("entity_id"))],
    };
    const key = string.of("hp");

    const entity = hostApi.runtime.setEntity({
      id: string.of("entity_id"),
      numberMap: {
        "hp": number.of(5)
      }
    })

    const effect = hostApi.runtime.setEffect({
      name: string.of("drain"),
      reoccurAfterMs: () => hostApi.runtime.maybe.none(),
      apply: (context) => {
        context.getMutableEntityBy(filter).map(el => {
          el.number_map.put(key, (current) => {
            return current.map(v => {
              return v.isGreaterThan(number.of(0))
                .ifTrue((val) => val.sum(number.of(-1)))
                .or((val) => val);
            });
          });
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
      hostApi.ui.field("content", { entity: entity, map: "number", name: "hp", fallback: "0" }, font),
    ])
}
