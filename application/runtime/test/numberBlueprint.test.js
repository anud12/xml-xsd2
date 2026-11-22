'use strict';
// Engine-agnostic tests for the numberBlueprint host API.
// Run with: node test/numberBlueprint.test.js
const assert = require('node:assert');

// The embedded runtime registers this as globalThis.__numberBlueprintFactory;
// in Node the UMD wrapper exports it via module.exports.
const nbFactory = require('../src/js_host_api/numberBlueprint.js');
const nb = nbFactory.create();

// The blueprint draws concrete integers from the host's seeded LXM stream
// (globalThis.__lxmNext), which the embedded runtime injects. In plain Node it
// is not present, so install a deterministic xorshift32 stream to make
// generateValue() reproducible and testable.
let _lxmState = 0x87654321 >>> 0;
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

// oneOf takes a list-maker lambda () => Array<element>; range takes a
// [fromInclusive, toInclusive] interval. Elements are fixed numbers,
// NumberExpressions, or sub-blueprints (passed directly).

// --- oneOf: union language + membership -------------------------------------

test('oneOf builds a union of integers; isGeneratedBy is integer membership', () => {
  const g = nb.oneOf(() => [1, 2, 4]);
  assert.strictEqual(g.isGeneratedBy(1).value, true);
  assert.strictEqual(g.isGeneratedBy(2).value, true);
  assert.strictEqual(g.isGeneratedBy(4).value, true);
  assert.strictEqual(g.isGeneratedBy(3).value, false); // gap
  assert.strictEqual(g.isGeneratedBy(0).value, false);
  assert.strictEqual(g.isGeneratedBy(5).value, false);
});

test('oneOf merges overlapping and adjacent pieces', () => {
  // [1,3] and [3,5] overlap at 3 -> merged [1,5]; [7,8] is separate.
  const g = nb.oneOf(() => [nb.range(1, 3), nb.range(3, 5), nb.range(7, 8)]);
  assert.deepStrictEqual(g.pieces(), [{ lo: 1, hi: 5 }, { lo: 7, hi: 8 }]);
  assert.strictEqual(g.isGeneratedBy(6).value, false); // gap between [1,5] and [7,8]
  assert.strictEqual(g.isGeneratedBy(5).value, true);
  assert.strictEqual(g.isGeneratedBy(7).value, true);
});

test('oneOf can union a literal with a sub-blueprint (passed directly)', () => {
  const g = nb.oneOf(() => [0, nb.range(10, 12)]);
  assert.strictEqual(g.isGeneratedBy(0).value, true);
  assert.strictEqual(g.isGeneratedBy(10).value, true);
  assert.strictEqual(g.isGeneratedBy(11).value, true);
  assert.strictEqual(g.isGeneratedBy(12).value, true);
  assert.strictEqual(g.isGeneratedBy(13).value, false);
  assert.strictEqual(g.isGeneratedBy(1).value, false);
});

// --- range: inclusive interval ------------------------------------------------

test('range yields an inclusive integer interval', () => {
  const g = nb.range(3, 7);
  assert.deepStrictEqual(g.pieces(), [{ lo: 3, hi: 7 }]);
  for (let n = 3; n <= 7; n++) assert.strictEqual(g.isGeneratedBy(n).value, true);
  assert.strictEqual(g.isGeneratedBy(2).value, false);
  assert.strictEqual(g.isGeneratedBy(8).value, false);
});

test('an empty range (from > to) is an empty language', () => {
  const g = nb.range(5, 2);
  assert.deepStrictEqual(g.pieces(), []);
  assert.strictEqual(g.generateValue().isPresent().value, false);
  assert.strictEqual(g.isGeneratedBy(3).value, false);
});

// --- generateValue: the blueprint -> Maybe<NumberExpression> conversion ------

test('generateValue is present for a non-empty language and yields a valid member', () => {
  const g = nb.oneOf(() => [1, 5, 9]);
  const m = g.generateValue();
  assert.strictEqual(m.isPresent().value, true);
  assert.strictEqual(m.isEmpty().value, false);
  const v = m.orElse(-1).value;
  assert.ok([1, 5, 9].indexOf(v) !== -1);
});

test('generateValue for a range always lands within the interval', () => {
  const g = nb.range(10, 20);
  for (let i = 0; i < 200; i++) {
    const v = g.generateValue().orElse(-1).value;
    assert.ok(v >= 10 && v <= 20, 'value ' + v + ' out of range');
  }
});

test('generateValue distributes across all pieces', () => {
  // Two disjoint singletons: over many draws both must appear.
  const g = nb.oneOf(() => [1, 2]);
  const seen = Object.create(null);
  for (let i = 0; i < 400; i++) seen[g.generateValue().orElse(-1).value] = 1;
  assert.ok(seen[1], 'value 1 never drawn');
  assert.ok(seen[2], 'value 2 never drawn');
});

test('oneOf([]) is an empty language: generateValue absent, isGeneratedBy false', () => {
  const g = nb.oneOf(() => []);
  const m = g.generateValue();
  assert.strictEqual(m.isPresent().value, false);
  assert.strictEqual(m.isEmpty().value, true);
  assert.strictEqual(m.orElse(-1), -1);
  assert.strictEqual(g.isGeneratedBy(0).value, false);
});

// --- returned Maybe / Condition shapes --------------------------------------

test('isGeneratedBy returns a ConditionExpression with and/or/negate/ifTrue/ifFalse', () => {
  const g = nb.oneOf(() => [1, 2]);
  const c = g.isGeneratedBy(1);
  assert.strictEqual(c.value, true);
  let hit = 0;
  c.ifTrue(() => hit++);
  assert.strictEqual(hit, 1);
  assert.strictEqual(c.negate().value, false);
  assert.strictEqual(c.and(g.isGeneratedBy(2)).value, true);
  assert.strictEqual(c.and(g.isGeneratedBy(9)).value, false);
  assert.strictEqual(c.or(g.isGeneratedBy(9)).value, true);
  let miss = 0;
  g.isGeneratedBy(9).ifFalse(() => miss++);
  assert.strictEqual(miss, 1);
});

test('generateValue Maybe supports map/flatMap/ifPresent', () => {
  const g = nb.oneOf(() => [7]);
  const m = g.generateValue();
  assert.strictEqual(m.map((e) => e.value * 2).value, 14);
  let ran = false;
  m.ifPresent(() => { ran = true; });
  assert.strictEqual(ran, true);
  const n = nb.oneOf(() => []).generateValue();
  let ranNone = false;
  n.ifPresent(() => { ranNone = true; });
  assert.strictEqual(ranNone, false);
});

test('isGeneratedBy accepts a number-expression object (with .value)', () => {
  const g = nb.oneOf(() => [1, 2]);
  assert.strictEqual(g.isGeneratedBy({ value: 2 }).value, true);
  assert.strictEqual(g.isGeneratedBy({ value: 9 }).value, false);
});

// --- summary -----------------------------------------------------------------

console.log('\n' + passed + ' passed, ' + failed + ' failed');
if (failed > 0) process.exit(1);
