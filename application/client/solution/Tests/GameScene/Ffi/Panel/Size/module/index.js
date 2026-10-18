/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("center", {
    panel: {
      width: 100,
      height: 100,
      background: texture,
    },
  })
}
