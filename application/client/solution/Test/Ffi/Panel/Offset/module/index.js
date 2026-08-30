/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.window("top", {
    x: 0,
    y: -100,
    width: 10,
    height: 10,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
  })
  hostApi.ui.window("left", {
    x: -100,
    y: 0,
    width: 10,
    height: 10,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
  })
  hostApi.ui.window("bottom", {
    x: 0,
    y: 100,
    width: 10,
    height: 10,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
  })
  hostApi.ui.window("right", {
    x: 100,
    y: 0,
    width: 10,
    height: 10,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
  })
}
