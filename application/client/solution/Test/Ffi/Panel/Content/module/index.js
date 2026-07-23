/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.ui.registerPanel({
    id: "panel",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    content: {
      type: "constant",
      value: string.of("Content"),
      align: "center"
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
  })
}
