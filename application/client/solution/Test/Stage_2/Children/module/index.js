/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number } = hostApi.runtime;
  hostApi.ui.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
    children: [
      {
        id: "child",
        size: {
          height: number.of(10),
          width: number.of(10),
        },
        background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture_2.png"))
      },
      {
        id: "child_2",
        size: {
          height: number.of(10),
          width: number.of(10),
        },
        background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture_2.png"))
      },
    ]
  })
}