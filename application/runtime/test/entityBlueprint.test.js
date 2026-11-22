'use strict';

const assert = require('node:assert');
const eg = require('../src/js_host_api/entityBlueprint.js').create();
const sg = require('../src/js_host_api/stringBlueprint.js').create();
globalThis.__lxmNext = () => 0;

const names = sg.oneOf(() => ['alpha', 'beta']);
const blueprint = eg.oneOf(() => [{
  textMap: { name: names.generateValue() },
  numberMap: { hp: 10 },
}]);
const entity = blueprint.generateValue();
assert.strictEqual(String(entity.id), entity.__id);
assert.strictEqual(entity.string_map.get('name').orElse(null).value, 'alpha');
assert.strictEqual(blueprint.isGeneratedBy(entity).value, true);

let registered;
const idExpression = { value: "hosted" };
globalThis.hostApi = { runtime: {
  string: { of: () => idExpression },
  setEntity: (declaration) => { registered = declaration; return entity; },
} };
const hosted = blueprint.generateValue();
assert.strictEqual(hosted.string_map.get('name').orElse(null).value, 'alpha');
assert.strictEqual(hosted.members('any').count().value, 0);
assert.strictEqual(registered.id, idExpression);
assert.strictEqual(registered.numberMap.hp, 10);
delete globalThis.hostApi;

let templates = [{}];
const dynamic = eg.oneOf(() => templates);
assert.ok(dynamic.generateValue().__id);
templates = [];
assert.throws(() => dynamic.generateValue(), /non-empty array/);
assert.throws(() => dynamic.isGeneratedBy({}), /non-empty array/);

const emptyField = eg.oneOf(() => [{ numberMap: { hp: { __numberBlueprint: true, pieces: () => [] } } }]);
assert.throws(() => emptyField.generateValue(), /empty language/);

console.log('entity blueprint tests passed');
