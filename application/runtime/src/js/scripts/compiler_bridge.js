// Compiler bridge: instruments HostApi calls to build AST nodes instead of executing immediately.
// Provides a registry of AST nodes that can be flushed as JSON for Rust-side compilation.

var __astNodes = {};
var __nextNodeId = 1;

function registerNode(node) {
    var id = __nextNodeId++;
    __astNodes[id] = node;
    return id;
}

// ---- Number wrapper ----
// entityRefInfo: { queryId, keyId } if this number comes from an entity reference
function makeNumberWrapper(id, entityRefInfo) {
    return {
        id: id,
        _entityRef: entityRefInfo || null,
        sum: function(otherId) {
            var newId = registerNode({
                type: "NumberSum",
                left: id,
                right: otherId
            });
            // If this number comes from an entity reference, also record a SumNumber mutation
            if (entityRefInfo) {
                registerNode({
                    type: "SumNumber",
                    entity: entityRefInfo.queryId,
                    key: entityRefInfo.keyId,
                    addend: otherId
                });
            }
            return makeNumberWrapper(newId, null);
        },
        subtract: function(otherId) {
            var newId = registerNode({
                type: "NumberSubtract",
                left: id,
                right: otherId
            });
            if (entityRefInfo) {
                registerNode({
                    type: "SetNumberMapValue",
                    entity: entityRefInfo.queryId,
                    key: entityRefInfo.keyId,
                    value: newId
                });
            }
            return makeNumberWrapper(newId, null);
        },
        multiply: function(otherId) {
            var newId = registerNode({
                type: "NumberMultiply",
                left: id,
                right: otherId
            });
            if (entityRefInfo) {
                registerNode({
                    type: "SetNumberMapValue",
                    entity: entityRefInfo.queryId,
                    key: entityRefInfo.keyId,
                    value: newId
                });
            }
            return makeNumberWrapper(newId, null);
        },
        divide: function(otherId) {
            var newId = registerNode({
                type: "NumberDivide",
                left: id,
                right: otherId
            });
            if (entityRefInfo) {
                registerNode({
                    type: "SetNumberMapValue",
                    entity: entityRefInfo.queryId,
                    key: entityRefInfo.keyId,
                    value: newId
                });
            }
            return makeNumberWrapper(newId, null);
        }
    };
}

// ---- String wrapper ----
function makeStringWrapper(id) {
    return {
        id: id,
        concat: function(otherId) {
            var newId = registerNode({
                type: "StringConcat",
                left: id,
                right: otherId
            });
            return makeStringWrapper(newId);
        },
        isContainingExactly: function(otherId) {
            var condId = registerNode({
                type: "StringContains",
                haystack: id,
                needle: otherId,
                exact: true
            });
            return makeConditionWrapper(condId);
        },
        isContaining: function(otherId) {
            var condId = registerNode({
                type: "StringContains",
                haystack: id,
                needle: otherId,
                exact: false
            });
            return makeConditionWrapper(condId);
        }
    };
}

// ---- Condition wrapper ----
function makeConditionWrapper(id) {
    return {
        id: id,
        and: function(otherId) {
            var newId = registerNode({
                type: "ConditionAnd",
                left: id,
                right: otherId
            });
            return makeConditionWrapper(newId);
        },
        or: function(otherId) {
            var newId = registerNode({
                type: "ConditionOr",
                left: id,
                right: otherId
            });
            return makeConditionWrapper(newId);
        },
        negate: function() {
            var newId = registerNode({
                type: "ConditionNegate",
                inner: id
            });
            return makeConditionWrapper(newId);
        }
    };
}

// ---- Instrumented HostApi ----
function createInstrumentedHostApi() {
    return {
        number: {
            of: function(value) {
                var id = registerNode({ type: "NumberLiteral", value: Number(value) });
                return makeNumberWrapper(id);
            }
        },
        string: {
            of: function(value) {
                var id = registerNode({ type: "StringLiteral", value: String(value) });
                return makeStringWrapper(id);
            }
        },
        texture: {
            of: function(t) { return t; }
        },
        log: function(msg) {
            var msgId;
            if (msg && typeof msg === 'object' && typeof msg.id === 'number') {
                msgId = msg.id;
            } else {
                msgId = registerNode({ type: "StringLiteral", value: String(msg || "") });
            }
            registerNode({
                type: "Log",
                message: msgId
            });
        },
        emitEvent: function(name, payload) {
            var nameId;
            if (name && typeof name === 'object' && typeof name.id === 'number') {
                nameId = name.id;
            } else if (name && typeof name === 'object' && typeof name.name === 'string') {
                nameId = registerNode({ type: "StringLiteral", value: name.name });
            } else {
                nameId = registerNode({ type: "StringLiteral", value: String(name || "") });
            }
            registerNode({
                type: "EmitEvent",
                eventName: nameId,
                payload: payload || {}
            });
        },
        emitEffect: function(name, payload) {
            var nameId;
            if (name && typeof name === 'object' && typeof name.id === 'number') {
                nameId = name.id;
            } else if (name && typeof name === 'object' && typeof name.name === 'string') {
                nameId = registerNode({ type: "StringLiteral", value: name.name });
            } else {
                nameId = registerNode({ type: "StringLiteral", value: String(name || "") });
            }
            registerNode({
                type: "EmitEvent",
                eventName: nameId,
                payload: payload || {}
            });
        },
        setEntity: function(id, data) {
            // Record entity creation — tracked via entity_data from extraction
        },
        registerAction: function() { /* no-op during compilation */ },
        registerEffect: function() { /* no-op during compilation */ },
        registerEvent: function() { /* no-op during compilation */ },
        registerPanel: function() { /* no-op during compilation */ },
        createEntity: function(data) {
            var textMap = [];
            var numberMap = [];
            if (data && typeof data === "object") {
                if (data.textMap) {
                    for (var key in data.textMap) {
                        if (data.textMap.hasOwnProperty(key)) {
                            var keyId = registerNode({ type: "StringLiteral", value: key });
                            var val = data.textMap[key];
                            var valId;
                            if (val && typeof val === 'object' && typeof val.id === 'number') {
                                valId = val.id;
                            } else {
                                valId = registerNode({ type: "StringLiteral", value: String(val || "") });
                            }
                            textMap.push([keyId, valId]);
                        }
                    }
                }
                if (data.numberMap) {
                    for (var key in data.numberMap) {
                        if (data.numberMap.hasOwnProperty(key)) {
                            var keyId2 = registerNode({ type: "StringLiteral", value: key });
                            var val2 = data.numberMap[key];
                            var valId2;
                            if (val2 && typeof val2 === 'object' && typeof val2.id === 'number') {
                                valId2 = val2.id;
                            } else {
                                valId2 = registerNode({ type: "NumberLiteral", value: Number(val2 || 0) });
                            }
                            numberMap.push([keyId2, valId2]);
                        }
                    }
                }
            }
            registerNode({
                type: "CreateEntity",
                textMap: textMap,
                numberMap: numberMap
            });
        },
        entity: {
            filter: {
                create: function() {
                    return makeFilterWrapper();
                }
            }
        }
    };
}

// ---- Entity filter wrapper ----
function makeFilterWrapper() {
    return {
        byId: function(predicateFn) {
            var placeholderId = registerNode({ type: "StringLiteral", value: "__self_id__" });
            var placeholder = makeStringWrapper(placeholderId);
            var condResult;
            try {
                condResult = predicateFn(placeholder);
            } catch(e) {
                condResult = null;
            }
            var condId;
            if (condResult && typeof condResult.id === 'number') {
                condId = condResult.id;
            } else {
                condId = registerNode({ type: "ConditionLiteral", value: true });
            }
            var filterId = registerNode({
                type: "FilterById",
                predicate: condId
            });
            return { filterId: filterId };
        }
    };
}

// ---- Instrumented EventContext ----
function createInstrumentedEventContext(hostApi) {
    return {
        getEntityBy: function(filterWrapper) {
            var filterId;
            if (filterWrapper && typeof filterWrapper.filterId === 'number') {
                filterId = filterWrapper.filterId;
            } else {
                filterId = registerNode({ type: "FilterAll" });
            }
            var queryId = registerNode({
                type: "EntityQuery",
                filter: filterId
            });
            return {
                queryId: queryId,
                map: function(callback) {
                    var elemId = registerNode({
                        type: "ElementRef",
                        query: queryId
                    });
                    var iElem = createInstrumentedElement(elemId, hostApi);
                    try { callback(iElem); } catch(e) { /* ignore */ }
                }
            };
        },
        emitEffect: function(name, payload) {
            var nameId;
            if (name && typeof name === 'object' && typeof name.id === 'number') {
                nameId = name.id;
            } else {
                nameId = registerNode({ type: "StringLiteral", value: String(name || "") });
            }
            registerNode({
                type: "EmitEvent",
                eventName: nameId,
                payload: payload || {}
            });
        },
        emitEvent: function(name, payload) {
            var nameId;
            if (name && typeof name === 'object' && typeof name.id === 'number') {
                nameId = name.id;
            } else {
                nameId = registerNode({ type: "StringLiteral", value: String(name || "") });
            }
            registerNode({
                type: "EmitEvent",
                eventName: nameId,
                payload: payload || {}
            });
        },
        createEntity: function(data) {
            var textMap = [];
            var numberMap = [];
            if (data && typeof data === "object") {
                if (data.textMap) {
                    for (var key in data.textMap) {
                        if (data.textMap.hasOwnProperty(key)) {
                            var keyId = registerNode({ type: "StringLiteral", value: key });
                            var val = data.textMap[key];
                            var valId;
                            if (val && typeof val === 'object' && typeof val.id === 'number') {
                                valId = val.id;
                            } else {
                                valId = registerNode({ type: "StringLiteral", value: String(val || "") });
                            }
                            textMap.push([keyId, valId]);
                        }
                    }
                }
                if (data.numberMap) {
                    for (var key in data.numberMap) {
                        if (data.numberMap.hasOwnProperty(key)) {
                            var keyId2 = registerNode({ type: "StringLiteral", value: key });
                            var val2 = data.numberMap[key];
                            var valId2;
                            if (val2 && typeof val2 === 'object' && typeof val2.id === 'number') {
                                valId2 = val2.id;
                            } else {
                                valId2 = registerNode({ type: "NumberLiteral", value: Number(val2 || 0) });
                            }
                            numberMap.push([keyId2, valId2]);
                        }
                    }
                }
            }
            registerNode({
                type: "CreateEntity",
                textMap: textMap,
                numberMap: numberMap
            });
        },
        log: function(msg) {
            var msgId;
            if (msg && typeof msg === 'object' && typeof msg.id === 'number') {
                msgId = msg.id;
            } else {
                msgId = registerNode({ type: "StringLiteral", value: String(msg || "") });
            }
            registerNode({
                type: "Log",
                message: msgId
            });
        },
        entity: {
            filter: {
                create: function() {
                    return makeFilterWrapper();
                }
            }
        },
        string: {
            of: function(value) {
                var id = registerNode({ type: "StringLiteral", value: String(value) });
                return makeStringWrapper(id);
            }
        },
        number: {
            of: function(value) {
                var id = registerNode({ type: "NumberLiteral", value: Number(value) });
                return makeNumberWrapper(id);
            }
        }
    };
}

// ---- Instrumented element (returned by getEntityBy().map) ----
function createInstrumentedElement(elementId, hostApi) {
    return {
        getNumber: function(keyWrapper) {
            var keyId;
            if (keyWrapper && typeof keyWrapper.id === 'number') {
                keyId = keyWrapper.id;
            } else {
                keyId = registerNode({ type: "StringLiteral", value: String(keyWrapper || "") });
            }
            var refId = registerNode({
                type: "NumberEntityRef",
                element: elementId,
                key: keyId
            });
            // Pass entity reference info so .sum() can record mutations
            var entityRefInfo = { queryId: elementId, keyId: keyId };
            return {
                id: refId,
                _entityRef: entityRefInfo,
                map: function(callback) {
                    var numWrapper = makeNumberWrapper(refId, entityRefInfo);
                    try { callback(numWrapper); } catch(e) { /* ignore */ }
                },
                orElse: function(fallbackWrapper) {
                    var fallbackId;
                    if (fallbackWrapper && typeof fallbackWrapper.id === 'number') {
                        fallbackId = fallbackWrapper.id;
                    } else if (typeof fallbackWrapper === 'string') {
                        fallbackId = registerNode({ type: "StringLiteral", value: fallbackWrapper });
                    } else {
                        fallbackId = registerNode({ type: "StringLiteral", value: String(fallbackWrapper || "") });
                    }
                    registerNode({
                        type: "OrElseNumberRef",
                        expr: refId,
                        fallback: fallbackId
                    });
                    return {
                        id: refId,
                        _fallbackId: fallbackId,
                        _entityRef: entityRefInfo
                    };
                }
            };
        },
        getText: function(keyWrapper) {
            var keyId;
            if (keyWrapper && typeof keyWrapper.id === 'number') {
                keyId = keyWrapper.id;
            } else {
                keyId = registerNode({ type: "StringLiteral", value: String(keyWrapper || "") });
            }
            var refId = registerNode({
                type: "StringEntityRef",
                element: elementId,
                key: keyId
            });
            return {
                id: refId,
                _entityRef: { queryId: elementId, keyId: keyId },
                map: function(callback) {
                    var strWrapper = makeStringWrapper(refId);
                    try { callback(strWrapper); } catch(e) { /* ignore */ }
                },
                concat: function(suffix) {
                    return makeStringWrapper(refId);
                },
                orElse: function(fallbackWrapper) {
                    var fallbackId;
                    if (fallbackWrapper && typeof fallbackWrapper.id === 'number') {
                        fallbackId = fallbackWrapper.id;
                    } else if (typeof fallbackWrapper === 'string') {
                        fallbackId = registerNode({ type: "StringLiteral", value: fallbackWrapper });
                    } else {
                        fallbackId = registerNode({ type: "StringLiteral", value: String(fallbackWrapper || "") });
                    }
                    return {
                        id: refId,
                        _fallbackId: fallbackId,
                        _entityRef: { queryId: elementId, keyId: keyId }
                    };
                }
            };
        }
    };
}

// ---- Instrument globalThis.host methods in place ----
// This is the KEY function: it replaces hostApi methods with instrumented versions
// WITHOUT changing the object reference. Closures that captured hostApi will see
// the new methods because they hold a reference to the same object.
function instrumentHostApiInPlace() {
    if (!globalThis.host) return;
    var h = globalThis.host;
    // Replace log
    h.log = function(msg) {
        var msgId = registerNode({ type: "StringLiteral", value: String(msg || "") });
        registerNode({ type: "Log", message: msgId });
    };
    // Replace emitEvent
    h.emitEvent = function(name, payload) {
        var nameId;
        if (typeof name === 'string') {
            nameId = registerNode({ type: "StringLiteral", value: name });
        } else if (name && typeof name === 'object' && typeof name.name === 'string') {
            nameId = registerNode({ type: "StringLiteral", value: name.name });
        } else {
            nameId = registerNode({ type: "StringLiteral", value: String(name || "") });
        }
        registerNode({ type: "EmitEvent", eventName: nameId, payload: payload || {} });
    };
    // Replace emitEffect (some APIs use emitEffect on hostApi directly)
    h.emitEffect = function(name, payload) {
        var nameId;
        if (typeof name === 'string') {
            nameId = registerNode({ type: "StringLiteral", value: name });
        } else if (name && typeof name === 'object' && typeof name.name === 'string') {
            nameId = registerNode({ type: "StringLiteral", value: name.name });
        } else {
            nameId = registerNode({ type: "StringLiteral", value: String(name || "") });
        }
        registerNode({ type: "EmitEvent", eventName: nameId, payload: payload || {} });
    };
    // Replace number.of
    h.number = {
        of: function(value) {
            var id = registerNode({ type: "NumberLiteral", value: Number(value) });
            return makeNumberWrapper(id);
        }
    };
    // Replace string.of
    h.string = {
        of: function(value) {
            var id = registerNode({ type: "StringLiteral", value: String(value) });
            return makeStringWrapper(id);
        }
    };
    // Replace entity.filter.create
    h.entity = {
        filter: {
            create: function() {
                return makeFilterWrapper();
            }
        }
    };
    // Replace registerPanel with instrumented version
    h.registerPanel = instrumentedRegisterPanel;
}

// ---- Flush/clear ----
function __flushAstNodes() {
    return JSON.stringify(__astNodes);
}

function __clearAstNodes() {
    __astNodes = {};
    __nextNodeId = 1;
}

// ---- Compiled panels storage ----
var __compiledPanels = [];

function __flushCompiledPanels() {
    return JSON.stringify(__compiledPanels);
}

// Helper: unwrap AST number wrapper {id: N} to plain number
function _unwrapNumber(v) {
    if (v && typeof v === "object" && typeof v.id === "number") {
        var node = __astNodes[v.id];
        if (node && typeof node.value === "number") return node.value;
    }
    return v;
}

// Helper: unwrap AST string wrapper {id: N} to plain string
function _unwrapString(v) {
    if (v && typeof v === "object" && typeof v.id === "number") {
        var node = __astNodes[v.id];
        if (node && typeof node.value === "string") return node.value;
    }
    return v;
}

// Helper: recursively unwrap AST wrappers in anchor/offset/size objects
function _unwrapCoords(obj) {
    if (!obj || typeof obj !== "object") return obj;
    var out = {};
    for (var k in obj) {
        out[k] = _unwrapNumber(obj[k]);
    }
    return out;
}

// ---- Instrumented registerPanel for compilation ----
function instrumentedRegisterPanel(p) {
    if (!p || typeof p !== "object") return;

    var panelObj = {
        id: p.id || "unknown",
        background: p.background || null,
        size: _unwrapCoords(p.size) || { width: 100, height: 100 },
        anchor: _unwrapCoords(p.anchor),
        offset: _unwrapCoords(p.offset),
    };

    // Preserve onClick handler
    if (p.onClick) {
        panelObj.onClick = p.onClick;
    }

    // Preserve children (for panel hierarchy)
    if (p.children) {
        panelObj.children = p.children;
    }

    // Preserve layout configuration
    if (p.layout) {
        panelObj.layout = p.layout;
    }

    // If content has a function-valued value, compile it into AST
    if (p.content && p.content.type === "entityNumberValue" && typeof p.content.value === "function") {
        var entityIdVal = p.content.entityId;
        var entityIdStr = null;
        if (entityIdVal && typeof entityIdVal === "string") {
            entityIdStr = entityIdVal;
        } else if (entityIdVal && typeof entityIdVal === "object" && typeof entityIdVal.id === "number") {
            // It's an AST node reference — resolve from registry
            var node = __astNodes[entityIdVal.id];
            if (node && typeof node.value === "string") {
                entityIdStr = node.value;
            }
        }

        // Create an instrumented element representing the entity
        var elemId = registerNode({
            type: "ElementRef",
            query: registerNode({ type: "EntityQuery", filter: registerNode({ type: "FilterAll" }) })
        });
        var iElem = createInstrumentedElement(elemId, globalThis.host);

        // Evaluate the value lambda against the instrumented entity
        var result;
        try { result = p.content.value(iElem); } catch(e) { result = null; }

        // Check if result has .orElse fallback (from OrElseNumberRef)
        var exprId = null;
        var fallbackId = null;
        if (result && typeof result.id === "number") {
            exprId = result.id;
            // If .orElse was called, a StringLiteral fallback was registered
            // We capture it via the OrElseNumberRef node
            if (result._fallbackId && typeof result._fallbackId === "number") {
                fallbackId = result._fallbackId;
            }
        }

        panelObj.content = {
            contentEntityNumberValue: {
                entityId: entityIdStr,
                align: p.content.align || "center",
                exprId: exprId,
                fallbackId: fallbackId,
            }
        };
    } else if (p.content && p.content.type === "entityTextValue" && typeof p.content.value === "function") {
        // Handle entityTextValue with value lambda (similar to entityNumberValue)
        var textEntityIdVal = p.content.entityId;
        var textEntityIdStr = null;
        if (textEntityIdVal && typeof textEntityIdVal === "string") {
            textEntityIdStr = textEntityIdVal;
        } else if (textEntityIdVal && typeof textEntityIdVal === "object" && typeof textEntityIdVal.id === "number") {
            var textNode = __astNodes[textEntityIdVal.id];
            if (textNode && typeof textNode.value === "string") {
                textEntityIdStr = textNode.value;
            }
        }

        var textElemId = registerNode({
            type: "ElementRef",
            query: registerNode({ type: "EntityQuery", filter: registerNode({ type: "FilterAll" }) })
        });
        var textElem = createInstrumentedElement(textElemId, globalThis.host);

        var textResult;
        try { textResult = p.content.value(textElem); } catch(e) { textResult = null; }

        var textExprId = null;
        var textFallbackId = null;
        if (textResult && typeof textResult.id === "number") {
            textExprId = textResult.id;
            if (textResult._fallbackId && typeof textResult._fallbackId === "number") {
                textFallbackId = textResult._fallbackId;
            }
        }

        panelObj.content = {
            contentEntityTextValue: {
                entityId: textEntityIdStr,
                align: p.content.align || "center",
                exprId: textExprId,
                fallbackId: textFallbackId,
            }
        };
    } else if (p.content) {
        // Unwrap AST wrappers in content.value for non-function content
        var contentCopy = {};
        for (var ck in p.content) {
            if (ck === "value") {
                contentCopy[ck] = _unwrapString(p.content[ck]);
            } else {
                contentCopy[ck] = p.content[ck];
            }
        }
        panelObj.content = contentCopy;
    }

    __compiledPanels.push(panelObj);

    // Also store to __registeredPanels so the extraction phase picks up the compiled panel JSON
    // This ensures onClick, children, and other properties are available to the FFI
    if (!globalThis.__registeredPanels) globalThis.__registeredPanels = [];
    globalThis.__registeredPanels.push(JSON.stringify(panelObj));
}

// Override registerPanel on the host object with the instrumented version
function instrumentRegisterPanel() {
    if (!globalThis.host) return;
    globalThis.host.registerPanel = instrumentedRegisterPanel;
}
