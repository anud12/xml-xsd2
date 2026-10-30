/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });

  // Faithful copy of the TestTickSource "add-btn": a window whose only child is
  // a `field` bound to the game's "property" number, which a reoccurring tick
  // changes every 10ms. The label must re-render cleanly as the value changes.
  const game = hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { property: number.of(1000) },
  });

  const tick = hostApi.runtime.setEffect({
    name: string.of("interest-tick"),
    reoccurAfterMs: () => hostApi.runtime.maybe.of(number.of(10)),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).forEach((entity) => {
        entity.number_map.put("property", (current) => current.map(v => v.sum(number.of(-10))));
      });
    },
  });
  hostApi.runtime.emitEvent(tick, {});

  hostApi.ui.window("add-btn", {
    panel: { x: 600, y: 440, width: 100, height: 40 },
  }, [
    hostApi.ui.field("add-label", { entity: game, map: "number", name: "property", fallback: "0" }, font),
  ]);
}
