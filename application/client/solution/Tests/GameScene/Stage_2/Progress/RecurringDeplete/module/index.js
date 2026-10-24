/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string, maybe } = hostApi.runtime;
  const fill = hostApi.runtime.setAnimation(string.of("fill"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("fill.png") }],
    duration: number.of(1),
  });
  const track = hostApi.runtime.setAnimation(string.of("track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("track.png") }],
    duration: number.of(1),
  });
  /** @type {{width: "tile" | "stretch", height: "tile" | "stretch"}} */
  const hFill = { width: "tile", height: "stretch" };
  const game = hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { progress: number.of(5) }
  });
  const tick = hostApi.runtime.setEffect({
    name: string.of("tick"),
    reoccurAfterMs: (context) => {
      const current = Number(
        context.getEntityBy({
          filters: [(e) => e.id.isContainingExactly(string.of("game"))],
        }).map((el) =>
          el.number_map.get("progress").orElse(number.of(0))
        )
      );
      if (current <= 0) return maybe.none();
      return maybe.of(number.of(1));
    },
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((entity) => {
        entity.number_map.put("progress", (cur) =>
          cur.map(v => v.sum(number.of(-1)))
        );
      });
    },
  });
  hostApi.runtime.emitEvent(tick, {});
  hostApi.ui.window("deplete-win", {
    panel: { x: 50, y: 50, width: 200, height: 40 },
  }, [
    hostApi.ui.progress("deplete-bar", {
      panel: { x: 0, y: 0, width: 200, height: 40 },
      progress: {
        fill: { animation: fill, fill: hFill },
        track: { animation: track, fill: hFill },
        orientation: "horizontal",
        entity: game,
        value: (entity) => entity.number_map.get("progress").orElse(number.of(0)),
        max: (entity) => number.of(5),
        min: (entity) => number.of(0),
        steps: 5,
      }
    }),
  ]);
};
