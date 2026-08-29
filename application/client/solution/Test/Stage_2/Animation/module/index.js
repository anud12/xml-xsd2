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

  hostApi.ui.window("fastPanel", {
    width: 100,
    height: 100,
    x: 100,
    y: 100,
    background: hostApi.ui.getAnimation(string.of("sequence"), { duration: number.of(5) }),
  }, []);

  hostApi.ui.window("slowPanel", {
    width: 100,
    height: 100,
    x: 300,
    y: 100,
    background: hostApi.ui.getAnimation(string.of("sequence"), { duration: number.of(10) }),
  }, []);
}
