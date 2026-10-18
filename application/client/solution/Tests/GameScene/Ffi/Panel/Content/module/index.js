/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("panel", {
    panel: {
      width: 100,
      height: 100,
      background: texture,
    },
  }, [
    hostApi.ui.text("panel-content", "Content"),
  ])
}
