// Globals setup for QuickJS context
var string = { of: function(s) { return s; } };
var number = { of: function(n) { return n; } };
globalThis.string = string;
globalThis.number = number;
