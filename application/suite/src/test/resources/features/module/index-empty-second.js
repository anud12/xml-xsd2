/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.emitEvent("empty event", {})
  hostApi.registerEffect({
    name: "empty effect second",
  })

}