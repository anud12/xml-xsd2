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
  hostApi.ui.progress("prog-x", {
    panel: {
      x: 100, y: 100, width: 200, height: 50,
    },
    progress: {
      fill: hostApi.ui.getSpritePNG("fill.png"),
      track: hostApi.ui.getSpritePNG("track.png"),
      orientation: "horizontal",
      entity: hero,
      value: (entity) => entity.number_map.get("hp").orElse(number.of(0)),
      max: (entity) => entity.number_map.get("max").orElse(number.of(1))
    }
  });
  hostApi.ui.progress("prog-y", {
    panel: {
      x: 100, y: 200, width: 50, height: 200,
    },
    progress: {
      fill: hostApi.ui.getSpritePNG("fill.png"),
      track: hostApi.ui.getSpritePNG("track.png"),
      orientation: "vertical",
      steps: 4,
      entity: hero,
      value: (entity) => entity.number_map.get("mp").orElse(number.of(0)),
      max: (entity) => entity.number_map.get("maxMp").orElse(number.of(1))
    }
  });
};
