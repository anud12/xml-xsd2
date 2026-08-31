'use strict';
// Engine-agnostic tests for the .ui spine (host.js). Run with: node test/spine.test.js
const assert = require('node:assert');

let registered = [];
globalThis.__uiTransport = {
  registerNode: (n) => registered.push(n),
  emitDelta: () => {},
  readClientState: () => ({ clientId: 'local', actor: null, values: {} }),
  resolveResource: (name) => name,
};

const host = require('../ui/host.js');

let passed = 0;
function test(name, fn) {
  fn();
  passed++;
  console.log('ok - ' + name);
}

test('ui.text registers a text node with its id and value', () => {
  registered = [];
  host.clear();
  const id = host.text('spine-text', 'spine');
  assert.strictEqual(id, 'spine-text');
  assert.strictEqual(registered.length, 1);
  assert.deepStrictEqual(registered[0], {
    id: 'spine-text', kind: 'text', value: 'spine', children: [],
  });
});

test('ui.div registers a division node with children ids', () => {
  registered = [];
  host.clear();
  const t = host.text('spine-text', 'spine');
  const d = host.div('spine-div', {}, [t]);
  assert.strictEqual(d, 'spine-div');
  const snap = host.snapshot();
  assert.strictEqual(snap.length, 2);
  const div = snap.find((n) => n.id === 'spine-div');
  assert.deepStrictEqual(div.children, ['spine-text']);
  assert.strictEqual(div.kind, 'division');
});

test('id is mandatory: missing id throws', () => {
  assert.throws(() => host.text('', 'x'), /mandatory id/);
  assert.throws(() => host.div(undefined, {}, []), /mandatory id/);
});

test('duplicate id is rejected', () => {
  host.clear();
  host.text('dup', 'a');
  assert.throws(() => host.text('dup', 'b'), /duplicate id/);
  assert.throws(() => host.div('dup', {}, []), /duplicate id/);
});

test('children must be node ids', () => {
  host.clear();
  assert.throws(() => host.div('d', {}, [42]), /children must be node ids/);
});

test('ui.field registers a field node with its binding', () => {
  registered = [];
  host.clear();
  const id = host.field('hp-field', {
    entity: 'ent-a', map: 'number', name: 'hp', fallback: 'n/a',
  });
  assert.strictEqual(id, 'hp-field');
  assert.strictEqual(registered.length, 1);
  assert.deepStrictEqual(registered[0], {
    id: 'hp-field',
    kind: 'field',
    binding: { entity: 'ent-a', map: 'number', name: 'hp', fallback: 'n/a' },
    value: 'n/a',
    children: [],
  });
});

test('ui.image registers an image node with its src', () => {
  registered = [];
  host.clear();
  const id = host.image('spine-img', 'art/hover.png');
  assert.strictEqual(id, 'spine-img');
  assert.strictEqual(registered.length, 1);
  assert.deepStrictEqual(registered[0], {
    id: 'spine-img', kind: 'image', src: 'art/hover.png', children: [],
  });
});

test('ui.image rejects empty src and missing id', () => {
  host.clear();
  assert.throws(() => host.image('', 'art/a.png'), /mandatory id/);
  assert.throws(() => host.image('img', ''), /non-empty archive path/);
  assert.throws(() => host.image('img', 42), /non-empty archive path/);
});

test('ui.canvas registers a canvas node with world options', () => {
  registered = [];
  host.clear();
  const id = host.canvas('world-canvas', {
    world: { map: 'cave', room: 'cave-1' },
    camera: { room: 'cave-1', x: 0, y: 0, zoom: 2 },
  }, []);
  assert.strictEqual(id, 'world-canvas');
  assert.strictEqual(registered.length, 1);
  assert.deepStrictEqual(registered[0], {
    id: 'world-canvas',
    kind: 'canvas',
    options: {
      world: { map: 'cave', room: 'cave-1' },
      camera: { room: 'cave-1', x: 0, y: 0, zoom: 2 },
    },
    children: [],
  });
});

test('ui.container registers a division node with a container marker child', () => {
  registered = [];
  host.clear();
  const render = (entity) => [host.window(entity.id, {}, [
    host.field(entity.id + ':value', { entity: entity.id, map: 'number', name: 'value', fallback: '0' }),
  ])];
  const id = host.container('items', { container: 'items' }, render);
  assert.strictEqual(id, 'items');
  const list = host.snapshot().find((n) => n.id === 'items');
  assert.strictEqual(list.kind, 'division');
  assert.strictEqual(list.options.container, 'items');
  assert.deepStrictEqual(list.children, ['$$container:items']);
});

test('ui.container rejects bad name, args, or render', () => {
  host.clear();
  assert.throws(() => host.container('', { container: 'c' }, () => []), /mandatory name/);
  assert.throws(() => host.container('x', {}, () => []), /args.container/);
  assert.throws(() => host.container('x', { container: 'c' }, null), /render must be a function/);
});

test('ui.container expands one item per entity via expandContainers', () => {
  registered = [];
  host.clear();
  const render = (entity) => [host.window(entity.id, { w: 1 }, [
    host.text(entity.id + '-t', 'v' + entity.index),
  ])];
  host.container('items', { container: 'items' }, render);
  host.expandContainers((name) => {
    assert.strictEqual(name, 'items');
    return ['a', 'b', 'c'];
  });
  const snap = host.snapshot();
  const list = snap.find((n) => n.id === 'items');
  assert.deepStrictEqual(list.children, ['a', 'b', 'c']);
  const winA = snap.find((n) => n.id === 'a');
  assert.strictEqual(winA.kind, 'window');
  assert.strictEqual(winA.children[0], 'a-t');
  const textC = snap.find((n) => n.id === 'c-t');
  assert.strictEqual(textC.value, 'v2');
});

test('ui.container with unknown container renders zero items', () => {
  host.clear();
  const render = (entity) => [host.window(entity.id, {}, [])];
  host.container('items', { container: 'items' }, render);
  host.expandContainers(() => []);
  const list = host.snapshot().find((n) => n.id === 'items');
  assert.deepStrictEqual(list.children, []);
});

test('snapshot is ordered by declaration and deep-copied', () => {
  host.clear();
  host.text('a', '1');
  host.text('b', '2');
  const snap = host.snapshot();
  assert.deepStrictEqual(snap.map((n) => n.id), ['a', 'b']);
  snap[0].value = 'mutated';
  assert.strictEqual(host.snapshot()[0].value, '1');
});

console.log(`# ${passed} tests passed`);
