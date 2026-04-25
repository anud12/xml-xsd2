/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi;
  hostApi.registerPanel({
    id: "top",
    size: {
      height: number.of(10),
      width: number.of(10)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    offset: {
      top: number.of(-100),
      bottom: number.of(-100),
      left: number.of(0),
      right: number.of(0),
    },
    background: hostApi.texture.of("texture.exr"),
  })

  hostApi.registerPanel({
    id: "left",
    size: {
      height: number.of(10),
      width: number.of(10)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    offset: {
      top: number.of(0),
      bottom: number.of(0),
      left: number.of(-100),
      right: number.of(-100),
    },
    background: hostApi.texture.of("texture.exr"),
  })

  hostApi.registerPanel({
    id: "bottom",
    size: {
      height: number.of(10),
      width: number.of(10)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    offset: {
      top: number.of(100),
      bottom: number.of(100),
      left: number.of(0),
      right: number.of(0),
    },
    background: hostApi.texture.of("texture.exr"),
  })

  hostApi.registerPanel({
    id: "right",
    size: {
      height: number.of(10),
      width: number.of(10)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    offset: {
      top: number.of(0),
      bottom: number.of(0),
      left: number.of(100),
      right: number.of(100),
    },
    background: hostApi.texture.of("texture.exr"),
  })
}