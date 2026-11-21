/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  const roll = number.random(number.of(0), number.of(1000000));
  hostApi.runtime.log("RND:" + roll.toString());
};
