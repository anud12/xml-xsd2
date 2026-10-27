/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const { number, string } = hostApi.runtime;
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });

    // Display entity holding the live count of "item" entities.
    const game = hostApi.runtime.setEntity({
      id: string.of("game"),
      numberMap: { count: number.of(0), itemCount: number.of(2) },
    });

    // Two pre-existing items; the list grows by one on each effect execution.
    hostApi.runtime.setEntity({ id: string.of("item0") });
    hostApi.runtime.setEntity({ id: string.of("item1") });

    const itemFilter = { filters: [(e) => e.id.isContaining(string.of("item"))] };
    const gameFilter = { filters: [(e) => e.id.isContainingExactly(string.of("game"))] };

    const grow = hostApi.runtime.setEffect({
      name: string.of("grow"),
      reoccurAfterMs: () => hostApi.runtime.maybe.of(number.of(10)),
      apply: (context) => {
        // Record the current list size; reflects items added on earlier ticks.
        const cnt = context.getMutableEntityBy(itemFilter).count();
        context.getMutableEntityBy(gameFilter).map((g) => {
          g.number_map.put("count", () => hostApi.runtime.maybe.of(cnt));
        });
        // Grow the list so the next tick observes a different count.
        context.getMutableEntityBy(gameFilter).map((g) => {
          const n = g.number_map.get("itemCount").orElse(number.of(0));
          const item = hostApi.runtime.setEntity({
            id: string.of("item").concat(n.toStringExpression()),
          });
          context.createEntity(item);
          g.number_map.put("itemCount", () => hostApi.runtime.maybe.of(n.sum(number.of(1))));
        });
      },
    });

    hostApi.runtime.emitEvent(grow, {});

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
      hostApi.ui.field("content", { entity: game, map: "number", name: "count", fallback: "?" }, font),
    ])
}
