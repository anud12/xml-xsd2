/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.emitEvent(string.of("empty event"), {})
  hostApi.registerEvent({
    name: string.of("empty event"),
  })

}