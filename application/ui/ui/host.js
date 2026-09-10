(function (root, factory) {
    if (typeof module === 'object' && module.exports) {
        module.exports = factory(root);
    } else {
        root.__uiHost = factory(root);
    }
})(typeof globalThis !== 'undefined' ? globalThis : this,
function (root) {
    'use strict';

    var nodes = Object.create(null);
    var order = [];
    // Render lambdas for ui.container lists, keyed by list name. The engine
    // re-invokes these per entity during expansion (see expandContainers).
    var containerRenders = Object.create(null);
    // Item node ids materialized per container list by the last expansion, so
    // resetContainers can drop them and restore the list's marker between
    // engine ticks.
    var containerItems = Object.create(null);

    function transport() {
        var t = root.__uiTransport;
        if (!t || typeof t.registerNode !== 'function') {
            throw new Error('ui: transport not installed on globalThis.__uiTransport');
        }
        return t;
    }

    function requireId(id, kind) {
        if (typeof id !== 'string' || id.length === 0) {
            throw new Error('ui: mandatory id missing for ' + kind + ' node');
        }
        if (nodes[id]) {
            throw new Error('ui: duplicate id "' + id + '" for ' + kind + ' node');
        }
    }

    function childIds(children) {
        return (children || []).map(function (c) {
            if (typeof c !== 'string' || c.length === 0) {
                throw new Error('ui: children must be node ids returned by ui.* factories');
            }
            return c;
        });
    }

    function isMarker(c) {
        return typeof c === 'string' && c.indexOf('$$container:') === 0;
    }

    // Hover visuals must carry a concrete texture reference (an archive path
    // or a sprite map ref) that the renderer can load directly; animation
    // registrations ({ frames: [...] }) resolve to their first frame's
    // reference.
    function firstFrameRef(v) {
        if (typeof v === 'string' && v.length > 0) return v;
        if (!v || typeof v !== 'object' || !Array.isArray(v.frames)) return null;
        for (var i = 0; i < v.frames.length; i++) {
            var f = v.frames[i];
            if (typeof f === 'string' && f.length > 0) return f;
            if (f && typeof f === 'object' && f.sprite != null) {
                var s = f.sprite;
                if (typeof s === 'string' && s.length > 0) return s;
                if (typeof s === 'object' && s.__spriteMap) {
                    return JSON.stringify({
                        __spriteMap: true,
                        map: s.map,
                        layers: s.layers || []
                    });
                }
                if (typeof s === 'object' && typeof s.name === 'string') {
                    return s.name;
                }
            }
        }
        return null;
    }

    // A background may be a registered animation object (the registry
    // reference itself, with no name of its own). The renderer consumes a
    // named reference { name, duration, loop }, so resolve the object back to
    // its registry key by reference equality (ui.getAnimation returns the
    // stored object itself).
    function backgroundRef(v) {
        if (typeof v === 'string') return v;
        if (!v || typeof v !== 'object') return v;
        if (typeof v.name === 'string') return v;
        var store = root.__registeredAnimations || {};
        for (var k in store) {
            if (store[k] === v) {
                return {
                    name: k,
                    duration: typeof v.duration === 'number' ? v.duration : 1,
                    loop: v.loop === true
                };
            }
        }
        return v;
    }

    function normalizeOptions(options) {
        if (!options || typeof options !== 'object') return options;
        if (options.background == null) return options;
        var bg = backgroundRef(options.background);
        if (bg === options.background) return options;
        var copy = {};
        for (var k in options) {
            if (Object.prototype.hasOwnProperty.call(options, k)) copy[k] = options[k];
        }
        copy.background = bg;
        return copy;
    }

    function resolveHoverOptions(options) {
        if (!options || typeof options !== 'object') return options;
        var h = options.onHover;
        if (!h || typeof h !== 'object') return options;
        var needCopy = (h.texture != null && typeof h.texture !== 'string')
            || (h.background != null && typeof h.background !== 'string');
        if (!needCopy) return options;
        var copy = {};
        for (var k in options) {
            if (Object.prototype.hasOwnProperty.call(options, k)) copy[k] = options[k];
        }
        copy.onHover = {
            texture: h.texture,
            thickness: h.thickness,
            background: h.background,
            emitAction: h.emitAction,
            stopPropagation: h.stopPropagation
        };
        if (copy.onHover.texture != null && typeof copy.onHover.texture !== 'string') {
            copy.onHover.texture = firstFrameRef(copy.onHover.texture);
        }
        if (copy.onHover.background != null && typeof copy.onHover.background !== 'string') {
            copy.onHover.background = firstFrameRef(copy.onHover.background);
        }
        return copy;
    }

    // A border's texture must carry a concrete texture reference (like the
    // hover visuals); an animation registration ({ frames: [...] }) resolves
    // to its first frame's reference.
    function resolveBorderOptions(options) {
        if (!options || typeof options !== 'object') return options;
        var b = options.border;
        if (!b || typeof b !== 'object') return options;
        if (b.texture == null || typeof b.texture === 'string') return options;
        var copy = {};
        for (var k in options) {
            if (Object.prototype.hasOwnProperty.call(options, k)) copy[k] = options[k];
        }
        var border = {};
        for (var bk in b) {
            if (Object.prototype.hasOwnProperty.call(b, bk)) border[bk] = b[bk];
        }
        border.texture = firstFrameRef(b.texture);
        copy.border = border;
        return copy;
    }

    /// Marks the children slot of a container list with `name`: the engine
    /// replaces this marker with the ids the render lambda declares, one item
    /// per entity of the container.
    function containerMarker(name) {
        return '$$container:' + name;
    }

    /// Replaces the marker in each node's children with the ids the stored
    /// render lambda declared for that node's container entities. The render
    /// lambda is declarative: re-invoking it per entity re-registers the same
    /// item node ids, so the engine's id-diff reconciles add/update/remove.
    /// `entitiesFor(name)` is injected by the engine and must return an array
    /// of entity ids for the list's container (empty array when unknown).
    function expandContainers(entitiesFor) {
        for (var i = 0; i < order.length; i++) {
            var node = nodes[order[i]];
            if (!node || node.kind !== 'division') continue;
            if (!Array.isArray(node.children)) continue;
            var changed = false;
            for (var j = 0; j < node.children.length; j++) {
                var c = node.children[j];
                if (!isMarker(c)) continue;
                var name = c.slice('$$container:'.length);
                var render = containerRenders[name];
                var entities = (typeof entitiesFor === 'function' && render)
                    ? entitiesFor(name) : [];
                var itemIds = [];
                if (render && Array.isArray(entities)) {
                    for (var e = 0; e < entities.length; e++) {
                        var result = render({ id: entities[e], index: e });
                        if (result == null) continue;
                        var arr = Array.isArray(result) ? result : [result];
                        for (var k = 0; k < arr.length; k++) itemIds.push(arr[k]);
                    }
                }
                containerItems[name] = itemIds;
                for (var m = itemIds.length - 1; m >= 0; m--) {
                    node.children.splice(j, 0, itemIds[m]);
                }
                node.children.splice(j + itemIds.length, 1);
                changed = true;
                break;
            }
            if (changed) {
                // Children were replaced; re-scan this node in case the render
                // declared further markers (nested container lists).
                i--;
            }
        }
    }

    /// Drops the node with `id` and every node in its subtree from the
    /// registries (used to undo a container list's materialized items).
    function removeSubtree(id) {
        var node = nodes[id];
        if (!node) return;
        if (Array.isArray(node.children)) {
            for (var i = 0; i < node.children.length; i++) {
                removeSubtree(node.children[i]);
            }
        }
        delete nodes[id];
        var idx = order.indexOf(id);
        if (idx >= 0) order.splice(idx, 1);
    }

    /// Undoes the last expansion: drops every materialized container item and
    /// restores each list node's `$$container:` marker, so the engine can
    /// re-expand against a fresh entity list on the next tick.
    function resetContainers() {
        for (var name in containerRenders) {
            var items = containerItems[name];
            if (items) {
                for (var i = 0; i < items.length; i++) removeSubtree(items[i]);
            }
            containerItems[name] = [];
            var list = nodes[name];
            if (list && Array.isArray(list.children)) {
                list.children = [containerMarker(name)];
            }
        }
    }

    function register(node) {
        requireId(node.id, node.kind);
        nodes[node.id] = node;
        order.push(node.id);
        try {
            transport().registerNode(JSON.parse(JSON.stringify(node)));
        } catch (e) {
            // Transport is optional at declaration time in tests; the
            // persistent engine reads the snapshot directly.
        }
        return node.id;
    }

    var api = {
        div: function (id, options, children) {
            var opts = {};
            if (options && typeof options === 'object') {
                for (var k in options) { if (Object.prototype.hasOwnProperty.call(options, k)) opts[k] = options[k]; }
            }
            return register({
                id: id,
                kind: 'division',
                options: resolveBorderOptions(resolveHoverOptions(normalizeOptions(opts))),
                children: childIds(children)
            });
        },
        window: function (id, options, children) {
            var opts = {};
            if (options && typeof options === 'object') {
                for (var k in options) { if (Object.prototype.hasOwnProperty.call(options, k)) opts[k] = options[k]; }
            }
            return register({
                id: id,
                kind: 'window',
                options: resolveBorderOptions(resolveHoverOptions(normalizeOptions(opts))),
                children: childIds(children)
            });
        },
        canvas: function (id, options, children) {
            var opts = {};
            if (options && typeof options === 'object') {
                for (var k in options) { if (Object.prototype.hasOwnProperty.call(options, k)) opts[k] = options[k]; }
            }
            return register({
                id: id,
                kind: 'canvas',
                options: opts,
                children: childIds(children)
            });
        },
        text: function (id, value) {
            return register({
                id: id,
                kind: 'text',
                value: typeof value === 'string' ? value : String(value),
                children: []
            });
        },
        image: function (id, src) {
            requireId(id, 'image');
            if (typeof src !== 'string' || src.length === 0) {
                throw new Error('ui: image src must be a non-empty archive path string');
            }
            return register({
                id: id,
                kind: 'image',
                src: src,
                children: []
            });
        },
        container: function (name, args, render) {
            if (typeof name !== 'string' || name.length === 0) {
                throw new Error('ui: container mandatory name missing');
            }
            if (!args || typeof args !== 'object') {
                throw new Error('ui: container args must be an object with container');
            }
            if (typeof args.container !== 'string' || args.container.length === 0) {
                throw new Error('ui: container args.container must be a non-empty container id');
            }
            if (typeof render !== 'function') {
                throw new Error('ui: container render must be a function(entity) => nodeIds');
            }
            containerRenders[name] = render;
            return register({
                id: name,
                kind: 'division',
                options: { container: args.container },
                children: [containerMarker(name)]
            });
        },
        field: function (id, binding) {
            if (!binding || typeof binding !== 'object') {
                throw new Error('ui: field binding must be an object with entity, map, name');
            }
            if (typeof binding.entity !== 'string' || binding.entity.length === 0) {
                throw new Error('ui: field binding.entity must be a non-empty entity id');
            }
            if (binding.map !== 'number' && binding.map !== 'text') {
                throw new Error('ui: field binding.map must be "number" or "text"');
            }
            if (typeof binding.name !== 'string' || binding.name.length === 0) {
                throw new Error('ui: field binding.name must be a non-empty field name');
            }
            var fallback = typeof binding.fallback === 'string' ? binding.fallback : '';
            return register({
                id: id,
                kind: 'field',
                binding: {
                    entity: binding.entity,
                    map: binding.map,
                    name: binding.name,
                    fallback: fallback
                },
                value: fallback,
                children: []
            });
        },
        snapshot: function () {
            var out = [];
            for (var i = 0; i < order.length; i++) {
                out.push(JSON.parse(JSON.stringify(nodes[order[i]])));
            }
            return out;
        },
        clear: function () {
            nodes = Object.create(null);
            order = [];
            containerRenders = Object.create(null);
            containerItems = Object.create(null);
        },
        loadSnapshot: function (arr) {
            nodes = Object.create(null);
            order = [];
            containerItems = Object.create(null);
            (arr || []).forEach(function (n) {
                requireId(n.id, n.kind);
                nodes[n.id] = n;
                order.push(n.id);
            });
        },
        expandContainers: function (entitiesFor) {
            expandContainers(entitiesFor);
        },
        resetContainers: function () {
            resetContainers();
        }
    };
    return api;
});
