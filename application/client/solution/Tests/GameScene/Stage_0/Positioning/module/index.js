/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("base", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [])

  hostApi.ui.window("offset", {
    panel: {
      width: 50,
      height: 50,
      x: 20,
      y: 30,
    },
  }, [])

  hostApi.ui.window("tl", {
    panel: {
      width: 100,
      height: 100,
      anchor: "top-left",
    },
  }, [])

  hostApi.ui.window("br", {
    panel: {
      width: 100,
      height: 100,
      anchor: "bottom-right",
    },
  }, [])

  hostApi.ui.window("bl", {
    panel: {
      width: 100,
      height: 100,
      anchor: "bottom-left",
    },
  }, [])

  hostApi.ui.window("tr", {
    panel: {
      width: 100,
      height: 100,
      anchor: "top-right",
    },
  }, [])
}
