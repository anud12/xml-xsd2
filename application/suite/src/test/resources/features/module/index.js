/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.registerEvent({
    name: "empty event",
  })

}