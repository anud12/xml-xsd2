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
    // Render lambdas for ui.entityList lists, keyed by list name. The engine
    // re-invokes these per entity during expansion (see expandContainers).
    // A list render returns an array of node ids; the items are flow children
    // placed by the parent's layout (insertion order).
    var containerRenders = Object.create(null);
    // Item node ids materialized per container list by the last expansion, so
    // resetContainers can drop them and restore the list's marker between
    // engine ticks.
    var containerItems = Object.create(null);
    // Render lambdas for ui.containerView views, keyed by view name. Unlike
    // container lists (which return an array of node ids), a view render
    // returns a single panel id per entity; the engine stamps each item with
    // `options.entity = entityId` so the per-tick position pass can resolve
    // its geometry from the container.
    var containerViewRenders = Object.create(null);
    // Item node ids materialized per container view by the last expansion.
    var containerViewItems = Object.create(null);
    // Render lambdas for ui.sectorGrid views, keyed by view name. The render
    // takes a cell/portal descriptor (the grid's "container expression") and
    // returns a single node id; the engine positions + sizes that node from the
    // grid geometry, and the module sets its background/content.
    var sectorGridRenders = Object.create(null);
    // Item node ids (cell + portal windows) materialized per sector grid view
    // by the last expansion, so resetContainers can drop them and restore the
    // view's marker between engine ticks.
    var sectorGridItems = Object.create(null);
    // The global screen: each setScreen call registers a fresh screen node
    // (host-owned id, so re-declarations never collide); the snapshot keeps
    // only the latest one visible — earlier screens stay registered but
    // unreachable (a no-op, like any orphan node).
    var screenSeq = 0;
    var currentScreenId = null;

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

    // Cross-structure reference edges accept either a raw id string or a
    // registered *Expression handle (the object returned by set*/world.sectorGrid).
    // A handle carries its id on `.value`/`.name`/`.id` and a `toString`; this
    // normalizes it back to the plain id string the engine stores and matches on.
    function resolveId(v) {
        if (v == null) return v;
        if (typeof v === 'string') return v;
        if (typeof v === 'object') {
            if (typeof v.value === 'string' && v.value.length > 0) return v.value;
            if (typeof v.name === 'string' && v.name.length > 0) return v.name;
            if (typeof v.id === 'string' && v.id.length > 0) return v.id;
            if (typeof v.toString === 'function') {
                var s = String(v);
                if (s.length > 0 && s !== '[object Object]') return s;
            }
        }
        return String(v);
    }

    // The *Expression handle a public factory returns for the node it
    // registered: the same shape as the set*/world.sectorGrid handles (id on
    // `.id`/`.value` plus toString/toJSON), so it flows into any id-accepting
    // slot (children, binding.entity, args.container) or stringifies back to
    // the raw id.
    function nodeHandle(id) {
        return {
            id: id,
            value: id,
            toString: function () { return id; },
            toJSON: function () { return id; }
        };
    }

    // The *Expression handle for a node with an explicit children list
    // (division/window): nodeHandle plus addChild, which appends a child
    // after the node was created. The child resolves exactly like a factory
    // children entry (a raw id or another handle); the same handle is
    // returned so calls chain.
    function parentHandle(id) {
        var h = nodeHandle(id);
        h.addChild = function (child) {
            var node = nodes[id];
            if (!node) {
                throw new Error('ui: addChild on unknown node "' + id + '"');
            }
            node.children.push(childIds([child])[0]);
            return h;
        };
        return h;
    }

    function childIds(children) {
        return (children || []).map(function (c) {
            // A child is either a raw id string or a *Expression handle (an
            // object); anything else (a number, null, ...) is rejected.
            if (typeof c === 'string') {
                if (c.length === 0) {
                    throw new Error('ui: children must be node ids returned by ui.* factories');
                }
                return c;
            }
            if (c != null && typeof c === 'object') {
                var id = resolveId(c);
                if (typeof id === 'string' && id.length > 0) return id;
            }
            throw new Error('ui: children must be node ids returned by ui.* factories');
        });
    }

    function isMarker(c) {
        return typeof c === 'string' && c.indexOf('$$container:') === 0;
    }

    // Hover visuals must carry a concrete texture reference (an archive path
    // or a sprite map ref) that the renderer can load directly; an animation
    // (a { frames: [...] } registration or a { name } ref resolved through
    // the registration store) resolves to its first frame's reference.
    function animationFrames(v) {
        if (!v || typeof v !== 'object') return null;
        if (Array.isArray(v.frames)) return v.frames;
        if (typeof v.name === 'string' && v.name.length > 0) {
            var reg = (root.__registeredAnimations || {})[v.name];
            if (reg && Array.isArray(reg.frames)) return reg.frames;
        }
        return null;
    }
    function firstFrameRef(v) {
        if (typeof v === 'string' && v.length > 0) return v;
        var frames = animationFrames(v);
        if (!frames) return null;
        for (var i = 0; i < frames.length; i++) {
            var f = frames[i];
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

    // A background is passed by reference, like a bound entity: a static
    // archive path string, or an animation reference — the setAnimation
    // handle (the object it returns), a plain { name, duration?, loop? }
    // ref, or a bare name. The renderer consumes a named reference
    // { name, duration, loop }; normalize any accepted shape to that,
    // resolving missing duration/loop from the registration store (the
    // engine re-reads the registration live every tick). Normalizing to a
    // plain object matters: the handle's toJSON degrades to a bare string
    // under the register() JSON round-trip, and a bare string encodes as a
    // static texture path, not an animation. A full registration object
    // ({ frames: [...] }) is rejected — pass the name or handle instead.
    function isAnimationRef(v) {
        return !!v && typeof v === 'object'
            && typeof v.name === 'string' && v.name.length > 0;
    }
    function backgroundRef(v) {
        if (typeof v === 'string') return v;
        if (!v || typeof v !== 'object') return v;
        if (isAnimationRef(v)) {
            var reg = (root.__registeredAnimations || {})[v.name];
            var ref = { name: v.name };
            if (typeof v.duration === 'number' && isFinite(v.duration)) {
                ref.duration = v.duration;
            } else if (reg && typeof reg.duration === 'number' && isFinite(reg.duration)) {
                ref.duration = reg.duration;
            }
            if (typeof v.loop === 'boolean') {
                ref.loop = v.loop;
            } else if (reg) {
                ref.loop = reg.loop === true;
            }
            return ref;
        }
        if (Array.isArray(v.frames)) {
            throw new Error('ui: background must be an animation name or the setAnimation handle, not a registration object');
        }
        return v;
    }

    // A progress fill/track reference: a non-empty archive path string, or a
    // named animation reference (the background shape, with a "name").
    function validProgressTexRef(v) {
        if (typeof v === 'string') return v.length > 0;
        return !!v && typeof v === 'object'
            && typeof v.name === 'string' && v.name.length > 0;
    }

    // A progress field (`value`/`min`/`max`) is a lambda
    // `(entity: EntityExpression) => NumberExpression`. It either selects a
    // field off the entity's number map (e.g.
    // `(e) => e.number_map.get("hp").orElse(0)`) or returns a constant
    // NumberExpression (e.g. `(e) => number.of(3)`). We invoke the lambda
    // against a capturing proxy whose number_map.get records the requested
    // key and whose orElse records the default. A field select is re-read
    // from the entity's number data every tick, using the captured orElse
    // default when the key is absent; a constant is used as-is. Returns
    // { key, fallback }: key null marks a constant.
    function progressFieldRef(fn, entity, label) {
        var ref = { key: null, fallback: 0 };
        var proxy = {
            id: typeof entity === 'string' ? entity : (entity && entity.id),
            number_map: {
                get: function (key) {
                    ref.key = key;
                    return { orElse: function (def) { ref.fallback = def; return def; } };
                }
            },
            text_map: { get: function () { return ''; } },
            getNumber: function () { return undefined; },
            getText: function () { return ''; }
        };
        var result = fn(proxy);
        if (typeof ref.key === 'string' && ref.key.length > 0) {
            if (typeof ref.fallback !== 'number' || !isFinite(ref.fallback)) {
                throw new Error('ui: progress options.progress.' + label + ' lambda .orElse default must be a finite number');
            }
            return ref;
        }
        if (typeof result !== 'number' || !isFinite(result)) {
            throw new Error('ui: progress options.progress.' + label + ' lambda must select a number_map field via entity.number_map.get or return a finite number');
        }
        ref.fallback = result;
        return ref;
    }

    // A progress min/max is an entity-bound or constant lambda (field selects
    // are re-read every tick) or, if omitted, a static default. Returns
    // { key, fallback }: a field lambda yields { key: <captured field>,
    // fallback: <orElse default> }, a constant lambda yields
    // { key: null, fallback: <constant> }, and an omitted value yields
    // { key: null, fallback: <static default> }.
    function progressRange(opt, entity, label, staticDefault) {
        if (opt == null) {
            return { key: null, fallback: staticDefault };
        }
        if (typeof opt !== 'function') {
            throw new Error('ui: progress options.progress.' + label + ' must be a lambda (entity) => NumberExpression');
        }
        var ref;
        try {
            ref = progressFieldRef(opt, entity, label);
        } catch (e) {
            throw new Error('ui: progress options.progress.' + label + ' lambda threw: ' + (e && e.message));
        }
        return ref;
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

    // Marks the children slot of a container view with `name`: same marker
    // mechanism as container lists, but the render lambda returns a single
    // panel per entity (not an array), and the engine stamps each item with
    // `options.entity = entityId` so the per-tick position pass can resolve
    // its geometry from the container's getX/getY/getSpanX/getSpanY.
    function containerViewMarker(name) {
        return '$$containerView:' + name;
    }

    function isContainerViewMarker(c) {
        return typeof c === 'string' && c.indexOf('$$containerView:') === 0;
    }

    // Marks the children slot of a sector grid view with `name`: same marker
    // mechanism as container views, but the engine materializes one cell
    // window per footprint square (positioned by cellSize) and one thin marker
    // per portal from the runtime's computed grid, not from a render lambda.
    function sectorGridMarker(name) {
        return '$$sectorGrid:' + name;
    }

    function isSectorGridMarker(c) {
        return typeof c === 'string' && c.indexOf('$$sectorGrid:') === 0;
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
                        for (var k = 0; k < arr.length; k++) {
                            var rid = resolveId(arr[k]);
                            if (typeof rid === 'string' && rid.length > 0) itemIds.push(rid);
                        }
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

    /// Re-expands every container view's marker against the current entity
    /// list, stamping each materialized item panel with `options.entity` so
    /// the engine's per-tick position pass can resolve its geometry. The
    /// render lambda returns a single panel id per entity (not an array);
    /// returning null skips the entity.
    function expandContainerViews(entitiesFor) {
        for (var i = 0; i < order.length; i++) {
            var node = nodes[order[i]];
            if (!node || node.kind !== 'window') continue;
            if (!Array.isArray(node.children)) continue;
            var changed = false;
            for (var j = 0; j < node.children.length; j++) {
                var c = node.children[j];
                if (!isContainerViewMarker(c)) continue;
                var name = c.slice('$$containerView:'.length);
                var render = containerViewRenders[name];
                var cid = node.options && node.options.container;
                var entities = (typeof entitiesFor === 'function' && render && typeof cid === 'string')
                    ? entitiesFor(cid) : [];
                var itemIds = [];
                if (render && Array.isArray(entities)) {
                    for (var e = 0; e < entities.length; e++) {
                        var result = render({ id: entities[e], index: e });
                        if (result == null) continue;
                        // The render returns a single panel id (a raw string or
                        // a node handle). If a module returns an array or
                        // multiple, take the first id; the view cannot place
                        // more than one panel per entity.
                        var itemId = resolveId(Array.isArray(result) ? result[0] : result);
                        if (typeof itemId !== 'string' || itemId.length === 0) continue;
                        itemIds.push(itemId);
                        // Stamp the item with its entity so the position pass
                        // can resolve geometry. The item node may be a window
                        // or division; both carry an options object.
                        var itemNode = nodes[itemId];
                        if (itemNode) {
                            if (!itemNode.options || typeof itemNode.options !== 'object')
                                itemNode.options = {};
                            itemNode.options.entity = entities[e];
                        }
                    }
                }
                containerViewItems[name] = itemIds;
                for (var m = itemIds.length - 1; m >= 0; m--) {
                    node.children.splice(j, 0, itemIds[m]);
                }
                node.children.splice(j + itemIds.length, 1);
                changed = true;
                break;
            }
            if (changed) i--;
        }
    }

    /// Re-expands every sector grid view's marker against the runtime's
    /// computed grids (globalThis.__uiSectorGrids, gridId -> { cells, portals
    /// }). For each footprint cell a window node is placed at
    /// (x*cellSize, y*cellSize) with span (cellSize, cellSize); for each
    /// portal a thin marker is centered on the shared boundary edge. The view
    /// node carries options.grid (grid id), options.cellSize (default 32) and
    /// options.portalThickness (default 6).
    function expandSectorGrids() {
        var grids = root.__uiSectorGrids || {};
        for (var i = 0; i < order.length; i++) {
            var node = nodes[order[i]];
            if (!node || node.kind !== 'window') continue;
            if (!Array.isArray(node.children)) continue;
            var changed = false;
            for (var j = 0; j < node.children.length; j++) {
                var c = node.children[j];
                if (!isSectorGridMarker(c)) continue;
                var name = c.slice('$$sectorGrid:'.length);
                var gridId = node.options && node.options.grid;
                var cellSize = (node.options && typeof node.options.cellSize === 'number' && node.options.cellSize > 0)
                    ? node.options.cellSize : 32;
                var thickness = (node.options && typeof node.options.portalThickness === 'number' && node.options.portalThickness > 0)
                    ? node.options.portalThickness : 6;
                // Gap between adjacent cells: each cell is inset by gap/2 on
                // every side, so the space between two neighbors is `gap`.
                var cellGap = (node.options && typeof node.options.cellGap === 'number' && node.options.cellGap > 0)
                    ? node.options.cellGap : 0;
                if (cellGap >= cellSize) cellGap = cellSize;
                var cellInset = cellGap / 2;
                var grid = (typeof gridId === 'string') ? grids[gridId] : null;
                var render = sectorGridRenders[name];
                var itemIds = [];
                if (render && grid) {
                    // One item per footprint cell: the render returns a node id
                    // (the module sets its content/background) and the engine
                    // stamps its geometry from (x, y) * cellSize.
                    var cells = grid.cells || [];
                    // Map of "x,y" -> container so each cell can tell whether an
                    // adjacent square belongs to the same container.
                    var containerByXY = Object.create(null);
                    for (var ck = 0; ck < cells.length; ck++)
                        containerByXY[cells[ck].x + ',' + cells[ck].y] = cells[ck].container;
                    for (var ci = 0; ci < cells.length; ci++) {
                        var cell = cells[ci];
                        var pid = render({
                            id: name + '-cell-' + cell.x + '-' + cell.y,
                            index: ci,
                            sector: 'cell',
                            x: cell.x,
                            y: cell.y,
                            container: cell.container
                        });
                        var itemId = resolveId(Array.isArray(pid) ? pid[0] : pid);
                        if (typeof itemId !== 'string' || itemId.length === 0) continue;
                        itemIds.push(itemId);
                        var in1 = nodes[itemId];
                        if (in1) {
                            if (!in1.options || typeof in1.options !== 'object') in1.options = {};
                            // Extend toward a same-container neighbour (no inset, so
                            // same-container cells read as one continuous region) and
                            // inset toward a different/absent neighbour (gap boundary).
                            var sameL = containerByXY[(cell.x - 1) + ',' + cell.y] === cell.container;
                            var sameR = containerByXY[(cell.x + 1) + ',' + cell.y] === cell.container;
                            var sameT = containerByXY[cell.x + ',' + (cell.y - 1)] === cell.container;
                            var sameB = containerByXY[cell.x + ',' + (cell.y + 1)] === cell.container;
                            var left = sameL ? cell.x * cellSize : cell.x * cellSize + cellInset;
                            var right = sameR ? (cell.x + 1) * cellSize : (cell.x + 1) * cellSize - cellInset;
                            var top = sameT ? cell.y * cellSize : cell.y * cellSize + cellInset;
                            var bottom = sameB ? (cell.y + 1) * cellSize : (cell.y + 1) * cellSize - cellInset;
                            if (!in1.options.panel || typeof in1.options.panel !== 'object') in1.options.panel = {};
                            in1.options.panel.x = left;
                            in1.options.panel.y = top;
                            in1.options.panel.width = right - left;
                            in1.options.panel.height = bottom - top;
                            in1.options.cell = cell.x + ',' + cell.y;
                            in1.options.sector = 'cell';
                            in1.options.container = cell.container;
                        }
                    }
                    // One node per portal. Facing portals are a single edge-to-edge
                    // line; non-facing (diagonal) portals become a Manhattan path of a
                    // few axis-aligned segments that travel along the GAPS between cells
                    // (the "streets"), turning only at street intersections. Each segment
                    // is a headless filled rect of width `thickness`.
                    var portals = grid.portals || [];
                    // Street center of an E/W door: the gap boundary it faces (a multiple
                    // of cellSize). The visible building edge sits inset from it.
                    function streetX(side, cell) {
                        return side === 'E' ? (cell[0] + 1) * cellSize : cell[0] * cellSize;
                    }
                    function streetY(side, cell) {
                        return side === 'S' ? (cell[1] + 1) * cellSize : cell[1] * cellSize;
                    }
                    // Position along an E/W door's street (the opening's span center, y).
                    function doorY(side, cell, span, len) {
                        return (cell[1] + span + len / 2) * cellSize;
                    }
                    // Position along an N/S door's street (the opening's span center, x).
                    function doorX(side, cell, span, len) {
                        return (cell[0] + span + len / 2) * cellSize;
                    }
                    // Nearest street line (a multiple of cellSize) to a coordinate.
                    function nearestStreet(v) {
                        var k = v / cellSize;
                        var fl = k - (k % 1);
                        var fr = fl + 1;
                        return (k - fl <= fr - k) ? fl * cellSize : fr * cellSize;
                    }
                    for (var pi = 0; pi < portals.length; pi++) {
                        var p = portals[pi];
                        var a = p.a || {};
                        var b = p.b || {};
                        var acell = a.cell || [0, 0];
                        var bcell = b.cell || [0, 0];
                        var aspan = typeof a.span === 'number' ? a.span : 0;
                        var alen = (typeof a.length === 'number' && a.length > 0) ? a.length : 1;
                        var bspan = typeof b.span === 'number' ? b.span : 0;
                        var blen = (typeof b.length === 'number' && b.length > 0) ? b.length : 1;
                        // A "facing" portal joins two openings on opposing sides that line
                        // up on the same axis — directly adjacent or across one or more blank
                        // squares. Render one solid line that starts on a's visible cell edge
                        // and ends on b's, so there is neither a gap nor an inset at either end.
                        var facing = b.cell && b.side &&
                            ((a.side === 'E' && b.side === 'W' && acell[1] === bcell[1] && bcell[0] > acell[0]) ||
                             (a.side === 'W' && b.side === 'E' && acell[1] === bcell[1] && acell[0] > bcell[0]) ||
                             (a.side === 'S' && b.side === 'N' && acell[0] === bcell[0] && bcell[1] > acell[1]) ||
                             (a.side === 'N' && b.side === 'S' && acell[0] === bcell[0] && acell[1] > bcell[1]));
                        if (facing) {
                            var lr;
                            if (a.side === 'E' || a.side === 'W') {
                                var lcell = a.side === 'E' ? acell : bcell;
                                var rcell = a.side === 'E' ? bcell : acell;
                                var hsp = a.side === 'E' ? aspan : bspan;
                                var hln = a.side === 'E' ? alen : blen;
                                var x0 = (lcell[0] + 1) * cellSize - cellInset;
                                var x1 = rcell[0] * cellSize + cellInset;
                                var cym = (lcell[1] + hsp + hln / 2) * cellSize;
                                lr = { x: x0, y: cym - thickness / 2, w: x1 - x0, h: thickness };
                            } else {
                                var tcell = a.side === 'S' ? acell : bcell;
                                var ucell = a.side === 'S' ? bcell : acell;
                                var vsp = a.side === 'S' ? aspan : bspan;
                                var vln = a.side === 'S' ? alen : blen;
                                var y0 = (tcell[1] + 1) * cellSize - cellInset;
                                var y1 = ucell[1] * cellSize + cellInset;
                                var cxm = (tcell[0] + vsp + vln / 2) * cellSize;
                                lr = { x: cxm - thickness / 2, y: y0, w: thickness, h: y1 - y0 };
                            }
                            itemIds.push(register({
                                id: name + '-portal-' + pi,
                                kind: 'window',
                                options: {
                                    panel: { x: lr.x, y: lr.y, width: lr.w, height: lr.h },
                                    portalArrow: true,
                                    portalLine: true,
                                    sector: 'portal',
                                    portal: p.id
                                },
                                children: []
                            }));
                        } else {
                            // A non-facing portal (e.g. a diagonal link) joins two openings
                            // on different boundary edges. Treat the cells as buildings and
                            // the gaps between them as streets: build an axis-aligned
                            // polyline that only runs along street lines (multiples of
                            // cellSize), turning at street intersections, from a's door to
                            // b's door. Stroke it as one rect per segment plus a
                            // thickness x thickness square at each internal corner so the
                            // joints read as smooth rectangles rather than notches.
                            var vertA = a.side === 'E' || a.side === 'W';
                            var vertB = b.side === 'E' || b.side === 'W';
                            var inX = function (side, cell) {
                                return side === 'E' ? (cell[0] + 1) * cellSize - cellInset : cell[0] * cellSize + cellInset;
                            };
                            var inY = function (side, cell) {
                                return side === 'S' ? (cell[1] + 1) * cellSize - cellInset : cell[1] * cellSize + cellInset;
                            };
                            var pts = [];
                            if (vertA && vertB) {
                                // Both doors face a vertical street: A stub, up/down A's
                                // street to a cross-street, across it, up/down B's street, B stub.
                                var ax = streetX(a.side, acell), ay = doorY(a.side, acell, aspan, alen);
                                var bx = streetX(b.side, bcell), by = doorY(b.side, bcell, bspan, blen);
                                var cross = nearestStreet((ay + by) / 2);
                                pts = [
                                    { x: inX(a.side, acell), y: ay },
                                    { x: ax, y: ay },
                                    { x: ax, y: cross },
                                    { x: bx, y: cross },
                                    { x: bx, y: by },
                                    { x: inX(b.side, bcell), y: by }
                                ];
                            } else if (!vertA && !vertB) {
                                // Both doors face a horizontal street: mirror of the above.
                                var aax = doorX(a.side, acell, aspan, alen), aay = streetY(a.side, acell);
                                var bax = doorX(b.side, bcell, bspan, blen), bay = streetY(b.side, bcell);
                                var crossX = nearestStreet((aax + bax) / 2);
                                pts = [
                                    { x: aax, y: inY(a.side, acell) },
                                    { x: aax, y: aay },
                                    { x: crossX, y: aay },
                                    { x: crossX, y: bay },
                                    { x: bax, y: bay },
                                    { x: bax, y: inY(b.side, bcell) }
                                ];
                            } else if (vertA) {
                                // A faces a vertical street, B a horizontal one: L at their
                                // intersection (A's street-x, B's street-y).
                                var ax2 = streetX(a.side, acell), ay2 = doorY(a.side, acell, aspan, alen);
                                var bax2 = doorX(b.side, bcell, bspan, blen), bay2 = streetY(b.side, bcell);
                                pts = [
                                    { x: inX(a.side, acell), y: ay2 },
                                    { x: ax2, y: ay2 },
                                    { x: ax2, y: bay2 },
                                    { x: bax2, y: bay2 },
                                    { x: bax2, y: inY(b.side, bcell) }
                                ];
                            } else {
                                // A faces a horizontal street, B a vertical one: L at their
                                // intersection (B's street-x, A's street-y).
                                var aax3 = doorX(a.side, acell, aspan, alen), aay3 = streetY(a.side, acell);
                                var bax3 = streetX(b.side, bcell), bay3 = doorY(b.side, bcell, bspan, blen);
                                pts = [
                                    { x: aax3, y: inY(a.side, acell) },
                                    { x: aax3, y: aay3 },
                                    { x: bax3, y: aay3 },
                                    { x: bax3, y: bay3 },
                                    { x: inX(b.side, bcell), y: bay3 }
                                ];
                            }
                            var half = thickness / 2;
                            for (var si = 0; si < pts.length - 1; si++) {
                                var p1 = pts[si], p2 = pts[si + 1];
                                var sx, sy, sw, sh;
                                if (p1.y === p2.y) {
                                    sx = p1.x < p2.x ? p1.x : p2.x; sw = p2.x - p1.x; if (sw < 0) sw = -sw;
                                    sy = p1.y - half; sh = thickness;
                                } else {
                                    sx = p1.x - half; sw = thickness;
                                    sy = p1.y < p2.y ? p1.y : p2.y; sh = p2.y - p1.y; if (sh < 0) sh = -sh;
                                }
                                if (sw <= 0 && sh <= 0) continue;
                                itemIds.push(register({
                                    id: name + '-portal-' + pi + (si === 0 ? '' : '-s' + si),
                                    kind: 'window',
                                    options: {
                                        panel: { x: sx, y: sy, width: sw, height: sh },
                                        portalArrow: true,
                                        portalLine: true,
                                        sector: 'portal',
                                        portal: p.id
                                    },
                                    children: []
                                }));
                            }
                            for (var ci = 1; ci < pts.length - 1; ci++) {
                                var cp = pts[ci];
                                itemIds.push(register({
                                    id: name + '-portal-' + pi + '-c' + ci,
                                    kind: 'window',
                                    options: {
                                        panel: { x: cp.x - half, y: cp.y - half, width: thickness, height: thickness },
                                        portalArrow: true,
                                        portalLine: true,
                                        sector: 'portal',
                                        portal: p.id
                                    },
                                    children: []
                                }));
                            }
                        }
                    }
                    // Unlinked openings: declared openings whose (cell, side) is not
                    // covered by a linked portal (no facing opening on the adjacent
                    // square). Rendered as red headless shafts on the boundary edge
                    // so a lone sector's dead-end openings stay visible.
                    var linkedSide = Object.create(null);
                    for (var li = 0; li < portals.length; li++) {
                        var la = portals[li].a || {}, lb = portals[li].b || {};
                        var lc = la.cell || [0, 0], lb2 = lb.cell || [0, 0];
                        linkedSide[lc[0] + ',' + lc[1] + ':' + la.side] = true;
                        linkedSide[lb2[0] + ',' + lb2[1] + ':' + lb.side] = true;
                    }
                    var unlinkedSeq = 0;
                    for (var uci = 0; uci < cells.length; uci++) {
                        var uopen = cells[uci].openings || [];
                        for (var uoi = 0; uoi < uopen.length; uoi++) {
                            var o = uopen[uoi];
                            var uc = o.cell || [0, 0];
                            var ux = uc[0], uy = uc[1], uside = o.side;
                            if (linkedSide[ux + ',' + uy + ':' + uside]) continue;
                            var useq = unlinkedSeq++;
                            var ustart = typeof o.start === 'number' ? o.start : 0;
                            var ulen = (typeof o.length === 'number' && o.length > 0) ? o.length : 1;
                            var upx, upy, upw, uph;
                            if (uside === 'N') {
                                upx = ux * cellSize + ustart * cellSize;
                                upy = uy * cellSize - thickness / 2;
                                upw = ulen * cellSize; uph = thickness;
                            } else if (uside === 'S') {
                                upx = ux * cellSize + ustart * cellSize;
                                upy = (uy + 1) * cellSize - thickness / 2;
                                upw = ulen * cellSize; uph = thickness;
                            } else if (uside === 'W') {
                                upx = ux * cellSize - thickness / 2;
                                upy = uy * cellSize + ustart * cellSize;
                                upw = thickness; uph = ulen * cellSize;
                            } else {
                                upx = (ux + 1) * cellSize - thickness / 2;
                                upy = uy * cellSize + ustart * cellSize;
                                upw = thickness; uph = ulen * cellSize;
                            }
                            var uitemId = register({
                                id: name + '-unlinked-' + useq,
                                kind: 'window',
                                options: {
                                    panel: { x: upx, y: upy, width: upw, height: uph },
                                    portalArrow: true,
                                    unlinked: true,
                                    sector: 'portal',
                                    portal: ''
                                },
                                children: []
                            });
                            itemIds.push(uitemId);
                        }
                    }
                }
                sectorGridItems[name] = itemIds;
                for (var m = itemIds.length - 1; m >= 0; m--) {
                    node.children.splice(j, 0, itemIds[m]);
                }
                node.children.splice(j + itemIds.length, 1);
                changed = true;
                break;
            }
            if (changed) i--;
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
        for (var vname in containerViewRenders) {
            var vitems = containerViewItems[vname];
            if (vitems) {
                for (var vi = 0; vi < vitems.length; vi++) removeSubtree(vitems[vi]);
            }
            containerViewItems[vname] = [];
            var view = nodes[vname];
            if (view && Array.isArray(view.children)) {
                view.children = [containerViewMarker(vname)];
            }
        }
        for (var sname in sectorGridItems) {
            var sitems = sectorGridItems[sname];
            if (sitems) {
                for (var si = 0; si < sitems.length; si++) removeSubtree(sitems[si]);
            }
            sectorGridItems[sname] = [];
            var sgrid = nodes[sname];
            if (sgrid && Array.isArray(sgrid.children)) {
                sgrid.children = [sectorGridMarker(sname)];
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
            var panel = (options && typeof options.panel === 'object' && options.panel) ? options.panel : {};
            return parentHandle(register({
                id: id,
                kind: 'division',
                options: { panel: resolveBorderOptions(resolveHoverOptions(normalizeOptions(panel))) },
                children: childIds(children)
            }));
        },
        window: function (id, options, children) {
            var panel = (options && typeof options.panel === 'object' && options.panel) ? options.panel : {};
            return parentHandle(register({
                id: id,
                kind: 'window',
                options: { panel: resolveBorderOptions(resolveHoverOptions(normalizeOptions(panel))) },
                children: childIds(children)
            }));
        },
        // The global full-screen screen: the panel api minus positioning/
        // sizing (the renderer fills the viewport). Presentation is nested
        // under `screen`; only `background` is honored for now. Each call
        // replaces the current screen (the previous screen node is dropped
        // from the view on the next snapshot).
        setScreen: function (options, children) {
            var scr = (options && typeof options.screen === 'object' && options.screen) ? options.screen : {};
            screenSeq += 1;
            var id = '__ui-screen-' + screenSeq;
            currentScreenId = id;
            return parentHandle(register({
                id: id,
                kind: 'screen',
                options: { screen: normalizeOptions(scr) },
                children: childIds(children)
            }));
        },
        text: function (id, value) {
            return nodeHandle(register({
                id: id,
                kind: 'text',
                value: typeof value === 'string' ? value : String(value),
                children: []
            }));
        },
        image: function (id, src) {
            requireId(id, 'image');
            if (typeof src !== 'string' || src.length === 0) {
                throw new Error('ui: image src must be a non-empty archive path string');
            }
            return nodeHandle(register({
                id: id,
                kind: 'image',
                src: src,
                children: []
            }));
        },
        entityList: function (name, args, render) {
            if (typeof name !== 'string' || name.length === 0) {
                throw new Error('ui: entityList mandatory name missing');
            }
            if (!args || typeof args !== 'object') {
                throw new Error('ui: entityList args must be an object with container');
            }
            var entityListContainer = resolveId(args.container);
            if (typeof entityListContainer !== 'string' || entityListContainer.length === 0) {
                throw new Error('ui: entityList args.container must be a non-empty container id');
            }
            if (typeof render !== 'function') {
                throw new Error('ui: entityList render must be a function(entity) => nodeIds');
            }
            containerRenders[name] = render;
            return nodeHandle(register({
                id: name,
                kind: 'division',
                options: { container: entityListContainer },
                children: [containerMarker(name)]
            }));
        },
        containerView: function (name, args, render) {
            if (typeof name !== 'string' || name.length === 0) {
                throw new Error('ui: containerView mandatory name missing');
            }
            if (!args || typeof args !== 'object') {
                throw new Error('ui: containerView args must be an object with container and panel');
            }
            var containerViewContainer = resolveId(args.container);
            if (typeof containerViewContainer !== 'string' || containerViewContainer.length === 0) {
                throw new Error('ui: containerView args.container must be a non-empty container id');
            }
            var panel = (args.panel && typeof args.panel === 'object') ? args.panel : {};
            if (typeof panel.width !== 'number' || !(panel.width > 0)) {
                throw new Error('ui: containerView panel.width must be a positive number (view width)');
            }
            if (typeof panel.height !== 'number' || !(panel.height > 0)) {
                throw new Error('ui: containerView panel.height must be a positive number (view height)');
            }
            if (typeof render !== 'function') {
                throw new Error('ui: containerView render must be a function(entity) => panelId');
            }
            containerViewRenders[name] = render;
            // The view node is a window (it extends panel: the size/position/
            // background/anchor live in `panel`). It carries `container`,
            // `viewWidth`, `viewHeight` (top-level, for the per-tick position
            // pass) plus `area?` and the resolved `panel` config.
            var opts = {
                container: containerViewContainer,
                viewWidth: panel.width,
                viewHeight: panel.height
            };
            if (args.area != null) opts.area = args.area;
            opts.panel = resolveBorderOptions(resolveHoverOptions(normalizeOptions(panel)));
            return nodeHandle(register({
                id: name,
                kind: 'window',
                options: opts,
                children: [containerViewMarker(name)]
            }));
        },
        sectorGrid: function (name, args, render) {
            if (typeof name !== 'string' || name.length === 0) {
                throw new Error('ui: sectorGrid mandatory name missing');
            }
            if (!args || typeof args !== 'object') {
                throw new Error('ui: sectorGrid args must be an object with grid');
            }
            var sectorGridId = resolveId(args.grid);
            if (typeof sectorGridId !== 'string' || sectorGridId.length === 0) {
                throw new Error('ui: sectorGrid args.grid must be a non-empty grid id');
            }
            if (typeof render !== 'function') {
                throw new Error('ui: sectorGrid render must be a function(item) => nodeId');
            }
            // The view node is a window (it extends panel: the size/position/
            // background live in `panel`). It carries `grid` (the runtime grid
            // id) plus optional `cellSize`/`cellGap`/`portalThickness`
            // (top-level, read by the expansion pass); the engine invokes
            // `render` once per cell + portal each tick and positions the node
            // it returns from the grid geometry.
            sectorGridRenders[name] = render;
            var panel = (args.panel && typeof args.panel === 'object') ? args.panel : {};
            var opts = {
                grid: sectorGridId,
                panel: resolveBorderOptions(resolveHoverOptions(normalizeOptions(panel)))
            };
            opts.cellSize = (typeof args.cellSize === 'number' && args.cellSize > 0) ? args.cellSize : 32;
            if (args.cellGap != null) opts.cellGap = args.cellGap;
            if (args.portalThickness != null) opts.portalThickness = args.portalThickness;
            return nodeHandle(register({
                id: name,
                kind: 'window',
                options: opts,
                children: [sectorGridMarker(name)]
            }));
        },
        field: function (id, binding) {
            if (!binding || typeof binding !== 'object') {
                throw new Error('ui: field binding must be an object with entity, map, name');
            }
            var fieldEntity = resolveId(binding.entity);
            if (typeof fieldEntity !== 'string' || fieldEntity.length === 0) {
                throw new Error('ui: field binding.entity must be a non-empty entity id');
            }
            if (binding.map !== 'number' && binding.map !== 'text') {
                throw new Error('ui: field binding.map must be "number" or "text"');
            }
            if (typeof binding.name !== 'string' || binding.name.length === 0) {
                throw new Error('ui: field binding.name must be a non-empty field name');
            }
            var fallback = typeof binding.fallback === 'string' ? binding.fallback : '';
            return nodeHandle(register({
                id: id,
                kind: 'field',
                binding: {
                    entity: fieldEntity,
                    map: binding.map,
                    name: binding.name,
                    fallback: fallback
                },
                value: fallback,
                children: []
            }));
        },
        progress: function (id, options) {
            if (!options || typeof options !== 'object') {
                throw new Error('ui: progress options must be an object');
            }
            var progress = options.progress;
            if (!progress || typeof progress !== 'object') {
                throw new Error('ui: progress options.progress is required (an object)');
            }
            var fill = backgroundRef(progress.fill);
            if (!validProgressTexRef(fill)) {
                throw new Error('ui: progress options.progress.fill must be a non-empty archive path or animation');
            }
            var track = null;
            if (progress.track != null) {
                track = backgroundRef(progress.track);
                if (!validProgressTexRef(track)) {
                    throw new Error('ui: progress options.progress.track must be a non-empty archive path or animation');
                }
            }
            var orientation = progress.orientation == null ? 'horizontal' : progress.orientation;
            if (orientation !== 'horizontal' && orientation !== 'vertical') {
                throw new Error('ui: progress options.progress.orientation must be "horizontal" or "vertical"');
            }
            // steps quantizes the fill into an integer number of steps
            // (default 100); the engine stores round(fraction * steps) and the
            // renderer divides back by steps.
            var steps = progress.steps == null ? 100 : progress.steps;
            if (typeof steps !== 'number' || !isFinite(steps) || steps < 1 || Math.floor(steps) !== steps) {
                throw new Error('ui: progress options.progress.steps must be a positive integer');
            }
            // The progress is always bound: entity, value, and max are required.
            var entity = resolveId(progress.entity);
            if (typeof entity !== 'string' || entity.length === 0) {
                throw new Error('ui: progress options.progress.entity is required (a non-empty entity id)');
            }
            if (typeof progress.value !== 'function') {
                throw new Error('ui: progress options.progress.value is required (a lambda (entity) => NumberExpression)');
            }
            if (typeof progress.max !== 'function') {
                throw new Error('ui: progress options.progress.max is required (a lambda (entity) => NumberExpression)');
            }
            // value is a lambda: (entity: EntityExpression) => NumberExpression.
            // It either selects a field via the entity's number_map api (the
            // engine re-reads the field from the entity's number data every
            // tick and uses the .orElse default when the field is absent) or
            // returns a constant NumberExpression (used as-is).
            var valueRef;
            try {
                valueRef = progressFieldRef(progress.value, progress.entity, 'value');
            } catch (e) {
                throw new Error('ui: progress options.progress.value lambda threw: ' + (e && e.message));
            }
            // max is a required lambda; min is an optional lambda (default 0).
            // The captured .orElse defaults / constants are the raw-value
            // fallbacks used when a field is absent on the entity.
            var maxRef;
            try {
                maxRef = progressFieldRef(progress.max, progress.entity, 'max');
            } catch (e) {
                throw new Error('ui: progress options.progress.max lambda threw: ' + (e && e.message));
            }
            var minR = progressRange(progress.min, progress.entity, 'min', 0);
            var binding = {
                entity: entity,
                map: 'number',
                fallback: valueRef.fallback,
                maxFallback: maxRef.fallback,
                minFallback: minR.fallback
            };
            // Field selects carry their key for the per-tick re-read; constant
            // fields omit it (the engine keeps the captured value).
            if (valueRef.key != null) binding.name = valueRef.key;
            if (maxRef.key != null) binding.maxName = maxRef.key;
            if (minR.key != null) binding.minName = minR.key;
            // The panel config (size/position/background/border/anchor) lives in
            // `options.panel`; the progress presentation (fill/track/orientation/
            // steps) stays top-level for the renderer.
            var panel = (options.panel && typeof options.panel === 'object') ? options.panel : {};
            var opts = {
                panel: resolveBorderOptions(resolveHoverOptions(normalizeOptions(panel))),
                fill: fill,
                orientation: orientation,
                steps: steps
            };
            if (track != null) opts.track = track;
            return nodeHandle(register({
                id: id,
                kind: 'progress',
                value: 0,
                binding: binding,
                options: opts,
                children: []
            }));
        },
        snapshot: function () {
            // Only nodes reachable from a top-level surface belong to the view:
            // a window, or the current screen (the latest setScreen). A
            // standalone node (no window/screen ancestor) is a no-op: it
            // registers, but is never added to the view.
            var parent = Object.create(null);
            for (var i = 0; i < order.length; i++) {
                var pn = nodes[order[i]];
                if (!pn || !Array.isArray(pn.children)) continue;
                for (var k = 0; k < pn.children.length; k++) {
                    var ck = pn.children[k];
                    if (typeof ck === 'string' && nodes[ck]) parent[ck] = pn.id;
                }
            }
            var visMemo = Object.create(null);
            function visible(id) {
                if (id in visMemo) return visMemo[id];
                visMemo[id] = false;
                var n = nodes[id];
                var v = false;
                if (n) {
                    var p = parent[id];
                    v = p == null
                        ? (n.kind === 'window'
                            || (n.kind === 'screen' && id === currentScreenId))
                        : visible(p);
                }
                visMemo[id] = v;
                return v;
            }
            var out = [];
            for (var i2 = 0; i2 < order.length; i2++) {
                var nid = order[i2];
                if (!visible(nid)) continue;
                out.push(JSON.parse(JSON.stringify(nodes[nid])));
            }
            return out;
        },
        clear: function () {
            nodes = Object.create(null);
            order = [];
            containerRenders = Object.create(null);
            containerItems = Object.create(null);
            containerViewRenders = Object.create(null);
            containerViewItems = Object.create(null);
            sectorGridRenders = Object.create(null);
            sectorGridItems = Object.create(null);
            screenSeq = 0;
            currentScreenId = null;
        },
        loadSnapshot: function (arr) {
            nodes = Object.create(null);
            order = [];
            containerItems = Object.create(null);
            containerViewItems = Object.create(null);
            sectorGridItems = Object.create(null);
            (arr || []).forEach(function (n) {
                requireId(n.id, n.kind);
                nodes[n.id] = n;
                order.push(n.id);
            });
        },
        expandContainers: function (entitiesFor) {
            expandContainers(entitiesFor);
        },
        expandContainerViews: function (entitiesFor) {
            expandContainerViews(entitiesFor);
        },
        expandSectorGrids: function () {
            expandSectorGrids();
        },
        resetContainers: function () {
            resetContainers();
        }
    };
    return api;
});
