/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("small", {
    width: 100,
    height: 100,
  }, [])

  hostApi.ui.panel("wide", {
    width: 200,
    height: 50,
    x: 250,
  }, [])

  hostApi.ui.panel("tall", {
    width: 50,
    height: 200,
    x: 500,
  }, [])
}
