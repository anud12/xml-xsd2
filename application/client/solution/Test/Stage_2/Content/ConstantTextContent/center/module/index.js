/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.registerPanel({
    id: "center",
    size: {
      height: number.of(300),
      width: number.of(300)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    content: {
      type: "constant",
      align: "center",
      value: string.of("center")
    }
  })
}