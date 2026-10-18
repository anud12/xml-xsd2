/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  const frames = [
    { sprite: hostApi.ui.getSpritePNG("fill_1.png") },
    { sprite: hostApi.ui.getSpritePNG("fill_2.png") },
  ];
  const fillAnim = hostApi.runtime.setAnimation(string.of("fillAnim"), {
    frames: frames,
    duration: number.of(2),
    loop: true,
  });
  const hero = hostApi.runtime.setEntity({
    id: string.of("hero"),
    numberMap: { hp: number.of(5), max: number.of(10) }
  });
  hostApi.ui.progress("prog-anim", {
    panel: {
      x: 100, y: 100, width: 200, height: 50,
    },
    progress: {
      fill: fillAnim,
      track: hostApi.ui.getSpritePNG("track.png"),
      orientation: "horizontal",
      entity: hero,
      value: (entity) => entity.number_map.get("hp").orElse(number.of(0)),
      max: (entity) => entity.number_map.get("max").orElse(number.of(1))
    }
  });
};
