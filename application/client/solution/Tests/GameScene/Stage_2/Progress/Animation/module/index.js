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
  // The track is a still image: a single-frame animation.
  const track = hostApi.runtime.setAnimation(string.of("track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("track.png") }],
    duration: number.of(1),
  });
  /** @type {{width: "tile" | "stretch", height: "tile" | "stretch"}} */
  const hFill = { width: "tile", height: "stretch" };
  const hero = hostApi.runtime.setEntity({
    id: string.of("hero"),
    numberMap: { hp: number.of(5), max: number.of(10) }
  });
  hostApi.ui.window("prog-anim-win", {
    panel: {
      x: 100, y: 100, width: 200, height: 50,
    },
  }, [
    hostApi.ui.progress("prog-anim", {
      panel: {
        x: 0, y: 0, width: 200, height: 50,
      },
      progress: {
        fill: { animation: fillAnim, fill: hFill },
        track: { animation: track, fill: hFill },
        orientation: "horizontal",
        entity: hero,
        value: (entity) => entity.number_map.get("hp").orElse(number.of(0)),
        max: (entity) => entity.number_map.get("max").orElse(number.of(1))
      }
    }),
  ]);
};
