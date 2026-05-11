// Call __module_default with hostApi if it exists
if (typeof __module_default === 'function') {
  try {
    var hostApi = globalThis.host || {};
    __module_default(hostApi);
  } catch (e) {
    // ignore errors during module initialization
  }
}
