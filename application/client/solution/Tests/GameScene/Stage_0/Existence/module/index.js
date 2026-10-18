/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("base", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [])
}
