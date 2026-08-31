/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture_2"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture_2.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("center", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  }, [
    hostApi.ui.panel("child", {
      width: 10,
      height: 10,
      x: 0,
      y: 0,
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture_2")),
    }, []),
    hostApi.ui.panel("child_2", {
      width: 10,
      height: 10,
      x: 0,
      y: 10,
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture_2")),
    }, []),
  ])
}