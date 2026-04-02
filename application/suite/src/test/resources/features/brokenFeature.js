/** Valid JS for TS check — cast to any when implementing subset */
/** @type {any} */
const api = /** @type {import('../../../../types/HostApi').HostApi} */ ({
  emitEvent(eventName, args) {
    // placeholder implementation
    return undefined;
  }
});

const value = String(1);

console.log('fixed file loaded', value);
