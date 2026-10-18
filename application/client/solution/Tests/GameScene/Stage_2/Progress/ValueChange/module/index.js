/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
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
        fill: hostApi.ui.getSpritePNG("fill.png"),
        track: hostApi.ui.getSpritePNG("track.png"),
        orientation: "horizontal",
        entity: hero,
        value: (entity) => entity.number_map.get("hp").orElse(number.of(0)),
        max: (entity) => entity.number_map.get("max").orElse(number.of(1))
      }
    }),
  ]);
};
