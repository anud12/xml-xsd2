/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.setAction({
    name:hostApi.runtime.string.of("second action"),
    apply: () => {
      hostApi.runtime.log("second action called")
    }
  })
}
