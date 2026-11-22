'use strict';

const assert = require('node:assert');
const eg = require('../src/js_host_api/entityBlueprint.js').create();
const sg = require('../src/js_host_api/stringBlueprint.js').create();
globalThis.__lxmNext = () => 0;

let registered;
globalThis.hostApi = { runtime: { setEntity: (entity) => { registered = entity; } } };

const names = sg.oneOf(() => ['alpha', 'beta']);
const blueprint = eg.oneOf(() => [{
  textMap: { name: names.generateValue() },
  numberMap: { hp: 10 },
}]);
const entity = blueprint.generateValue();
assert.strictEqual(entity.__id, registered.id);
assert.strictEqual(entity.text_map.get('name').orElse(null).value, 'alpha');
assert.strictEqual(blueprint.isGeneratedBy(entity).value, true);

let templates = [{}];
const dynamic = eg.oneOf(() => templates);
assert.ok(dynamic.generateValue().__id);
templates = [];
assert.throws(() => dynamic.generateValue(), /non-empty array/);
assert.throws(() => dynamic.isGeneratedBy({}), /non-empty array/);

const emptyField = eg.oneOf(() => [{ numberMap: { hp: { __numberBlueprint: true, pieces: () => [] } } }]);
assert.throws(() => emptyField.generateValue(), /empty language/);

console.log('entity blueprint tests passed');
