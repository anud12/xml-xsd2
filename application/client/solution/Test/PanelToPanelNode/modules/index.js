/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi;
  hostApi.registerPanel({
    id: "center",
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5)
    },
    pivot: {
      x: number.of(0),
      y: number.of(0)
    },
    offset: {
      top: number.of(0),
      bottom: number.of(0),
      left: number.of(0),
      right: number.of(0),
    },
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.texture.of("modules/texture.exr"),
    children: panelApi => {
      hostApi.log("register panel")
    }
  })


}