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
// number_map/text_map MaybeExpression lookups, getEntitiesInsideArea,
// containers. The condition extraction idiom:
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
  function conditionOf(b) {
    b = !!b;
    return {
      value: b,
      and: function (o) { return conditionOf(b && globalThis.__condToBool(o)); },
      or: function (o) { return conditionOf(b || globalThis.__condToBool(o)); },
      negate: function () { return conditionOf(!b); },
      ifTrue: function (cb) { if (b && typeof cb === 'function') cb(); },
      ifFalse: function (cb) { if (!b && typeof cb === 'function') cb(); }
    };
  }

  // The absent MaybeExpression.
  var absentMaybe = {
    isPresent: function () { return conditionOf(false); },
    isEmpty: function () { return conditionOf(true); },
    orElse: function (d) { return d; },
    ifPresent: function () { },
    map: function () { return absentMaybe; },
    flatMap: function () { return absentMaybe; }
  };

  // A present MaybeExpression: orElse -> raw value, ifPresent -> value
  // wrapper (carrying the expression's comparison/mutation surface), map ->
  // a new maybe over the transformed value.
  function presentMaybe(raw, wrapper) {
    return {
      isPresent: function () { return conditionOf(true); },
      isEmpty: function () { return conditionOf(false); },
      orElse: function (d) { return raw; },
      ifPresent: function (cb) { if (typeof cb === 'function') cb(wrapper); },
      map: function (fn) {
        var r;
        try { r = fn(wrapper); } catch (e) { return absentMaybe; }
        return {
          isPresent: function () { return conditionOf(true); },
          isEmpty: function () { return conditionOf(false); },
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

  // The per-entity query surface handed to FilterLambda predicates and list
  // element accessors.
  globalThis.__entityProxy = function (id) {
    var data = (globalThis.__entityData || {})[id] || {};
    var nm = data.numberMap || {};
    var tm = data.textMap || {};
    var eid = (typeof data.id === 'string' && data.id) ? data.id : id;

    function numberMaybe(key) {
      if (!Object.prototype.hasOwnProperty.call(nm, key)) return absentMaybe;
      function wrap(v) {
        return {
          _raw: v,
          isGreaterThan: function (t) { return conditionOf(Number(v) > Number(t)); },
          isLessThan: function (t) { return conditionOf(Number(v) < Number(t)); },
          isGreaterOrEqualTo: function (t) { return conditionOf(Number(v) >= Number(t)); },
          isLessOrEqualTo: function (t) { return conditionOf(Number(v) <= Number(t)); },
          isEqualTo: function (t) { return conditionOf(Number(v) === Number(t)); },
          isNotEqualTo: function (t) { return conditionOf(Number(v) !== Number(t)); },
          sum: function (d) { return wrap(Number(v) + Number(d)); },
          divide: function (d) { return wrap(Number(v) / Number(d)); },
          modulo: function (d) { return wrap(Number(v) % Number(d)); }
        };
      }
      return presentMaybe(nm[key], wrap(nm[key]));
    }

    function textMaybe(key) {
      if (!Object.prototype.hasOwnProperty.call(tm, key)) return absentMaybe;
      function wrap(v) {
        return {
          _raw: v,
          isContaining: function (o) { return conditionOf(String(v).indexOf(String(o)) >= 0); },
          isContainingExactly: function (o) { return conditionOf(String(v) === String(o)); },
          map: function (s) { return wrap(s); }
        };
      }
      return presentMaybe(tm[key], wrap(tm[key]));
    }

    function selfProxy() { return globalThis.__entityProxy(id); }

    return {
      id: {
        isContaining: function (o) { return conditionOf(String(eid).indexOf(String(o)) >= 0); },
        isContainingExactly: function (o) { return conditionOf(String(eid) === String(o)); }
      },
      number_map: {
        get: function (key) { return numberMaybe(key); },
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
            data.numberMap = data.numberMap || {};
            data.numberMap[key] = Number(raw);
          } else {
            if (data.numberMap && Object.prototype.hasOwnProperty.call(data.numberMap, key)) {
              delete data.numberMap[key];
            }
          }
          return selfProxy();
        },
        remove: function (key) {
          if (data.numberMap && Object.prototype.hasOwnProperty.call(data.numberMap, key)) {
            delete data.numberMap[key];
          }
          return selfProxy();
        }
      },
      text_map: {
        get: function (key) { return textMaybe(key); },
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
            data.textMap = data.textMap || {};
            data.textMap[key] = String(raw);
          } else {
            if (data.textMap && Object.prototype.hasOwnProperty.call(data.textMap, key)) {
              delete data.textMap[key];
            }
          }
          return selfProxy();
        },
        remove: function (key) {
          if (data.textMap && Object.prototype.hasOwnProperty.call(data.textMap, key)) {
            delete data.textMap[key];
          }
          return selfProxy();
        }
      },
      getEntitiesInsideArea: function (areaName) {
        var areaMap = (globalThis.__insideArea && globalThis.__insideArea[id]) || {};
        var ids = areaMap[areaName] || [];
        return {
          forEach: function (cb) {
            for (var i = 0; i < ids.length; i++) {
              var self = i;
              cb({ getId: function () { return ids[self]; } });
            }
          },
          size: function () { return ids.length; }
        };
      },
      containers: {
        forEach: function () { },
        size: function () { return 0; }
      }
    };
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
  globalThis.__makeEntityList = function (ids) {
    function degraded() {
      return {
        map: function () { return { orElse: function (d) { return d; } }; },
        flatMap: function () { return { orElse: function (d) { return d; } }; }
      };
    }
    function makeElement(index) {
      if (index < 0 || index >= ids.length) return degraded();
      var ee = globalThis.__entityProxy(ids[index]);
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
        for (var i = 0; i < ids.length; i++) {
          try { cb(globalThis.__entityProxy(ids[i])); } catch (e) { }
        }
      },
      forEach: function (cb) {
        for (var i = 0; i < ids.length; i++) {
          try { cb(globalThis.__entityProxy(ids[i]), i); } catch (e) { }
        }
      },
      size: function () { return ids.length; },
      length: function () { return ids.length; },
      get: function (index) {
        var i = Number(index);
        if (isNaN(i)) i = 0;
        return makeElement(i);
      },
      ifPresent: function (cb) {
        if (ids.length > 0 && typeof cb === 'function') {
          cb(globalThis.__entityProxy(ids[0]));
        }
      },
      randomElement: function () {
        return {
          ifPresent: function (cb) {
            if (typeof cb !== 'function') return;
            if (ids.length > 0) {
              cb(globalThis.__entityProxy(ids[Math.floor(Math.random() * ids.length)]));
            } else {
              cb(null);
            }
          }
        };
      }
    };
  };

  // The entry point every context's getEntityBy delegates to.
  globalThis.__evalEntityFilter = function (filter) {
    var arg = globalThis.__normalizeEntityFilter(filter);
    if (arg.malformed) {
      filterLog('___filter-malformed');
      return globalThis.__makeEntityList([]);
    }
    var data = globalThis.__entityData || {};
    var ids = [];
    for (var id in data) {
      if (Object.prototype.hasOwnProperty.call(data, id)) ids.push(id);
    }
    // Fresh simulate_action contexts may only carry raw store rows (entities
    // recorded during action execution) without __entityData entries.
    if (ids.length === 0) {
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
    if (matched.length === 0) filterLog('___filter-no-match');
    return globalThis.__makeEntityList(matched);
  };
})();"#
}
