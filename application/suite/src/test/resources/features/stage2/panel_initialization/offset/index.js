/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.ui.registerPanel({
    id: "panel",
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5)
    },
    offset: {
      top: number.of(0),
      bottom: number.of(0),
      left: number.of(0),
      right: number.of(0)
    },
    size: {
      height: number.of(0),
      width: number.of(0)
    },
    background: { frames: [{ sprite: hostApi.ui.getSpritePNG("modules/texture.exr") }] },
  })
  hostApi.ui.registerPanel({
    id: "panel_2",
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5)
    },
    offset: {
      top: number.of(1),
      bottom: number.of(1),
      left: number.of(1),
      right: number.of(1)
    },
    size: {
      height: number.of(0),
      width: number.of(0)
    },
    background: { frames: [{ sprite: hostApi.ui.getSpritePNG("modules/texture.exr") }] },
  })
}