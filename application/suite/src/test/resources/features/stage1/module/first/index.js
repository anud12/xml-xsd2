/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  hostApi.log("First module loaded");

}