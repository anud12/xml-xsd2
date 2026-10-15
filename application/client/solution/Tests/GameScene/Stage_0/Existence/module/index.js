/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("base", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [])
}
