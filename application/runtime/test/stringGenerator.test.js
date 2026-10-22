'use strict';
// Engine-agnostic tests for the stringGenerator host API.
// Run with: node test/stringGenerator.test.js
const assert = require('node:assert');

// The embedded runtime registers this as globalThis.__stringGeneratorFactory;
// in Node the UMD wrapper exports it via module.exports.
const sgFactory = require('../src/js_host_api/stringGenerator.js');
const sg = sgFactory.create();

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

// --- oneOf: union language + full-match membership ---------------------------

test('oneOf builds a union language; isGeneratedBy is full-match membership', () => {
  const g = sg.oneOf(['Al', 'Bo', 'Cy']);
  assert.strictEqual(g.isGeneratedBy('Al').value, true);
  assert.strictEqual(g.isGeneratedBy('Bo').value, true);
  assert.strictEqual(g.isGeneratedBy('Cy').value, true);
  assert.strictEqual(g.isGeneratedBy('A').value, false); // substring, not a member
  assert.strictEqual(g.isGeneratedBy('Boo').value, false);
  assert.strictEqual(g.isGeneratedBy('al').value, false); // case-sensitive
});

test('oneOf dedupes repeated elements', () => {
  const g = sg.oneOf(['Al', 'Al', 'Bo']);
  assert.deepStrictEqual(g.__lang, ['Al', 'Bo']);
});

test('oneOf can union a literal with a generator grammar', () => {
  const g = sg.oneOf(['Zed', sg.oneOf(['Al', 'Bo']).generateValue().orElse('')]);
  assert.deepStrictEqual(g.__lang, ['Zed', 'Al', 'Bo']);
});

// --- concat: ordered product -------------------------------------------------

test('concat of literal strings yields their concatenation', () => {
  const g = sg.concat(['foo', 'bar']);
  assert.deepStrictEqual(g.__lang, ['foobar']);
  assert.strictEqual(g.isGeneratedBy('foobar').value, true);
  assert.strictEqual(g.isGeneratedBy('foo').value, false);
});

test('concat of generator grammars composes the full product (name example)', () => {
  const first = sg.oneOf(['Al', 'Bo']);
  const last = sg.oneOf(['Smith', 'Jones']);
  const name = sg.concat([
    first.generateValue().orElse(''),
    ' ',
    last.generateValue().orElse(''),
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
  const a = sg.oneOf(['x', 'y']);
  const b = sg.concat([
    a.generateValue().orElse(''),
    '-',
    sg.oneOf(['1', '2']).generateValue().orElse(''),
  ]);
  const c = sg.concat([b.generateValue().orElse(''), '!']);
  assert.strictEqual(c.isGeneratedBy('x-1!').value, true);
  assert.strictEqual(c.isGeneratedBy('y-2!').value, true);
  assert.strictEqual(c.isGeneratedBy('x-1').value, false); // full-match
  assert.strictEqual(c.isGeneratedBy('z-1!').value, false);
});

// --- generateValue: the generator -> Maybe<StringExpression> conversion ------

test('generateValue is present for a non-empty language; grammar carries the language', () => {
  const g = sg.oneOf(['Al', 'Bo', 'Cy']);
  const m = g.generateValue();
  assert.strictEqual(m.isPresent().value, true);
  assert.strictEqual(m.isEmpty().value, false);
  const grammar = m.orElse('FALLBACK');
  assert.notStrictEqual(grammar, 'FALLBACK');
  assert.deepStrictEqual(grammar.__lang, ['Al', 'Bo', 'Cy']);
  // the representative value is a valid member
  assert.ok(g.__lang.indexOf(String(grammar)) !== -1);
});

test('oneOf([]) is an empty language: generateValue absent, isGeneratedBy false', () => {
  const g = sg.oneOf([]);
  const m = g.generateValue();
  assert.strictEqual(m.isPresent().value, false);
  assert.strictEqual(m.isEmpty().value, true);
  assert.strictEqual(m.orElse('FALLBACK'), 'FALLBACK');
  assert.strictEqual(g.isGeneratedBy('anything').value, false);
});

test('chaining an empty generator: the caller chooses the fallback', () => {
  const empty = sg.oneOf([]);
  const spliced = empty.generateValue().orElse('X');
  const g = sg.oneOf([spliced]);
  assert.deepStrictEqual(g.__lang, ['X']);
  assert.strictEqual(g.isGeneratedBy('X').value, true);
});

// --- returned Maybe / Condition shapes --------------------------------------

test('isGeneratedBy returns a ConditionExpression with and/or/negate/ifTrue/ifFalse', () => {
  const g = sg.oneOf(['Al']);
  const c = g.isGeneratedBy('Al');
  assert.strictEqual(c.value, true);
  let hit = 0;
  c.ifTrue(() => hit++);
  assert.strictEqual(hit, 1);
  assert.strictEqual(c.negate().value, false);
  const other = sg.oneOf(['Al']).isGeneratedBy('Al');
  assert.strictEqual(c.and(other).value, true);
  assert.strictEqual(c.and(g.isGeneratedBy('Nope')).value, false);
  assert.strictEqual(c.or(g.isGeneratedBy('Nope')).value, true);
  let miss = 0;
  g.isGeneratedBy('Nope').ifFalse(() => miss++);
  assert.strictEqual(miss, 1);
});

test('generateValue Maybe supports map/flatMap/ifPresent', () => {
  const g = sg.oneOf(['Al']);
  const m = g.generateValue();
  assert.strictEqual(m.map((grammar) => grammar.__lang.join(',')).value, 'Al');
  let ran = false;
  m.ifPresent(() => { ran = true; });
  assert.strictEqual(ran, true);
  const n = sg.oneOf([]).generateValue();
  let ranNone = false;
  n.ifPresent(() => { ranNone = true; });
  assert.strictEqual(ranNone, false);
});

test('isGeneratedBy accepts a string-expression object (with .value)', () => {
  const g = sg.oneOf(['Al']);
  assert.strictEqual(g.isGeneratedBy({ value: 'Al' }).value, true);
  assert.strictEqual(g.isGeneratedBy({ value: 'Nope' }).value, false);
});

// --- summary -----------------------------------------------------------------

console.log('\n' + passed + ' passed, ' + failed + ' failed');
if (failed > 0) process.exit(1);
