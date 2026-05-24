// Compile panel content by flushing compiled panels from the bridge.
// Returns JSON array of compiled panel objects with AST references.
if (typeof __flushCompiledPanels === 'function') {
  __flushCompiledPanels();
} else {
  '[]';
}
