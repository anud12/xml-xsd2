/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  // Still images are single-frame animations.
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
  const hero = hostApi.runtime.setEntity({
    id: string.of("hero"),
    numberMap: { hp: number.of(7), max: number.of(10) }
  });
  hostApi.ui.window("prog-x-win", {
    panel: {
      x: 100, y: 100, width: 200, height: 50,
    },
  }, [
    hostApi.ui.progress("prog-x", {
      panel: {
        x: 0, y: 0, width: 200, height: 50,
      },
      progress: {
        fill: { animation: fill, fill: hFill },
        track: { animation: track, fill: hFill },
        orientation: "horizontal",
        entity: hero,
        value: (entity) => entity.number_map.get("hp").orElse(number.of(0)),
        max: (entity) => entity.number_map.get("max").orElse(number.of(1))
      }
    }),
  ]);
};
