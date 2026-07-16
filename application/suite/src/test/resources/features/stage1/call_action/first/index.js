/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAction({
    name:"action",
    apply: () => {
      hostApi.runtime.log("action called")
    }
  })
}