/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.registerAction({
    name:"second action",
    apply: () => {
      hostApi.log("second action called")
    }
  })
}
