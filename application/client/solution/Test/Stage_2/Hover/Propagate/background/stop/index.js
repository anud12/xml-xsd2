/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("parent", {
    width: 100,
    height: 100,
    background: "texture.png",
    onHover: {
      background: "hover.png",
    },
  }, [
    hostApi.ui.window("child", {
      width: 20,
      height: 20,
      x: 40,
      y: 40,
      background: "texture.png",
      onHover: {
        background: "hover.png",
        stopPropagation: true,
      },
    }, [])
  ])
}
