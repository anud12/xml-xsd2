'use strict';
// Engine-agnostic tests for the stringBlueprint host API.
// Run with: node test/stringBlueprint.test.js
const assert = require('node:assert');

// The embedded runtime registers this as globalThis.__stringBlueprintFactory;
// in Node the UMD wrapper exports it via module.exports.
const sgFactory = require('../src/js_host_api/stringBlueprint.js');
const sg = sgFactory.create();

// The blueprint draws concrete members from the host's seeded LXM stream
// (globalThis.__lxmNext), which the embedded runtime injects. In plain Node it
// is not present, so install a deterministic xorshift32 stream to make
// generateValue() reproducible and testable.
let _lxmState = 0x12345678 >>> 0;
globalThis.__lxmNext = () => {
  let x = _lxmState;
  x ^= (x << 13) >>> 0;
  x ^= x >>> 17;
  x ^= (x << 5) >>> 0;
  _lxmState = x >>> 0;
  return _lxmState / 4294967296;
};

let passed = 0;
let failed = 0;
function test(name, fn) {
  try {
    fn();
    passed++;
    console.log('ok - ' + name);
  } catch (e) {
    failed++;
    console.error('FAIL - ' + name);
    console.error('  ' + (e && e.message ? e.message : e));
  }
}

// oneOf takes a list-maker lambda () => Array<element>; concat takes a plain
// array.

// --- oneOf: union language + full-match membership ---------------------------

test('oneOf builds a union language; isGeneratedBy is full-match membership', () => {
  const g = sg.oneOf(() => ['Al', 'Bo', 'Cy']);
  assert.strictEqual(g.isGeneratedBy('Al').value, true);
  assert.strictEqual(g.isGeneratedBy('Bo').value, true);
  assert.strictEqual(g.isGeneratedBy('Cy').value, true);
  assert.strictEqual(g.isGeneratedBy('A').value, false); // substring, not a member
  assert.strictEqual(g.isGeneratedBy('Boo').value, false);
  assert.strictEqual(g.isGeneratedBy('al').value, false); // case-sensitive
});

test('oneOf dedupes repeated elements', () => {
  const g = sg.oneOf(() => ['Al', 'Al', 'Bo']);
  assert.deepStrictEqual(g.language(), ['Al', 'Bo']);
});

test('oneOf can union a literal with a generator grammar', () => {
  const g = sg.oneOf(() => ['Zed', sg.oneOf(() => ['Al', 'Bo']).generateValue()]);
  assert.deepStrictEqual(g.language(), ['Zed', 'Al', 'Bo']);
});

// --- concat: ordered product -------------------------------------------------

test('concat of literal strings yields their concatenation', () => {
  const g = sg.concat(['foo', 'bar']);
  assert.deepStrictEqual(g.language(), ['foobar']);
  assert.strictEqual(g.isGeneratedBy('foobar').value, true);
  assert.strictEqual(g.isGeneratedBy('foo').value, false);
});

test('concat of generator grammars composes the full product (name example)', () => {
  const first = sg.oneOf(() => ['Al', 'Bo']);
  const last = sg.oneOf(() => ['Smith', 'Jones']);
  const name = sg.concat([
    first.generateValue(),
    ' ',
    last.generateValue(),
  ]);
  assert.strictEqual(name.isGeneratedBy('Al Smith').value, true);
  assert.strictEqual(name.isGeneratedBy('Al Jones').value, true);
  assert.strictEqual(name.isGeneratedBy('Bo Smith').value, true);
  assert.strictEqual(name.isGeneratedBy('Bo Jones').value, true);
  assert.strictEqual(name.isGeneratedBy('Smith').value, false); // substring only
  assert.strictEqual(name.isGeneratedBy('Bo Jon').value, false);
  assert.strictEqual(name.isGeneratedBy('Zed Jones').value, false); // not in vocab
});

test('deeply nested generators compose transitively', () => {
  const a = sg.oneOf(() => ['x', 'y']);
  const b = sg.concat([
    a.generateValue(),
    '-',
    sg.oneOf(() => ['1', '2']).generateValue(),
  ]);
  const c = sg.concat([b.generateValue(), '!']);
  assert.strictEqual(c.isGeneratedBy('x-1!').value, true);
  assert.strictEqual(c.isGeneratedBy('y-2!').value, true);
  assert.strictEqual(c.isGeneratedBy('x-1').value, false); // full-match
  assert.strictEqual(c.isGeneratedBy('z-1!').value, false);
});

// --- generateValue returns an expression ------------------------------------

test('generateValue returns a grammar carrying the language', () => {
  const g = sg.oneOf(() => ['Al', 'Bo', 'Cy']);
  const grammar = g.generateValue();
  assert.deepStrictEqual(grammar.__lang, ['Al', 'Bo', 'Cy']);
  assert.ok(g.language().includes(String(grammar)));
  assert.strictEqual(grammar.isContaining('Al').value, true);
});

test('oneOf rejects an empty dynamically produced array', () => {
  const g = sg.oneOf(() => []);
  assert.throws(() => g.generateValue(), /non-empty array/);
  assert.throws(() => g.isGeneratedBy('anything'), /non-empty array/);
});

test('oneOf rechecks a changing list on each evaluation', () => {
  let choices = ['X'];
  const g = sg.oneOf(() => choices);
  assert.strictEqual(g.generateValue().value, 'X');
  choices = [];
  assert.throws(() => g.generateValue(), /non-empty array/);
});

test('an empty concatenation produces an empty string expression', () => {
  assert.strictEqual(sg.concat([]).generateValue().value, '');
});

test('isGeneratedBy returns a ConditionExpression with and/or/negate/ifTrue/ifFalse', () => {
  const g = sg.oneOf(() => ['Al']);
  const c = g.isGeneratedBy('Al');
  assert.strictEqual(c.value, true);
  let hit = 0;
  c.ifTrue(() => hit++);
  assert.strictEqual(hit, 1);
  assert.strictEqual(c.negate().value, false);
  const other = sg.oneOf(() => ['Al']).isGeneratedBy('Al');
  assert.strictEqual(c.and(other).value, true);
  assert.strictEqual(c.and(g.isGeneratedBy('Nope')).value, false);
  assert.strictEqual(c.or(g.isGeneratedBy('Nope')).value, true);
  let miss = 0;
  g.isGeneratedBy('Nope').ifFalse(() => miss++);
  assert.strictEqual(miss, 1);
});

test('isGeneratedBy accepts a string-expression object (with .value)', () => {
  const g = sg.oneOf(() => ['Al']);
  assert.strictEqual(g.isGeneratedBy({ value: 'Al' }).value, true);
  assert.strictEqual(g.isGeneratedBy({ value: 'Nope' }).value, false);
});

// --- summary -----------------------------------------------------------------

console.log('\n' + passed + ' passed, ' + failed + ' failed');
if (failed > 0) process.exit(1);
