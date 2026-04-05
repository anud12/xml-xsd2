/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.registerEffect({
    name: "empty effect",
  })

}