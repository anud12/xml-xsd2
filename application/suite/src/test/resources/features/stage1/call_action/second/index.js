/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAction({
    name:"second action",
    apply: () => {
      hostApi.runtime.log("second action called")
    }
  })
}