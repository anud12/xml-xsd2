/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("base", {
    width: 100,
    height: 100,
  }, [])

  hostApi.ui.window("offset", {
    width: 50,
    height: 50,
    x: 20,
    y: 30,
  }, [])

  hostApi.ui.window("tl", {
    width: 100,
    height: 100,
    anchor: "top-left",
  }, [])

  hostApi.ui.window("br", {
    width: 100,
    height: 100,
    anchor: "bottom-right",
  }, [])

  hostApi.ui.window("bl", {
    width: 100,
    height: 100,
    anchor: "bottom-left",
  }, [])

  hostApi.ui.window("tr", {
    width: 100,
    height: 100,
    anchor: "top-right",
  }, [])
}
