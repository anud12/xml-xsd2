/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const sequence = hostApi.runtime.setAnimation(string.of("sequence"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
    ],
    duration: number.of(5),
    loop: true,
  });

  hostApi.ui.panel("loopPanel", {
    panel: {
      width: 100,
      height: 100,
      x: 100,
      y: 100,
      background: sequence,
    },
  }, []);
}
