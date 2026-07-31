/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.registerAnimation(string.of("sequence"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
    ],
  });

  hostApi.ui.registerPanel({
    id: "loopPanel",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(100),
      bottom: number.of(100),
      left: number.of(100),
      right: number.of(100),
    },
    background: hostApi.ui.getAnimation(string.of("sequence"), { duration: number.of(5), loop: true }),
  });
}
