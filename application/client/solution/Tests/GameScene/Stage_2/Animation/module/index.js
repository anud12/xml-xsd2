/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const frames = [
    { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
    { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
    { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
    { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
    { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
  ];

  hostApi.runtime.registerAnimation(string.of("fastSequence"), {
    frames: frames,
    duration: number.of(5),
  });

  hostApi.runtime.registerAnimation(string.of("slowSequence"), {
    frames: frames,
    duration: number.of(10),
  });

  hostApi.ui.panel("fastPanel", {
    width: 100,
    height: 100,
    x: 100,
    y: 100,
    background: hostApi.ui.getAnimation(string.of("fastSequence")),
  }, []);

  hostApi.ui.panel("slowPanel", {
    width: 100,
    height: 100,
    x: 300,
    y: 100,
    background: hostApi.ui.getAnimation(string.of("slowSequence")),
  }, []);
}
