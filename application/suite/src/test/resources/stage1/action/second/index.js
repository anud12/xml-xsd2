/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.registerAction({
    name:"second action",
    apply: () => {
    }
  })

  hostApi.registerAction({
    name:"second second action",
    apply: (context) => {

    }
  })
}
