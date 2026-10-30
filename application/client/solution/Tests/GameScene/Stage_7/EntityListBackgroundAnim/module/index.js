/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  const game = hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { landCount: number.of(0) },
  });

  const inventory = hostApi.runtime.setContainer({
    id: string.of("inventory"),
    entities: [],
  });

  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 14, color: "#FFFFFF" });

  const track = hostApi.runtime.setAnimation(string.of("item-track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("black_pixel.png") }],
    duration: number.of(1),
  });

  hostApi.ui.window("inv-list", {
    panel: {
      x: 10,
      y: 10,
      width: 200,
      height: 300,
      title: "Inventory",
    },
  }, [
    hostApi.ui.entityList("inv-items", { container: inventory },
      (entity) => [
        hostApi.ui.window(entity.id, {
          panel: {
            width: 180,
            height: 40,
            layout: "column",
            background: {
              animation: track,
              fill: { width: "stretch", height: "stretch" },
            },
          },
        }, [
          hostApi.ui.field(entity.id + ":name", {
            entity: entity,
            map: "text",
            name: "name",
            fallback: "Unknown",
          }, font),
        ]),
      ]),
  ]);

  const tickEffect = hostApi.runtime.setEffect({
    name: string.of("tick"),
    reoccurAfterMs: () => hostApi.runtime.maybe.of(number.of(10)),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).forEach((g) => {
        const count = g.number_map.get("landCount").orElse(number.of(0));
        const next = count.sum(number.of(1));
        const item = hostApi.runtime.setEntity({
          id: next.toStringExpression(),
          textMap: { name: string.of("Item") },
          numberMap: { cost: number.of(10) },
        });
        context.createEntity(item);
        context.getMutableContainersBy({
          filters: [(c) => c.id.isContainingExactly(string.of("inventory"))],
        }).forEach((container) => {
          container.addEntity(item);
        });
        g.number_map.put("landCount", () => hostApi.runtime.maybe.of(next));
      });
    },
  });

  hostApi.runtime.emitEvent(tickEffect, {});
}
