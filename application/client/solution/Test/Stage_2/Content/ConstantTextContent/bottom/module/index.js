/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number,string } = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("texture.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.ui.registerPanel({
    id: "bottom",
    size: {
      height: number.of(300),
      width: number.of(300)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture")),
      content: {
      type: "constant",
      align: "bottom",
      value: string.of("bottom")
      }
  })
}