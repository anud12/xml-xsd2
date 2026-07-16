/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAction({
    name:"second action",
    apply: () => {
    }
  })

  hostApi.runtime.registerAction({
    name:"second second action",
    apply: (context) => {

    }
  })
}