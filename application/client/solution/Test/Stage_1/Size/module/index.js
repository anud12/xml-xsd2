/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  })
}