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
                map: function(callback) {
                    var numWrapper = makeNumberWrapper(refId, entityRefInfo);
                    try { callback(numWrapper); } catch(e) { /* ignore */ }
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
                map: function(callback) {
                    var strWrapper = makeStringWrapper(refId);
                    try { callback(strWrapper); } catch(e) { /* ignore */ }
                },
                concat: function(suffix) {
                    return makeStringWrapper(refId);
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
}

// ---- Flush/clear ----
function __flushAstNodes() {
    return JSON.stringify(__astNodes);
}

function __clearAstNodes() {
    __astNodes = {};
    __nextNodeId = 1;
}
