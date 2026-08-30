/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("hoverParent", {
    width: 100,
    height: 100,
    background: "texture.png",
    onHover: {
      background: "hover.png",
    },
  }, [
    hostApi.ui.panel("inner", {
      width: 30,
      height: 30,
      x: 20,
      y: 20,
    }, [])
  ])
}
