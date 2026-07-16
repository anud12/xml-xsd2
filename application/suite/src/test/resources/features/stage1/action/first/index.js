/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAction({
    name:"action",
    apply: () => {
    }
  })

  hostApi.runtime.registerAction({
    name:"second action",
    apply: (context) => {

    }
  })
}