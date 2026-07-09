import { greet, add } from './helpers.js';

/** @type {ModuleEntrypoint} */
export default ({string, ...hostApi}) => {
  const name = greet("World");
  hostApi.log("Hello " + name);
  const sum = add(2, 3);
  hostApi.log("2 + 3 = " + sum);
}
