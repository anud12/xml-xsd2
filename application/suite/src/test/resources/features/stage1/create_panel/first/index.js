/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, number, ...hostApi}) => {
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
    children: panelApi => {
      hostApi.log("register panel")
    }
  })
}