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

test('ui.text registers a text node and returns its id handle', () => {
  registered = [];
  host.clear();
  const h = host.text('spine-text', 'spine');
  assert.strictEqual(h.value, 'spine-text');
  assert.strictEqual(String(h), 'spine-text');
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
  assert.strictEqual(d.value, 'spine-div');
  assert.deepStrictEqual(registered[1].children, ['spine-text']);
  // A standalone division is a no-op: under a window it reaches the view.
  host.window('spine-win', {}, [d]);
  const snap = host.snapshot();
  assert.strictEqual(snap.length, 3);
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
  assert.strictEqual(id.value, 'hp-field');
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
  assert.strictEqual(id.value, 'spine-img');
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

test('ui.progress registers a bound progress node and carries steps in the options', () => {
  registered = [];
  host.clear();
  const id = host.progress('spine-prog', {
    progress: {
      fill: 'art/fill.png', track: 'art/track.png', orientation: 'vertical',
      entity: 'ent-a',
      value: (e) => e.number_map.get('hp').orElse(0),
      max: (e) => e.number_map.get('max').orElse(100),
      steps: 50,
    },
  });
  assert.strictEqual(id.value, 'spine-prog');
  assert.strictEqual(registered.length, 1);
  assert.deepStrictEqual(registered[0], {
    id: 'spine-prog',
    kind: 'progress',
    value: 0,
    binding: { entity: 'ent-a', map: 'number', name: 'hp', fallback: 0, maxName: 'max', maxFallback: 100, minFallback: 0 },
    options: { panel: {}, fill: 'art/fill.png', track: 'art/track.png', orientation: 'vertical', steps: 50 },
    children: [],
  });
});

test('ui.progress defaults steps to 100 and captures the orElse defaults as raw-value fallbacks', () => {
  registered = [];
  host.clear();
  const id = host.progress('hp-bar', {
    progress: {
      fill: 'art/fill.png', entity: 'ent-a',
      value: (e) => e.number_map.get('hp').orElse(0),
      max: (e) => e.number_map.get('max').orElse(100),
    },
  });
  assert.strictEqual(id.value, 'hp-bar');
  assert.deepStrictEqual(registered[0], {
    id: 'hp-bar',
    kind: 'progress',
    value: 0,
    binding: { entity: 'ent-a', map: 'number', name: 'hp', fallback: 0, maxName: 'max', maxFallback: 100, minFallback: 0 },
    options: { panel: {}, fill: 'art/fill.png', orientation: 'horizontal', steps: 100 },
    children: [],
  });
});

test('ui.progress resolves a value lambda via the entity number_map api', () => {
  registered = [];
  host.clear();
  let sawEntity = null;
  const id = host.progress('hp-lambda', {
    progress: {
      fill: 'art/fill.png', entity: 'ent-a',
      value: (entity) => { sawEntity = entity.id; return entity.number_map.get('hp').orElse(0); },
      max: (e) => e.number_map.get('max').orElse(100),
    },
  });
  assert.strictEqual(id.value, 'hp-lambda');
  assert.strictEqual(sawEntity, 'ent-a');
  assert.deepStrictEqual(registered[0].binding, { entity: 'ent-a', map: 'number', name: 'hp', fallback: 0, maxName: 'max', maxFallback: 100, minFallback: 0 });
});

test('ui.progress captures the value orElse default as the raw-value fallback', () => {
  registered = [];
  host.clear();
  const id = host.progress('hp-orElse', {
    progress: {
      fill: 'a.png', entity: 'ent-a',
      value: (entity) => entity.number_map.get('hp').orElse(0.5),
      max: (e) => e.number_map.get('max').orElse(100),
    },
  });
  assert.strictEqual(id.value, 'hp-orElse');
  assert.strictEqual(registered[0].binding.name, 'hp');
  assert.strictEqual(registered[0].binding.fallback, 0.5);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: () => 'x', max: (e) => e.number_map.get('m').orElse(1) } }), /options.progress.value lambda/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: () => NaN, max: (e) => e.number_map.get('m').orElse(1) } }), /options.progress.value lambda/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: () => { throw new Error('boom'); }, max: (e) => e.number_map.get('m').orElse(1) } }), /lambda threw/);
});

test('ui.progress accepts constant lambdas and omits the per-tick binding keys', () => {
  registered = [];
  host.clear();
  const id = host.progress('const-prog', {
    progress: {
      fill: 'art/fill.png', entity: 'ent-a',
      value: (e) => 3,
      max: (e) => 10,
      min: (e) => 0,
    },
  });
  assert.strictEqual(id.value, 'const-prog');
  // Constant fields carry their value as the fallback and omit the key, so
  // the engine keeps the value instead of re-reading the entity each tick.
  assert.deepStrictEqual(registered[0].binding, {
    entity: 'ent-a', map: 'number',
    fallback: 3, maxFallback: 10, minFallback: 0,
  });
  assert.strictEqual(registered[0].binding.name, undefined);
  assert.strictEqual(registered[0].binding.maxName, undefined);
  assert.strictEqual(registered[0].binding.minName, undefined);
  // A mix of constant and field-bound fields is fine too.
  registered = [];
  host.clear();
  host.progress('mixed-prog', {
    progress: {
      fill: 'a.png', entity: 'ent-a',
      value: (e) => e.number_map.get('hp').orElse(0),
      max: (e) => 100,
    },
  });
  assert.deepStrictEqual(registered[0].binding, {
    entity: 'ent-a', map: 'number', name: 'hp',
    fallback: 0, maxFallback: 100, minFallback: 0,
  });
  assert.strictEqual(registered[0].binding.maxName, undefined);
});

test('ui.progress resolves min/max lambdas into binding keys and captures their orElse defaults', () => {
  registered = [];
  host.clear();
  const id = host.progress('range-lambda', {
    progress: {
      fill: 'a.png', entity: 'ent-a',
      value: (e) => e.number_map.get('hp').orElse(0),
      min: (e) => e.number_map.get('lo').orElse(2),
      max: (e) => e.number_map.get('hi').orElse(10),
    },
  });
  assert.strictEqual(id.value, 'range-lambda');
  assert.deepStrictEqual(registered[0].binding, {
    entity: 'ent-a', map: 'number', name: 'hp', fallback: 0,
    maxName: 'hi', maxFallback: 10, minName: 'lo', minFallback: 2,
  });
  assert.strictEqual(registered[0].options.min, undefined);
  assert.strictEqual(registered[0].options.max, undefined);
});

test('ui.progress requires entity, value, and max; min must be a lambda', () => {
  host.clear();
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png' } }), /options.progress.entity is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e' } }), /options.progress.value is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: (e) => e.number_map.get('hp').orElse(0) } }), /options.progress.max is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: (e) => e.number_map.get('hp').orElse(0), max: 'x' } }), /options.progress.max is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: (e) => e.number_map.get('hp').orElse(0), max: (e) => e.number_map.get('m').orElse(1), min: 5 } }), /options.progress.min must be a lambda/);
});

test('ui.progress passes an animation reference through for fill and track', () => {
  registered = [];
  host.clear();
  // A named animation reference (the panel-background shape) is carried
  // through intact so the renderer can advance its frames per tick.
  const id = host.progress('anim-prog', {
    progress: {
      fill: { name: 'fill-anim', duration: 2, loop: true },
      track: 'art/track.png',
      entity: 'ent-a',
      value: (e) => e.number_map.get('hp').orElse(0),
      max: (e) => e.number_map.get('max').orElse(100),
    },
  });
  assert.strictEqual(id.value, 'anim-prog');
  assert.deepStrictEqual(registered[0], {
    id: 'anim-prog',
    kind: 'progress',
    value: 0,
    binding: { entity: 'ent-a', map: 'number', name: 'hp', fallback: 0, maxName: 'max', maxFallback: 100, minFallback: 0 },
    options: {
      panel: {},
      fill: { name: 'fill-anim', duration: 2, loop: true },
      track: 'art/track.png',
      orientation: 'horizontal',
      steps: 100,
    },
    children: [],
  });
});

test('ui.progress accepts the setAnimation handle as fill and normalizes it to a named ref', () => {
  globalThis.__registeredAnimations = {
    'fill-anim': { frames: [{ sprite: 'art/f1.png' }, { sprite: 'art/f2.png' }], duration: 2, loop: true },
  };
  registered = [];
  host.clear();
  const handle = {
    name: 'fill-anim', value: 'fill-anim',
    toString: () => 'fill-anim', toJSON: () => 'fill-anim',
  };
  const id = host.progress('prog-handle', {
    progress: {
      fill: handle,
      entity: 'ent-a',
      value: (e) => e.number_map.get('hp').orElse(0),
      max: (e) => e.number_map.get('max').orElse(100),
    },
  });
  assert.strictEqual(id.value, 'prog-handle');
  assert.deepStrictEqual(registered[0].options.fill, { name: 'fill-anim', duration: 2, loop: true });
  delete globalThis.__registeredAnimations;
});

test('ui.window normalizes a setAnimation handle background to a named ref', () => {
  globalThis.__registeredAnimations = {
    background: { frames: [{ sprite: 'art/bg.png' }], duration: 4 },
  };
  registered = [];
  host.clear();
  const handle = {
    name: 'background', value: 'background',
    toString: () => 'background', toJSON: () => 'background',
  };
  host.window('bg-handle', { panel: { x: 0, y: 0, width: 10, height: 10, background: handle } });
  assert.deepStrictEqual(registered[0].options.panel.background, { name: 'background', duration: 4, loop: false });
  delete globalThis.__registeredAnimations;
});

test('hover and border resolve a named animation ref to the registration first frame', () => {
  globalThis.__registeredAnimations = {
    hover: { frames: [{ sprite: 'art/hover.png' }], duration: 1, loop: false },
    border: { frames: ['art/b1.png', 'art/b2.png'], duration: 1, loop: false },
  };
  registered = [];
  host.clear();
  host.window('ref-hover', {
    panel: {
      x: 0, y: 0, width: 10, height: 10,
      onHover: { texture: { name: 'hover' }, thickness: 2 },
      border: { width: 1, texture: { name: 'border' } },
    },
  });
  assert.deepStrictEqual(registered[0].options.panel.onHover.texture, 'art/hover.png');
  assert.deepStrictEqual(registered[0].options.panel.border.texture, 'art/b1.png');
  delete globalThis.__registeredAnimations;
});

test('background and fill reject a full registration object', () => {
  host.clear();
  const reg = { frames: [{ sprite: 'art/a.png' }], duration: 1, loop: false };
  assert.throws(() => host.window('bg-frames', { panel: { x: 0, width: 1, height: 1, background: reg } }),
    /not a registration object/);
  assert.throws(() => host.progress('p-frames', {
    progress: {
      fill: reg,
      entity: 'e',
      value: (e) => e.number_map.get('hp').orElse(0),
      max: (e) => e.number_map.get('max').orElse(1),
    },
  }), /not a registration object/);
});

test('ui.progress rejects missing fill, bad orientation, bad steps, or missing entity/value/max', () => {
  host.clear();
  assert.throws(() => host.progress('p', {}), /options.progress is required/);
  assert.throws(() => host.progress('p', null), /options must be an object/);
  assert.throws(() => host.progress('p', { progress: {} }), /options.progress.fill/);
  assert.throws(() => host.progress('p', { progress: { fill: '' } }), /options.progress.fill/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', track: '' } }), /options.progress.track/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', orientation: 'z' } }), /options.progress.orientation/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png' } }), /options.progress.entity is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e' } }), /options.progress.value is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: (e) => e.number_map.get('hp').orElse(0) } }), /options.progress.max is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: 'hp' } }), /options.progress.value is required/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: (e) => e.number_map.get('hp').orElse(0), max: (e) => e.number_map.get('m').orElse(1), min: 0 } }), /options.progress.min must be a lambda/);
  assert.throws(() => host.progress('p', { progress: { fill: 'a.png', entity: 'e', value: (e) => e.number_map.get('hp').orElse(0), max: (e) => e.number_map.get('m').orElse(1), steps: 1.5 } }), /options.progress.steps/);
});

test('ui.entityList registers a division node with a container marker child', () => {
  registered = [];
  host.clear();
  const render = (entity) => [host.window(entity.id, {}, [
    host.field(entity.id + ':value', { entity: entity.id, map: 'number', name: 'value', fallback: '0' }),
  ])];
  const id = host.entityList('items', { container: 'items' }, render);
  assert.strictEqual(id.value, 'items');
  // A standalone list is a no-op: nest it under a window for the view.
  host.window('items-win', {}, [id]);
  const list = host.snapshot().find((n) => n.id === 'items');
  assert.strictEqual(list.kind, 'division');
  assert.strictEqual(list.options.container, 'items');
  assert.deepStrictEqual(list.children, ['$$container:items']);
});

test('ui.entityList rejects bad name, args, or render', () => {
  host.clear();
  assert.throws(() => host.entityList('', { container: 'c' }, () => []), /mandatory name/);
  assert.throws(() => host.entityList('x', {}, () => []), /args.container/);
  assert.throws(() => host.entityList('x', { container: 'c' }, null), /render must be a function/);
});

test('ui.entityList expands one item per entity via expandContainers', () => {
  registered = [];
  host.clear();
  const render = (entity) => [host.window(entity.id, { w: 1 }, [
    host.text(entity.id + '-t', 'v' + entity.index),
  ])];
  host.window('items-win', {}, [host.entityList('items', { container: 'items' }, render)]);
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

test('ui.entityList with unknown container renders zero items', () => {
  host.clear();
  const render = (entity) => [host.window(entity.id, {}, [])];
  host.window('items-win', {}, [host.entityList('items', { container: 'items' }, render)]);
  host.expandContainers(() => []);
  const list = host.snapshot().find((n) => n.id === 'items');
  assert.deepStrictEqual(list.children, []);
});

test('ui.containerView registers a window node with view options and a view marker', () => {
  host.clear();
  const id = host.containerView('world', { container: 'grid-1', panel: { width: 700, height: 500, x: 10, y: 80 } },
    (entity) => host.window(entity.id, { background: 'tex' }, []));
  assert.strictEqual(id.value, 'world');
  const view = host.snapshot().find((n) => n.id === 'world');
  assert.strictEqual(view.kind, 'window');
  assert.strictEqual(view.options.container, 'grid-1');
  assert.strictEqual(view.options.viewWidth, 700);
  assert.strictEqual(view.options.viewHeight, 500);
  assert.strictEqual(view.options.panel.x, 10);
  assert.strictEqual(view.options.panel.y, 80);
  assert.deepStrictEqual(view.children, ['$$containerView:world']);
});

test('ui.containerView rejects bad name, args, or render', () => {
  host.clear();
  assert.throws(() => host.containerView('', { container: 'c', panel: { width: 10, height: 10 } }, () => 'x'), /mandatory name/);
  assert.throws(() => host.containerView('v', null, () => 'x'), /args must be an object/);
  assert.throws(() => host.containerView('v', { container: '', panel: { width: 10, height: 10 } }, () => 'x'), /args.container/);
  assert.throws(() => host.containerView('v', { container: 'c', panel: { width: 0, height: 10 } }, () => 'x'), /panel.width/);
  assert.throws(() => host.containerView('v', { container: 'c', panel: { width: 10, height: 0 } }, () => 'x'), /panel.height/);
  assert.throws(() => host.containerView('v', { container: 'c', panel: { width: 10, height: 10 } }, null), /render must be a function/);
});

test('ui.containerView expands one item per entity and stamps options.entity', () => {
  host.clear();
  const render = (entity) => host.window(entity.id, { background: 'tex' }, []);
  host.containerView('world', { container: 'grid-1', panel: { width: 700, height: 500 } }, render);
  host.expandContainerViews((cid) => {
    assert.strictEqual(cid, 'grid-1');
    return ['a', 'b'];
  });
  const snap = host.snapshot();
  const view = snap.find((n) => n.id === 'world');
  assert.deepStrictEqual(view.children, ['a', 'b']);
  const itemA = snap.find((n) => n.id === 'a');
  assert.strictEqual(itemA.options.entity, 'a');
  const itemB = snap.find((n) => n.id === 'b');
  assert.strictEqual(itemB.options.entity, 'b');
});

test('ui.containerView with unknown container renders zero items', () => {
  host.clear();
  host.containerView('world', { container: 'grid-1', panel: { width: 700, height: 500 } },
    (entity) => host.window(entity.id, {}, []));
  host.expandContainerViews(() => []);
  const view = host.snapshot().find((n) => n.id === 'world');
  assert.deepStrictEqual(view.children, []);
});

test('ui.containerView reset restores the view marker', () => {
  host.clear();
  host.containerView('world', { container: 'grid-1', panel: { width: 700, height: 500 } },
    (entity) => host.window(entity.id, {}, []));
  host.expandContainerViews(() => ['a']);
  let view = host.snapshot().find((n) => n.id === 'world');
  assert.deepStrictEqual(view.children, ['a']);
  host.resetContainers();
  view = host.snapshot().find((n) => n.id === 'world');
  assert.deepStrictEqual(view.children, ['$$containerView:world']);
});

test('snapshot is ordered by declaration and deep-copied', () => {
  host.clear();
  const a = host.text('a', '1');
  const b = host.text('b', '2');
  host.window('w', {}, [a, b]);
  const snap = host.snapshot();
  assert.deepStrictEqual(snap.map((n) => n.id), ['a', 'b', 'w']);
  snap[0].value = 'mutated';
  assert.strictEqual(host.snapshot()[0].value, '1');
});

test('standalone nodes are a no-op: registered but never added to the view', () => {
  registered = [];
  host.clear();
  host.text('orphan-text', 'orphan');
  host.div('orphan-div', {}, [host.text('orphan-nested', 'x')]);
  host.window('real-win', {}, [host.text('win-text', 'in')]);
  // All five nodes register...
  assert.strictEqual(registered.length, 5);
  // ...but only the window subtree reaches the view (declaration order).
  const snap = host.snapshot();
  assert.deepStrictEqual(snap.map((n) => n.id), ['win-text', 'real-win']);
});

test('ui.window carries the title option through the panel options', () => {
  registered = [];
  host.clear();
  host.window('titled-win', { panel: { x: 0, y: 0, width: 100, height: 60, title: 'My Window' } });
  assert.strictEqual(registered.length, 1);
  assert.strictEqual(registered[0].kind, 'window');
  assert.strictEqual(registered[0].options.panel.title, 'My Window');
  const snap = host.snapshot().find((n) => n.id === 'titled-win');
  assert.strictEqual(snap.options.panel.title, 'My Window');
});

test('panel/window handles expose addChild to append children after creation', () => {
  registered = [];
  host.clear();
  const a = host.text('ac-a', 'a');
  const b = host.text('ac-b', 'b');
  const w = host.window('ac-win', {});
  assert.strictEqual(typeof w.addChild, 'function');
  const ret = w.addChild(a);
  assert.strictEqual(ret, w); // chainable: returns the same handle
  w.addChild(b);
  const d = host.div('ac-div', {});
  assert.strictEqual(typeof d.addChild, 'function');
  d.addChild('ac-raw'); // raw id strings are accepted too
  host.window('ac-parent', {}, [d]);
  const snap = host.snapshot();
  assert.deepStrictEqual(snap.find((n) => n.id === 'ac-win').children, ['ac-a', 'ac-b']);
  assert.deepStrictEqual(snap.find((n) => n.id === 'ac-div').children, ['ac-raw']);
  // A child added after a snapshot is picked up by the next one.
  w.addChild('ac-late');
  assert.deepStrictEqual(host.snapshot().find((n) => n.id === 'ac-win').children, ['ac-a', 'ac-b', 'ac-late']);
  // Leaf handles do not carry addChild.
  assert.strictEqual(host.text('ac-leaf', 'x').addChild, undefined);
});

test('addChild rejects non-child values and throws once the node is cleared', () => {
  host.clear();
  const w = host.window('ac2-win', {});
  assert.throws(() => w.addChild(42), /children must be node ids/);
  assert.throws(() => w.addChild(''), /children must be node ids/);
  host.clear();
  assert.throws(() => w.addChild('x'), /addChild on unknown node/);
});

test('ui.setScreen registers a screen node with nested screen options and returns its handle', () => {
  registered = [];
  host.clear();
  const t = host.text('sc-text', 'game');
  const s = host.setScreen({ screen: { background: 'art/screen.png' } }, [t]);
  assert.strictEqual(s.value, '__ui-screen-1');
  assert.strictEqual(typeof s.addChild, 'function');
  assert.deepStrictEqual(registered[1], {
    id: '__ui-screen-1',
    kind: 'screen',
    options: { screen: { background: 'art/screen.png' } },
    children: ['sc-text'],
  });
  // The screen is a top-level surface: its subtree reaches the view.
  const snap = host.snapshot();
  assert.deepStrictEqual(snap.map((n) => n.id), ['sc-text', '__ui-screen-1']);
  const sc = snap.find((n) => n.id === '__ui-screen-1');
  assert.strictEqual(sc.kind, 'screen');
  assert.strictEqual(sc.options.screen.background, 'art/screen.png');
});

test('ui.setScreen replaces the current screen: only the latest screen reaches the view', () => {
  registered = [];
  host.clear();
  host.setScreen({ screen: { background: 'a.png' } }, [host.text('sc-a', 'a')]);
  host.window('sc-win', {}, [host.text('sc-in-win', 'w')]);
  host.setScreen({ screen: { background: 'b.png' } }, [host.text('sc-b', 'b')]);
  // Both screens registered...
  assert.strictEqual(registered.filter((n) => n.kind === 'screen').length, 2);
  // ...but only the latest screen (and the window subtree) is in the view.
  const snap = host.snapshot();
  assert.deepStrictEqual(snap.map((n) => n.id), ['sc-in-win', 'sc-win', 'sc-b', '__ui-screen-2']);
  // A third setScreen replaces again; ids never collide.
  host.setScreen({ screen: { background: 'c.png' } });
  const ids = host.snapshot().map((n) => n.id);
  assert.ok(ids.includes('__ui-screen-3'));
  assert.ok(!ids.includes('__ui-screen-2'));
  assert.ok(!ids.includes('__ui-screen-1'));
});

test('ui.setScreen handle exposes addChild and clear resets the screen ids', () => {
  host.clear();
  const s = host.setScreen({ screen: {} });
  s.addChild(host.text('sc-late', 'late'));
  assert.deepStrictEqual(
    host.snapshot().find((n) => n.id === '__ui-screen-1').children, ['sc-late']);
  // After clear the id sequence restarts: re-declaring does not throw.
  host.clear();
  host.setScreen({ screen: {} });
  assert.strictEqual(host.snapshot()[0].id, '__ui-screen-1');
});

test('ui.setScreen normalizes an animation handle background to a named ref', () => {
  globalThis.__registeredAnimations = {
    'screen-anim': { frames: [{ sprite: 'art/s1.png' }], duration: 3 },
  };
  registered = [];
  host.clear();
  const handle = {
    name: 'screen-anim', value: 'screen-anim',
    toString: () => 'screen-anim', toJSON: () => 'screen-anim',
  };
  host.setScreen({ screen: { background: handle } });
  assert.deepStrictEqual(registered[0].options.screen.background, { name: 'screen-anim', duration: 3, loop: false });
  delete globalThis.__registeredAnimations;
});

test('ui.setScreen rejects non-child values in children', () => {
  host.clear();
  assert.throws(() => host.setScreen({ screen: {} }, [42]), /children must be node ids/);
  assert.throws(() => host.setScreen({ screen: {} }, ['']), /children must be node ids/);
});

console.log(`# ${passed} tests passed`);
