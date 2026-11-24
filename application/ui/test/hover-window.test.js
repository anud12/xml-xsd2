'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const host = require('../ui/host.js');

test('hover window marker and lifecycle', () => {
  host.clear();
  const registrations = [];
  globalThis.__uiTransport = { registerNode: n => registrations.push(n) };
  let calls = 0;
  host.div('source', { panel: { onHover: {
    thickness: 4, emitAction: 'existing', window: ctx => {
      calls++;
      assert.deepEqual(ctx, { node: { id: 'source' }, window: { id: 'parent' }, cursor: undefined, rect: undefined, cell: undefined });
      return { options: { panel: { x: 5, pinnable: { key: 'P' } } }, children: [] };
    }
  } } }, []);
  assert.deepEqual(registrations[0].options.panel.onHover,
    { thickness: 4, emitAction: 'existing', __jsHandler: 'hoverWindow' });
  const args = { node: 'source', window: 'parent' };
  host.hoverWindowEvent('ui:hover-window:enter', args);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.equal(calls, 1);
  assert.equal(registrations[1].id, 'source:hover-window');
  assert.deepEqual(registrations[1].options.panel.pinnable, { key: 'P' });
  host.hoverWindowEvent('ui:hover-window:exit', args);
  assert.deepEqual(host.snapshot().map(n => n.id), ['source']);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.equal(calls, 2);
  host.clear();
});

test('pinned exit retains declaration, re-entry is idempotent, inactive unpin removes it', () => {
  host.clear();
  const args = { node: 'source', window: 'parent' };
  let calls = 0;
  host.div('source', { panel: { onHover: { window: () => {
    calls++;
    return { options: { panel: {} }, children: [] };
  } } } }, []);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  const pin = { node: 'source', window: 'source:hover-window', pinned: true };
  host.hoverWindowPinned(pin);
  host.hoverWindowEvent('ui:hover-window:exit', args);
  assert.equal(calls, 1);
  assert.deepEqual(host.snapshot().map(n => n.id), ['source', 'source:hover-window']);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.equal(calls, 2);
  assert.deepEqual(host.snapshot().map(n => n.id), ['source', 'source:hover-window']);
  host.hoverWindowEvent('ui:hover-window:exit', args);
  host.hoverWindowPinned({ ...pin, pinned: false });
  assert.deepEqual(host.snapshot().map(n => n.id), ['source']);
  host.clear();
});

test('unpin while active remains transient until exit; invalid cell unpin removes', () => {
  host.clear();
  const args = { node: 'source', window: 'parent', cell: { x: 1 } };
  const pin = { node: 'source', window: 'source:hover-window', pinned: true };
  host.div('source', { panel: { onHover: { window: ctx =>
    ctx.cell.x === 1 ? { options: { panel: {} }, children: [] } : null
  } } }, []);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  host.hoverWindowPinned(pin);
  host.hoverWindowPinned({ ...pin, pinned: false });
  assert.equal(host.snapshot().length, 2);
  host.hoverWindowEvent('ui:hover-window:exit', args);
  assert.equal(host.snapshot().length, 1);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  host.hoverWindowPinned(pin);
  args.cell.x = 2;
  host.hoverWindowEvent('ui:hover-window:move', args);
  assert.equal(host.snapshot().length, 2);
  host.hoverWindowPinned({ ...pin, pinned: false });
  assert.equal(host.snapshot().length, 1);
  host.clear();
});

test('reentrant exit cancels pending declaration and removed sources are ignored', () => {
  host.clear();
  const args = { node: 'source', window: 'parent' };
  host.div('source', { panel: { onHover: { window: () => {
    host.hoverWindowEvent('ui:hover-window:enter', args);
    host.hoverWindowEvent('ui:hover-window:exit', args);
    return { options: { panel: {} } };
  } } } }, []);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.deepEqual(host.snapshot().map(n => n.id), ['source']);
  host.clear();
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.deepEqual(host.snapshot(), []);
});

test('move reevaluates cursor and removes or recreates a derived window', () => {
  host.clear();
  const args = { node: 'source', window: 'parent', cursor: { x: 0, y: 1 }, rect: { x: 0, y: 0, width: 10, height: 10 } };
  host.div('source', { panel: { onHover: { window: ctx =>
    ctx.cursor.x === 1 ? { options: { panel: {} }, children: [] } : null
  } } }, []);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.deepEqual(host.snapshot().map(n => n.id), ['source']);
  args.cursor.x = 1;
  host.hoverWindowEvent('ui:hover-window:move', args);
  assert.deepEqual(host.snapshot().map(n => n.id), ['source', 'source:hover-window']);
  args.cursor.x = 2;
  host.hoverWindowEvent('ui:hover-window:move', args);
  assert.deepEqual(host.snapshot().map(n => n.id), ['source']);
  host.hoverWindowEvent('ui:hover-window:exit', args);
  host.clear();
});


test('pinned replacement prunes obsolete descendants while retaining moved nodes', () => {
  host.clear();
  let changed = false;
  const args = { node: 'source', window: 'parent' };
  host.div('source', { panel: { onHover: { window: () => {
    const keep = host.text('keep', 'live');
    if (changed) return { children: [keep, host.text('new', 'new')] };
    return { children: [host.div('old-parent', {}, [keep, host.text('obsolete', 'old')])] };
  } } } }, []);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  host.hoverWindowPinned({ node: 'source', window: 'source:hover-window', pinned: true });
  host.hoverWindowEvent('ui:hover-window:exit', args);
  changed = true;
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.deepEqual(host.snapshot().map(n => n.id).sort(), ['source', 'source:hover-window', 'keep', 'new'].sort());
  host.hoverWindowEvent('ui:hover-window:move', args);
  assert.equal(host.snapshot().length, 4);
  host.clear();
});

test('removed and recreated source retains pin state and reuses its declaration', () => {
  host.clear();
  const args = { node: 'source', window: 'parent' };
  const pin = { node: 'source', window: 'source:hover-window', pinned: true };
  const source = () => host.div('source', { panel: { onHover: { window: () => ({ children: [host.text('child', 'value')] }) } } }, []);
  host.setScreen({}, [source()]);
  host.hoverWindowEvent('ui:hover-window:enter', args);
  host.hoverWindowPinned(pin);
  host.setScreen({}, []);
  assert.ok(host.snapshot().some(n => n.id === 'source:hover-window'));
  source();
  host.hoverWindowEvent('ui:hover-window:enter', args);
  assert.equal(host.snapshot().filter(n => n.id === 'source:hover-window').length, 1);
  host.hoverWindowEvent('ui:hover-window:exit', args);
  assert.ok(host.snapshot().some(n => n.id === 'source:hover-window'));
  host.hoverWindowPinned({ ...pin, pinned: false });
  assert.ok(!host.snapshot().some(n => n.id === 'source:hover-window' || n.id === 'child'));
  host.clear();
});
