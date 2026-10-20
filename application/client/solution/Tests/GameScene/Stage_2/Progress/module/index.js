/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  const hero = hostApi.runtime.setEntity({
    id: string.of("hero"),
    numberMap: {
      hp: number.of(7), max: number.of(10),
      mp: number.of(6), maxMp: number.of(12)
    }
  });
  // Still images are single-frame animations: register the fill/track tiles.
  const fill = hostApi.runtime.setAnimation(string.of("fill"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("fill.png") }],
    duration: number.of(1),
  });
  const track = hostApi.runtime.setAnimation(string.of("track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("track.png") }],
    duration: number.of(1),
  });
  // Tile along the value axis, stretch across the other.
  /** @type {{width: "tile" | "stretch", height: "tile" | "stretch"}} */
  const hFill = { width: "tile", height: "stretch" };
  /** @type {{width: "tile" | "stretch", height: "tile" | "stretch"}} */
  const vFill = { width: "stretch", height: "tile" };
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
  hostApi.ui.window("prog-y-win", {
    panel: {
      x: 100, y: 200, width: 50, height: 200,
    },
  }, [
    hostApi.ui.progress("prog-y", {
      panel: {
        x: 0, y: 0, width: 50, height: 200,
      },
      progress: {
        fill: { animation: fill, fill: vFill },
        track: { animation: track, fill: vFill },
        orientation: "vertical",
        steps: 4,
        entity: hero,
        value: (entity) => entity.number_map.get("mp").orElse(number.of(0)),
        max: (entity) => entity.number_map.get("maxMp").orElse(number.of(1))
      }
    }),
  ]);
};
