/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("small", {
    width: 100,
    height: 100,
  }, [])

  hostApi.ui.window("wide", {
    width: 200,
    height: 50,
    x: 250,
  }, [])

  hostApi.ui.window("tall", {
    width: 50,
    height: 200,
    x: 500,
  }, [])
}
