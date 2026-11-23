// Shared entity-filter runtime: recursive FilterArg evaluation over the live
// entity data, installed in every host context. Filters are plain FilterArg
// objects (see plans/entity-filter.md); anything else — plain strings, bare
// functions, { id: fn }, null — is malformed (logged, no matches, never a
// first-entity fallback).
//
// FilterArg: a plain object with exactly one of four keys:
//   { filters: FilterLambda[] }  every lambda must yield a truthy condition
//   { and: FilterArg[] }         every child must match
//   { or: FilterArg[] }          at least one child must match
//   { not: FilterArg[] }         no child may match
// Empty arrays match everything (vacuous truth).
//
// FilterLambda: (e: EntityExpression) => ConditionExpression, where e is the
// per-entity query surface (__entityProxy): id StringExpression,
// number_map/string_map MaybeExpression lookups, named members and areas.
// The condition extraction idiom:
//   e.number_map.get("hp").map(v => v.isLessThan(3)).orElse(condition.of(false))
// The lookups hand the callback a value wrapper carrying the expression's
// comparison/mutation surface; comparisons return condition objects
// ({value, ifTrue, ifFalse, ...}) read by __condToBool.

pub fn host_api_script_entity_filter() -> &'static str {
    r#"(function () {
  'use strict';

  function filterLog(msg) {
    try {
      globalThis.__logs = globalThis.__logs || [];
      globalThis.__logs.push(String(msg));
    } catch (e) { }
  }

  // Read a ConditionExpression as a boolean. Accepts plain booleans,
  // {value: boolean} wrappers, and the ifTrue/ifFalse protocol.
  globalThis.__condToBool = function (c) {
    if (typeof c === 'boolean') return c;
    if (c !== null && typeof c === 'object') {
      if (typeof c.value === 'boolean') return c.value;
      var out = null;
      if (typeof c.ifTrue === 'function') {
        try { c.ifTrue(function () { out = true; }); } catch (e) { }
      }
      if (typeof c.ifFalse === 'function') {
        try { c.ifFalse(function () { out = false; }); } catch (e) { }
      }
      if (out !== null) return out === true;
    }
    if (typeof c === 'number' || typeof c === 'string') return !!c;
    return false;
  };

  // A condition object over a concrete boolean (the runtime shape of
  // ConditionExpression; cf. the host's condition.of factory).
  // Carries the asserted value so ifTrue/ifFalse can pass it to callbacks.
  function conditionOf(b, carried) {
    b = !!b;
    return {
      value: b,
      __carried: carried,
      and: function (o) { return conditionOf(b && globalThis.__condToBool(o), carried); },
      or: function (o) { return conditionOf(b || globalThis.__condToBool(o), carried); },
      negate: function () { return conditionOf(!b, carried); },
      ifTrue: function (cb) {
        var primary = (b && typeof cb === 'function') ? cb(carried) : undefined;
        return { or: function (cb2) { return b ? primary : (typeof cb2 === 'function' ? cb2(carried) : undefined); } };
      },
      ifFalse: function (cb) {
        var primary = (!b && typeof cb === 'function') ? cb(carried) : undefined;
        return { or: function (cb2) { return !b ? primary : (typeof cb2 === 'function' ? cb2(carried) : undefined); } };
      }
    };
  }

  // The absent MaybeExpression.
  var absentMaybe = {
    isPresent: function () { return conditionOf(false, undefined); },
    isEmpty: function () { return conditionOf(true, undefined); },
    orElse: function (d) { return d; },
    ifPresent: function () { },
    map: function () { return absentMaybe; },
    flatMap: function () { return absentMaybe; }
  };

  // A present MaybeExpression: orElse -> expression wrapper (carrying the
  // value's comparison/mutation surface), ifPresent -> wrapper, map ->
  // a new maybe over the transformed value.
  function presentMaybe(raw, wrapper) {
    return {
      isPresent: function () { return conditionOf(true, wrapper); },
      isEmpty: function () { return conditionOf(false, wrapper); },
      orElse: function (d) { return wrapper; },
      ifPresent: function (cb) { if (typeof cb === 'function') cb(wrapper); },
      map: function (fn) {
        var r;
        try { r = fn(wrapper); } catch (e) { return absentMaybe; }
        return {
          isPresent: function () { return conditionOf(true, r); },
          isEmpty: function () { return conditionOf(false, r); },
          orElse: function (d) { return r; },
          ifPresent: function (cb) { if (typeof cb === 'function') cb(r); },
          map: function (fn2) {
            var r2;
            try { r2 = fn2(r); } catch (e2) { return absentMaybe; }
            return presentMaybe(r2, r2);
          }
        };
      }
    };
  }

  // A NumberExpression value over a concrete number: comparisons return
  // condition objects, arithmetic returns new values. Carries _raw for
  // extraction and valueOf so plain arithmetic still coerces.
  function numValue(v) {
    v = Number(v);
    function numOf(x) {
      return (typeof x === 'object' && x !== null)
        ? (x._raw !== undefined ? x._raw : x.value) : x;
    }
    var self = {
      _raw: v,
      value: v,
      valueOf: function () { return v; },
      of: function (x) { return numValue(numOf(x)); },
      toString: function () { return String(v); },
      toJSON: function () { return v; },
      isGreaterThan: function (t) { return conditionOf(v > Number(numOf(t)), self); },
      isLessThan: function (t) { return conditionOf(v < Number(numOf(t)), self); },
      isGreaterOrEqualTo: function (t) { return conditionOf(v >= Number(numOf(t)), self); },
      isLessOrEqualTo: function (t) { return conditionOf(v <= Number(numOf(t)), self); },
      isEqualTo: function (t) { return conditionOf(v === Number(numOf(t)), self); },
      isNotEqualTo: function (t) { return conditionOf(v !== Number(numOf(t)), self); },
      sum: function (d) { return numValue(v + Number(numOf(d))); },
      subtract: function (d) { return numValue(v - Number(numOf(d))); },
      multiply: function (d) { return numValue(v * Number(numOf(d))); },
      divide: function (d) { return numValue(v / Number(numOf(d))); },
      modulo: function (d) { return numValue(v % Number(numOf(d))); },
      toStringExpression: function () { return String(v); }
    };
    return self;
  }

  // ArrayReadOnlyExpression over concrete values (mapped results): length/
  // count as number values, map transforms into a new value list,
  // exactlyOne -> a Maybe over the single element.
  globalThis.__makeValueList = function (values, elementWrap) {
    if (!Array.isArray(values)) values = [];
    elementWrap = elementWrap || function (v) { return v; };
    return {
      of: function () {
        var args = Array.prototype.slice.call(arguments);
        return globalThis.__makeValueList(args, function (v) { return v; });
      },
      map: function (cb) {
        var out = [];
        if (typeof cb === 'function') {
          for (var i = 0; i < values.length; i++) {
            try { out.push(cb(elementWrap(values[i]), i)); } catch (e) { }
          }
        }
        return globalThis.__makeValueList(out, function (v) { return v; });
      },
      findBy: function (filter) {
        var arg = globalThis.__normalizeEntityFilter(filter);
        var out = [];
        if (arg.malformed) filterLog('___filter-malformed');
        else for (var i = 0; i < values.length; i++) {
          if (elementWrap === globalThis.__entityProxy && !globalThis.__readEntity(values[i])) continue;
          if (globalThis.__evalFilterArg(arg, elementWrap(values[i]))) out.push(values[i]);
        }
        return globalThis.__makeValueList(out, elementWrap);
      },
      hasEntity: function (filter) {
        var arg = globalThis.__normalizeEntityFilter(filter);
        if (arg.malformed) { filterLog('___filter-malformed'); return conditionOf(false); }
        for (var i = 0; i < values.length; i++) {
          if (elementWrap === globalThis.__entityProxy && !globalThis.__readEntity(values[i])) continue;
          if (globalThis.__evalFilterArg(arg, elementWrap(values[i]))) return conditionOf(true);
        }
        return conditionOf(false);
      },
      forEach: function (cb) {
        if (typeof cb !== 'function') return;
        for (var i = 0; i < values.length; i++) {
          try { cb(elementWrap(values[i]), i); } catch (e) { }
        }
      },
      size: function () { return values.length; },
      length: function () { return numValue(values.length); },
      count: function () { return numValue(values.length); },
      get: function (index) {
        var i = Number(index);
        if (isNaN(i)) i = 0;
        if (i < 0 || i >= values.length) return absentMaybe;
        return presentMaybe(values[i], elementWrap(values[i]));
      },
      randomElement: function () {
        if (values.length === 0) return absentMaybe;
        var i = Math.floor(globalThis.__lxmNext() * values.length);
        if (i >= values.length) i = values.length - 1;
        return presentMaybe(values[i], elementWrap(values[i]));
      },
      exactlyOne: function () {
        if (values.length === 1) return presentMaybe(values[0], elementWrap(values[0]));
        return absentMaybe;
      }
    };
  };

  // The per-entity query surface handed to FilterLambda predicates and list
  // element accessors.
  globalThis.__entityProxy = function (id, mutable) {
    function read() { return globalThis.__readEntity(id) || {}; }
    var eid = id;

    function numberMaybe(key) {
      var nm = read().numberMap || {};
      if (!Object.prototype.hasOwnProperty.call(nm, key)) return absentMaybe;
      function numOf(x) { return (typeof x === 'object' && x !== null) ? (x._raw !== undefined ? x._raw : x.value) : x; }
      function wrap(v) {
        var self = {
          _raw: v,
          value: v,
          of: function (x) { return wrap(Number(numOf(x))); },
          toString: function () { return String(v); },
          toJSON: function () { return v; },
          isGreaterThan: function (t) { return conditionOf(Number(v) > Number(numOf(t)), self); },
          isLessThan: function (t) { return conditionOf(Number(v) < Number(numOf(t)), self); },
          isGreaterOrEqualTo: function (t) { return conditionOf(Number(v) >= Number(numOf(t)), self); },
          isLessOrEqualTo: function (t) { return conditionOf(Number(v) <= Number(numOf(t)), self); },
          isEqualTo: function (t) { return conditionOf(Number(v) === Number(numOf(t)), self); },
          isNotEqualTo: function (t) { return conditionOf(Number(v) !== Number(numOf(t)), self); },
          sum: function (d) { return wrap(Number(v) + Number(numOf(d))); },
          subtract: function (d) { return wrap(Number(v) - Number(numOf(d))); },
          multiply: function (d) { return wrap(Number(v) * Number(numOf(d))); },
          divide: function (d) { return wrap(Number(v) / Number(numOf(d))); },
          modulo: function (d) { return wrap(Number(v) % Number(numOf(d))); },
          toStringExpression: function () {
            var sv = String(v);
            function strWrap(s) {
              return {
                _raw: s, value: s,
                of: function (x) { return strWrap(String(typeof x === 'object' && x !== null ? (x._raw !== undefined ? x._raw : x.value) : x)); },
                toString: function () { return String(s); },
                toJSON: function () { return s; },
                isContaining: function (o) { return conditionOf(String(s).indexOf(String(typeof o === 'object' && o !== null ? (o._raw !== undefined ? o._raw : o.value) : o)) >= 0, { _raw: s }); },
                isContainingExactly: function (o) { return conditionOf(String(s) === String(typeof o === 'object' && o !== null ? (o._raw !== undefined ? o._raw : o.value) : o), { _raw: s }); },
                map: function (fn) { return strWrap(String(typeof fn === 'object' && fn !== null ? (fn._raw !== undefined ? fn._raw : fn.value) : fn)); },
                concat: function (o) { return strWrap(String(s) + String(typeof o === 'object' && o !== null ? (o._raw !== undefined ? o._raw : o.value) : o)); }
              };
            }
            return strWrap(sv);
          }
        };
        return self;
      }
      return presentMaybe(nm[key], wrap(nm[key]));
    }

    function wrapString(v) {
        var self = {
          _raw: v,
          value: v,
          of: function (x) { return wrapString(String(typeof x === 'object' && x !== null ? (x._raw !== undefined ? x._raw : x.value) : x)); },
          toString: function () { return String(v); },
          toJSON: function () { return v; },
          isContaining: function (o) { return conditionOf(String(v).indexOf(String(typeof o === 'object' && o !== null ? (o._raw !== undefined ? o._raw : o.value) : o)) >= 0, self); },
          isContainingExactly: function (o) { return conditionOf(String(v) === String(typeof o === 'object' && o !== null ? (o._raw !== undefined ? o._raw : o.value) : o), self); },
          map: function (s) { return wrapString(String(typeof s === 'object' && s !== null ? (s._raw !== undefined ? s._raw : s.value) : s)); },
          concat: function (o) { return wrapString(String(v) + String(typeof o === 'object' && o !== null ? (o._raw !== undefined ? o._raw : o.value) : o)); }
        };
        return self;
    }

    function textMaybe(key) {
      var tm = read().stringMap || {};
      if (!Object.prototype.hasOwnProperty.call(tm, key)) return absentMaybe;
      return presentMaybe(tm[key], wrapString(tm[key]));
    }

    var entity = {
      value: eid,
      toString: function () { return eid; },
      toJSON: function () { return eid; },
      id: wrapString(eid),
      number_map: { get: numberMaybe },
      string_map: { get: textMaybe },
      getEntitiesInsideArea: function (areaName) {
        var name = globalThis.__entityId(areaName);
        var ids = name !== undefined && globalThis.__nativeInsideArea
          ? JSON.parse(globalThis.__nativeInsideArea(id, name)) : [];
        return globalThis.__makeValueList(ids, globalThis.__entityProxy);
      },
      members: function (name) {
        var lists = read().entityListMap;
        return globalThis.__makeValueList(lists && lists[name], globalThis.__entityProxy);
      }
    };
    if (!mutable) return entity;

    function selfProxy() { return globalThis.__entityProxy(id, true); }

    Object.assign(entity.number_map, {
        put: function (key, transform) {
          var current = numberMaybe(key);
          var result;
          try { result = transform(current); } catch (e) { return selfProxy(); }
          if (!result || typeof result !== 'object') return selfProxy();
          var isPresent;
          if (typeof result.isPresent === 'function') {
            isPresent = globalThis.__condToBool(result.isPresent());
          } else if ('value' in result) {
            isPresent = result.value !== undefined;
          } else {
            return selfProxy();
          }
          if (isPresent) {
            var raw;
            if (typeof result.orElse === 'function') { raw = result.orElse(result); }
            else { raw = result.value; }
            if (raw && typeof raw === 'object' && typeof raw._raw === 'number') { raw = raw._raw; }
            globalThis.__writeEntityField(id, "numberMap", key, Number(raw));
          } else {
            globalThis.__writeEntityField(id, "numberMap", key, undefined);
          }
          return selfProxy();
        },
        remove: function (key) {
          globalThis.__writeEntityField(id, "numberMap", key, undefined);
          return selfProxy();
        }
    });
    Object.assign(entity.string_map, {
        put: function (key, transform) {
          var current = textMaybe(key);
          var result;
          try { result = transform(current); } catch (e) { return selfProxy(); }
          if (!result || typeof result !== 'object') return selfProxy();
          var isPresent;
          if (typeof result.isPresent === 'function') {
            isPresent = globalThis.__condToBool(result.isPresent());
          } else if ('value' in result) {
            isPresent = result.value !== undefined;
          } else {
            return selfProxy();
          }
          if (isPresent) {
            var raw;
            if (typeof result.orElse === 'function') { raw = result.orElse(result); }
            else { raw = result.value; }
            if (raw && typeof raw === 'object' && typeof raw._raw === 'string') { raw = raw._raw; }
            globalThis.__writeEntityField(id, "stringMap", key, String(raw));
          } else {
            globalThis.__writeEntityField(id, "stringMap", key, undefined);
          }
          return selfProxy();
        },
        remove: function (key) {
          globalThis.__writeEntityField(id, "stringMap", key, undefined);
          return selfProxy();
        }
    });
    entity.addMember = function (name, ref) {
        globalThis.__changeMembership(id, name, ref, true);
        return globalThis.__entityProxy(id);
    };
    entity.removeMember = function (name, ref) {
        globalThis.__changeMembership(id, name, ref, false);
        return selfProxy();
    };
    return entity;
  };

  // One recursive rule over the FilterArg variants.
  globalThis.__evalFilterArg = function (arg, e) {
    if (!arg || typeof arg !== 'object' || typeof arg === 'function') return false;
    var keys = ['filters', 'and', 'or', 'not'];
    var present = [];
    for (var k = 0; k < keys.length; k++) {
      if (arg[keys[k]] !== undefined) present.push(keys[k]);
    }
    if (present.length !== 1) return false;
    var kind = present[0];
    var arr = arg[kind];
    if (!Array.isArray(arr)) return false;
    if (kind === 'filters') {
      for (var i = 0; i < arr.length; i++) {
        var f = arr[i];
        if (typeof f !== 'function') return false;
        var ok;
        try { ok = globalThis.__condToBool(f(e)); } catch (err) { ok = false; }
        if (!ok) return false;
      }
      return true;
    }
    if (kind === 'and') {
      for (var i = 0; i < arr.length; i++) {
        if (!globalThis.__evalFilterArg(arr[i], e)) return false;
      }
      return true;
    }
    if (kind === 'or') {
      if (arr.length === 0) return true;
      for (var i = 0; i < arr.length; i++) {
        if (globalThis.__evalFilterArg(arr[i], e)) return true;
      }
      return false;
    }
    // not
    for (var i = 0; i < arr.length; i++) {
      if (globalThis.__evalFilterArg(arr[i], e)) return false;
    }
    return true;
  };

  // Strict FilterArg validation: only a plain object with exactly one of the
  // four variant keys (array-valued) is accepted; everything else — plain
  // strings, functions, { id: fn }, null — is malformed.
  globalThis.__normalizeEntityFilter = function (filter) {
    if (!filter || typeof filter !== 'object' || typeof filter === 'function') {
      return { malformed: true };
    }
    var known = ['filters', 'and', 'or', 'not'];
    var count = 0;
    for (var i = 0; i < known.length; i++) {
      if (filter[known[i]] !== undefined) count++;
    }
    if (count !== 1) return { malformed: true };
    for (var j = 0; j < known.length; j++) {
      if (filter[known[j]] !== undefined && !Array.isArray(filter[known[j]])) {
        return { malformed: true };
      }
    }
    return filter;
  };

  // ListExpression-shaped result over the matched ids (deterministic order).
  globalThis.__makeEntityList = function (ids, mutable) {
    function wrap(id) { return globalThis.__entityProxy(id, mutable); }
    function degraded() {
      return {
        map: function () { return { orElse: function (d) { return d; } }; },
        flatMap: function () { return { orElse: function (d) { return d; } }; }
      };
    }
    function makeElement(index) {
      if (index < 0 || index >= ids.length) return degraded();
      var ee = wrap(ids[index]);
      return {
        map: function (cb) {
          if (typeof cb === 'function') {
            try { cb(ee); } catch (e) { }
          }
          return { orElse: function (d) { return d; } };
        },
        flatMap: function (fn) {
          var r = null;
          try { r = fn(ee); } catch (e) { }
          if (r && typeof r === 'object') {
            return {
              map: function (cb) {
                if (r && typeof r.map === 'function') return r.map(cb);
                return { orElse: function (d) { return d; } };
              },
              orElse: function (d) {
                if (r && typeof r.orElse === 'function') return r.orElse(d);
                return d;
              }
            };
          }
          return degraded();
        }
      };
    }
    return {
      map: function (cb) {
        var out = [];
        if (typeof cb === 'function') {
          for (var i = 0; i < ids.length; i++) {
            try { out.push(cb(wrap(ids[i]), i)); } catch (e) { }
          }
        }
        return globalThis.__makeValueList(out, function (v) { return v; });
      },
      forEach: function (cb) {
        for (var i = 0; i < ids.length; i++) {
          try { cb(wrap(ids[i]), i); } catch (e) { }
        }
      },
      size: function () { return ids.length; },
      length: function () { return ids.length; },
      count: function () { return globalThis.host.number.of(ids.length); },
      get: function (index) {
        var i = Number(index);
        if (isNaN(i)) i = 0;
        return makeElement(i);
      },
      ifPresent: function (cb) {
        if (ids.length > 0 && typeof cb === 'function') {
          cb(wrap(ids[0]));
        }
      },
      randomElement: function () {
        return {
          ifPresent: function (cb) {
            if (typeof cb !== 'function') return;
            if (ids.length > 0) {
              cb(wrap(ids[Math.floor(globalThis.__lxmNext() * ids.length)]));
            } else {
              cb(null);
            }
          }
        };
      },
      exactlyOne: function () {
        if (ids.length === 1) return presentMaybe(ids[0], wrap(ids[0]));
        return absentMaybe;
      }
    };
  };

  // The entry point every context's getEntityBy delegates to.
  globalThis.__evalEntityFilter = function (filter, mutable) {
    var arg = globalThis.__normalizeEntityFilter(filter);
    if (arg.malformed) {
      filterLog('___filter-malformed');
      return globalThis.__makeEntityList([]);
    }
    var ids = globalThis.__entityIds();
    // Fresh simulate_action contexts may only carry raw store rows (entities
    // recorded during action execution) without declaration entries.
    if (!globalThis.__nativeEntityIds && ids.length === 0) {
      var store = globalThis.__entityStore || [];
      for (var i = 0; i < store.length; i++) {
        var row = store[i];
        if (!row || typeof row !== 'object') continue;
        var rid = (typeof row.id === 'string' && row.id) ? row.id : null;
        if (!rid) {
          for (var key in row) {
            if (Object.prototype.hasOwnProperty.call(row, key)
                && typeof row[key] === 'string') { rid = key; break; }
          }
        }
        if (rid && ids.indexOf(rid) < 0) ids.push(rid);
      }
    }
    var matched = [];
    for (var j = 0; j < ids.length; j++) {
      if (globalThis.__evalFilterArg(arg, globalThis.__entityProxy(ids[j]))) {
        matched.push(ids[j]);
      }
    }
    // Deterministic order: entity IDs are read from a Rust map whose
    // iteration order is not stable, so index-dependent operations (randomElement,
    // get(index), ifPresent) would otherwise see a different element for the
    // same seed. Sorting by id makes the list order stable across installs.
    matched.sort();
    if (matched.length === 0) filterLog('___filter-no-match');
    return globalThis.__makeEntityList(matched, mutable);
  };

})();"#
}
