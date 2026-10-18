/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("small", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [])

  hostApi.ui.window("wide", {
    panel: {
      width: 200,
      height: 50,
      x: 250,
    },
  }, [])

  hostApi.ui.window("tall", {
    panel: {
      width: 50,
      height: 200,
      x: 500,
    },
  }, [])
}
