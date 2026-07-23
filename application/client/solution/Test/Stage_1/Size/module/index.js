/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.ui.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
  })
}