/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("base", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [])

  hostApi.ui.panel("offset", {
    panel: {
      width: 50,
      height: 50,
      x: 20,
      y: 30,
    },
  }, [])

  hostApi.ui.panel("tl", {
    panel: {
      width: 100,
      height: 100,
      anchor: "top-left",
    },
  }, [])

  hostApi.ui.panel("br", {
    panel: {
      width: 100,
      height: 100,
      anchor: "bottom-right",
    },
  }, [])

  hostApi.ui.panel("bl", {
    panel: {
      width: 100,
      height: 100,
      anchor: "bottom-left",
    },
  }, [])

  hostApi.ui.panel("tr", {
    panel: {
      width: 100,
      height: 100,
      anchor: "top-right",
    },
  }, [])
}
