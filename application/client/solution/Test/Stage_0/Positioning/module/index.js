/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("base", {
    width: 100,
    height: 100,
  }, [])

  hostApi.ui.panel("offset", {
    width: 50,
    height: 50,
    x: 20,
    y: 30,
  }, [])

  hostApi.ui.panel("tl", {
    width: 100,
    height: 100,
    anchor: "top-left",
  }, [])

  hostApi.ui.panel("br", {
    width: 100,
    height: 100,
    anchor: "bottom-right",
  }, [])

  hostApi.ui.panel("bl", {
    width: 100,
    height: 100,
    anchor: "bottom-left",
  }, [])

  hostApi.ui.panel("tr", {
    width: 100,
    height: 100,
    anchor: "top-right",
  }, [])
}
