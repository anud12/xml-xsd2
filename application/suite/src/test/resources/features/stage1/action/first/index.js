/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.registerAction({
    name:"action",
    apply: () => {
    }
  })

  hostApi.registerAction({
    name:"second action",
    apply: (context) => {

    }
  })
}