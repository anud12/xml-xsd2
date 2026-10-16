/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  // The reloaded module no longer declares the "repeat" effect. It keeps the
  // entity (so the preserved value is still readable), but the previously
  // parked reoccurrence has nothing to resolve to and must stop firing.
  hostApi.runtime.setEntity({
    id: string.of("node-1"),
    numberMap: {
      "key": number.of(0)
    }
  });
}
