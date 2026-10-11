/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.setAction({
    name:"second action",
    apply: () => {
      hostApi.runtime.log("second action called")
    }
  })
}
