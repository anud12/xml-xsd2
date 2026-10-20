/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.setAnimation(string.of("sequence"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
    ],
    duration: number.of(5),
  });

  hostApi.ui.window("seqPanel", {
    panel: {
      width: 100,
      height: 100,
      x: 100,
      y: 100,
      background: {
        animation: hostApi.ui.getAnimation(string.of("sequence")),
        fill: { width: "stretch", height: "stretch", keepAspectRatio: true },
      },
    },
  }, []);
}
