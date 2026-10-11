/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.setAction({
    name:"second action",
    apply: () => {
    }
  })

  hostApi.runtime.setAction({
    name:"second second action",
    apply: (context) => {

    }
  })
}
