/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("center", {
    width: 200,
    height: 200,
    x: -50,
    y: -50,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  }, [])
}