/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.setAction({
    name:"action",
    apply: () => {
    }
  })

  hostApi.runtime.setAction({
    name:"second action",
    apply: (context) => {

    }
  })
}
