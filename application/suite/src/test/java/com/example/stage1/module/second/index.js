/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.log("Second module loaded");
}