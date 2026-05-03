/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.registerAction({
    name:"action",
    apply: () => {
      hostApi.log("action called")
    }
  })
}
