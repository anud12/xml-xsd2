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
                        // The render returns a single panel id (a string). If a
                        // module returns an array or multiple, take the first
                        // id; the view cannot place more than one panel per
                        // entity.
                        var itemId = Array.isArray(result) ? result[0] : result;
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
                var cellSpan = cellSize - cellGap;
                var grid = (typeof gridId === 'string') ? grids[gridId] : null;
                var render = sectorGridRenders[name];
                var itemIds = [];
                if (render && grid) {
                    // One item per footprint cell: the render returns a node id
                    // (the module sets its content/background) and the engine
                    // stamps its geometry from (x, y) * cellSize.
                    var cells = grid.cells || [];
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
                        var itemId = Array.isArray(pid) ? pid[0] : pid;
                        if (typeof itemId !== 'string' || itemId.length === 0) continue;
                        itemIds.push(itemId);
                        var in1 = nodes[itemId];
                        if (in1) {
                            if (!in1.options || typeof in1.options !== 'object') in1.options = {};
                            in1.options.x = cell.x * cellSize + cellInset;
                            in1.options.y = cell.y * cellSize + cellInset;
                            in1.options.width = cellSpan;
                            in1.options.height = cellSpan;
                            in1.options.cell = cell.x + ',' + cell.y;
                            in1.options.sector = 'cell';
                            in1.options.container = cell.container;
                        }
                    }
                    // One node per portal: the engine itself draws a headless
                    // arrow (a straight shaft of width `thickness`) centered on
                    // the shared boundary edge. No render lambda is invoked; the
                    // portal's representation is fixed to this shaft.
                    var portals = grid.portals || [];
                    for (var pi = 0; pi < portals.length; pi++) {
                        var p = portals[pi];
                        var a = p.a || {};
                        var side = a.side;
                        var pc = a.cell || [0, 0];
                        var span = typeof a.span === 'number' ? a.span : 0;
                        var len = (typeof a.length === 'number' && a.length > 0) ? a.length : 1;
                        var pcx = pc[0], pcy = pc[1];
                        var px, py, pw, ph;
                        if (side === 'N') {
                            px = pcx * cellSize + span * cellSize;
                            py = pcy * cellSize - thickness / 2;
                            pw = len * cellSize;
                            ph = thickness;
                        } else if (side === 'S') {
                            px = pcx * cellSize + span * cellSize;
                            py = (pcy + 1) * cellSize - thickness / 2;
                            pw = len * cellSize;
                            ph = thickness;
                        } else if (side === 'W') {
                            px = pcx * cellSize - thickness / 2;
                            py = pcy * cellSize + span * cellSize;
                            pw = thickness;
                            ph = len * cellSize;
                        } else {
                            px = (pcx + 1) * cellSize - thickness / 2;
                            py = pcy * cellSize + span * cellSize;
                            pw = thickness;
                            ph = len * cellSize;
                        }
                        var pitemId = register({
                            id: name + '-portal-' + pi,
                            kind: 'window',
                            options: {
                                x: px,
                                y: py,
                                width: pw,
                                height: ph,
                                portalArrow: true,
                                sector: 'portal',
                                portal: p.id
                            },
                            children: []
                        });
                        itemIds.push(pitemId);
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
        entityList: function (name, args, render) {
            if (typeof name !== 'string' || name.length === 0) {
                throw new Error('ui: entityList mandatory name missing');
            }
            if (!args || typeof args !== 'object') {
                throw new Error('ui: entityList args must be an object with container');
            }
            if (typeof args.container !== 'string' || args.container.length === 0) {
                throw new Error('ui: entityList args.container must be a non-empty container id');
            }
            if (typeof render !== 'function') {
                throw new Error('ui: entityList render must be a function(entity) => nodeIds');
            }
            containerRenders[name] = render;
            return register({
                id: name,
                kind: 'division',
                options: { container: args.container },
                children: [containerMarker(name)]
            });
        },
        containerView: function (name, args, render) {
            if (typeof name !== 'string' || name.length === 0) {
                throw new Error('ui: containerView mandatory name missing');
            }
            if (!args || typeof args !== 'object') {
                throw new Error('ui: containerView args must be an object with container, width, height');
            }
            if (typeof args.container !== 'string' || args.container.length === 0) {
                throw new Error('ui: containerView args.container must be a non-empty container id');
            }
            if (typeof args.width !== 'number' || !(args.width > 0)) {
                throw new Error('ui: containerView args.width must be a positive number (view width)');
            }
            if (typeof args.height !== 'number' || !(args.height > 0)) {
                throw new Error('ui: containerView args.height must be a positive number (view height)');
            }
            if (typeof render !== 'function') {
                throw new Error('ui: containerView render must be a function(entity) => panelId');
            }
            containerViewRenders[name] = render;
            // The view node is a window (it extends panel: x/y/width/height/
            // background/anchor are all honored). It carries `container`,
            // `viewWidth`, `viewHeight` for the per-tick position pass, plus
            // the standard panel options (minus container/viewWidth/viewHeight).
            var opts = {};
            for (var k in args) {
                if (Object.prototype.hasOwnProperty.call(args, k)) opts[k] = args[k];
            }
            opts.viewWidth = args.width;
            opts.viewHeight = args.height;
            return register({
                id: name,
                kind: 'window',
                options: resolveBorderOptions(resolveHoverOptions(normalizeOptions(opts))),
                children: [containerViewMarker(name)]
            });
        },
        sectorGrid: function (name, args, render) {
            if (typeof name !== 'string' || name.length === 0) {
                throw new Error('ui: sectorGrid mandatory name missing');
            }
            if (!args || typeof args !== 'object') {
                throw new Error('ui: sectorGrid args must be an object with grid');
            }
            if (typeof args.grid !== 'string' || args.grid.length === 0) {
                throw new Error('ui: sectorGrid args.grid must be a non-empty grid id');
            }
            if (typeof render !== 'function') {
                throw new Error('ui: sectorGrid render must be a function(item) => nodeId');
            }
            // The view node is a window (it extends panel: x/y/width/height and
            // background are honored). It carries `grid` (the runtime grid id)
            // plus optional `cellSize`/`portalThickness`; the engine invokes
            // `render` once per cell + portal each tick and positions the node
            // it returns from the grid geometry.
            sectorGridRenders[name] = render;
            var opts = {};
            for (var k in args) {
                if (Object.prototype.hasOwnProperty.call(args, k)) opts[k] = args[k];
            }
            if (typeof opts.cellSize !== 'number' || !(opts.cellSize > 0)) opts.cellSize = 32;
            return register({
                id: name,
                kind: 'window',
                options: resolveBorderOptions(resolveHoverOptions(normalizeOptions(opts))),
                children: [sectorGridMarker(name)]
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
            containerViewRenders = Object.create(null);
            containerViewItems = Object.create(null);
            sectorGridRenders = Object.create(null);
            sectorGridItems = Object.create(null);
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
