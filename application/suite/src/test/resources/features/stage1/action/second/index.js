/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.setAction({
    name:hostApi.runtime.string.of("second action"),
    apply: () => {
    }
  })

  hostApi.runtime.setAction({
    name:hostApi.runtime.string.of("second second action"),
    apply: (context) => {

    }
  })
}
