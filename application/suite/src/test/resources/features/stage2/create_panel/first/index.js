/**
 */
/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi;
  hostApi.registerPanel({
    id: "panel",
    anchor: {
      x: number.of(0),
      y: number.of(0)
    },
    pivot: {
      x: number.of(0),
      y: number.of(0)
    },
    offset: {
      x: number.of(0),
      y: number.of(0)
    },
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.texture.of("./texture.exr"),
    children: panelApi => {
      hostApi.log("register panel")
    }
  })
}