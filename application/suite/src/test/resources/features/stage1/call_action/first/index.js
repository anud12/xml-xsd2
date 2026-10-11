/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.setAction({
    name:"action",
    apply: () => {
      hostApi.runtime.log("action called")
    }
  })
}
