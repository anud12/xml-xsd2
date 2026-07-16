import { greet, add } from './helpers.js';

/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;
  const name = greet("World");
  hostApi.runtime.log("Hello " + name);
  const sum = add(2, 3);
  hostApi.runtime.log("2 + 3 = " + sum);
}
