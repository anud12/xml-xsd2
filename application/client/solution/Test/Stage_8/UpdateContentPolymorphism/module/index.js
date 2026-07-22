/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.ui.registerPanel({
    id: "update_target",
    size: {
      height: number.of(100),
      width: number.of(300)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.texture.of("texture.png"),
    content: {
      type: "constant",
      align: "center",
      value: string.of("initial")
    }
  })
}
