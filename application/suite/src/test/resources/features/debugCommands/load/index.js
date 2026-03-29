/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.emitEvent("empty event", {})
  hostApi.registerEvent({
    name: "empty event",
  })

}