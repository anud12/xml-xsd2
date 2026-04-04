/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  let a = 1;
  a++;
  hostApi.registerEvent({
    name: "empty event" + !a,
  })

}