/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  let a = 1;
  a++;
  hostApi.registerEffect({
    name: "empty effect" + a,
  })

}