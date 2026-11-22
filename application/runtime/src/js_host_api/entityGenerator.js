/**
 * EntityGenerator — a composable grammar over a set of entity blueprints.
 *
 * A blueprint is a template for an entity's field maps — it carries no id; the
 * caller supplies one at registration. Any textMap field value may itself be a
 * StringGenerator's generateValue() grammar, so a single blueprint can denote
 * many concrete entities — its *language* L(blueprint) = the cartesian
 * product of its generated fields' languages (numberMap fields are fixed).
 *
 * A generator exposes:
 *   - oneOf(blueprints) : L = union of the blueprints' (or sub-generators')
 *       languages.
 *   - generateValue()   -> MaybeExpression<EntityReadOnlyExpression>
 *       Instantiates a new entity from a random concrete blueprint (drawn from
 *       the shared seeded LXM stream), registers it in the world via setEntity
 *       under a fresh id, and returns a read-only EntityExpression over it.
 *       Absent (maybe.none) when the language is empty (oneOf([])).
 *   - isGeneratedBy(candidate) -> ConditionExpression
 *       Exact field membership: true iff `candidate` (an entity or blueprint)
 *       matches a member of the language (id is not considered).
 *
 * This file is self-contained (no dependencies) and UMD: in Node it sets
 * module.exports; when eval'd by the embedded runtime it sets
 * globalThis.__entityGeneratorFactory.
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.__entityGeneratorFactory = factory();
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';

  // ---------------------------------------------------------------------------
  // Maybe — minimal MaybeExpression with the methods callers need to unwrap.
  // ---------------------------------------------------------------------------
  function maybeOf(v) {
    return {
      __isMaybe: true,
      value: v,
      isPresent: function () { return conditionOf(true); },
      isEmpty: function () { return conditionOf(false); },
      orElse: function (d) { return v; },
      ifPresent: function (cb) { if (typeof cb === 'function') cb(v); },
      map: function (f) { return maybeOf(f(v)); },
      flatMap: function (f) { return f(v); }
    };
  }

  function maybeNone() {
    return {
      __isMaybe: true,
      value: undefined,
      isPresent: function () { return conditionOf(false); },
      isEmpty: function () { return conditionOf(true); },
      orElse: function (d) { return d; },
      ifPresent: function () {},
      map: function () { return maybeNone(); },
      flatMap: function () { return maybeNone(); }
    };
  }

  // A present MaybeExpression whose ifPresent/orElse wrapper is `wrapper`
  // (carrying _raw/value so callers can read the concrete value out).
  function presentMaybe(wrapper) {
    return {
      __isMaybe: true,
      value: wrapper,
      isPresent: function () { return conditionOf(true, wrapper); },
      isEmpty: function () { return conditionOf(false, wrapper); },
      orElse: function () { return wrapper; },
      ifPresent: function (cb) { if (typeof cb === 'function') cb(wrapper); },
      map: function (fn) {
        var r;
        try { r = fn(wrapper); } catch (e) { return maybeNone(); }
        return presentMaybe(r);
      },
      flatMap: function (fn) {
        var r;
        try { r = fn(wrapper); } catch (e) { return maybeNone(); }
        return (r && r.__isMaybe) ? r : presentMaybe(r);
      }
    };
  }

  // ---------------------------------------------------------------------------
  // Condition — minimal ConditionExpression.
  // ---------------------------------------------------------------------------
  function asBool(x) {
    if (x && typeof x === 'object' && x.value !== undefined) return !!x.value;
    return !!x;
  }

  function conditionOf(v, carried) {
    var b = !!v;
    return {
      __isCondition: true,
      value: b,
      __carried: carried,
      and: function (o) { return conditionOf(b && asBool(o), carried); },
      or: function (o) { return conditionOf(b || asBool(o), carried); },
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

  // ---------------------------------------------------------------------------
  // Value resolution.
  // ---------------------------------------------------------------------------
  // Resolve a string-ish field (plain string | StringExpression | a language-
  // bearing grammar | a Maybe-wrapped value) to its language: the array of
  // concrete strings it can denote.
  function fieldLanguage(e) {
    if (e && typeof e === 'object') {
      if (e.__isMaybe) return fieldLanguage(e.value);
      if (Array.isArray(e.__lang)) return e.__lang.slice();
      if (typeof e.value === 'string') return [e.value];
    }
    if (typeof e === 'string') return [e];
    return [];
  }

  // Resolve a number-ish field to a concrete number. Numbers have no generator
  // grammar, so a blueprint's numberMap fields are fixed values.
  function numberValue(e) {
    if (e && typeof e === 'object') {
      if (e._raw !== undefined) return Number(e._raw);
      if (typeof e.value === 'number') return e.value;
    }
    return Number(e);
  }

  // Own string keys of a plain object, in insertion order.
  function ownKeys(o) {
    var out = [];
    for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) out.push(k);
    return out;
  }

  // Cartesian product of an array of dimension arrays: each result is one pick
  // from every dimension, in order.
  function cartesian(dims) {
    var result = [ [] ];
    for (var d = 0; d < dims.length; d++) {
      var next = [];
      for (var r = 0; r < result.length; r++) {
        for (var v = 0; v < dims[d].length; v++) next.push(result[r].concat([dims[d][v]]));
      }
      result = next;
    }
    return result;
  }

  // A blueprint spec -> its concrete instantiations: each is
  // { textMap: {k: string}, numberMap: {k: number} }. A blueprint has no id;
  // the caller supplies one at registration.
  function blueprintInstantiations(spec) {
    if (!spec || typeof spec !== 'object') return [];
    var textMap = spec.textMap || {};
    var textKeys = ownKeys(textMap);
    var textLangs = [];
    for (var t = 0; t < textKeys.length; t++) textLangs.push(fieldLanguage(textMap[textKeys[t]]));
    var numberMap = spec.numberMap || {};
    var numKeys = ownKeys(numberMap);
    var resolvedNum = {};
    for (var n = 0; n < numKeys.length; n++) resolvedNum[numKeys[n]] = numberValue(numberMap[numKeys[n]]);

    var combos = cartesian(textLangs);
    var out = [];
    for (var i = 0; i < combos.length; i++) {
      var c = combos[i];
      var inst = { textMap: {}, numberMap: {} };
      for (var a = 0; a < textKeys.length; a++) inst.textMap[textKeys[a]] = c[a];
      for (var b = 0; b < numKeys.length; b++) inst.numberMap[numKeys[b]] = resolvedNum[numKeys[b]];
      out.push(inst);
    }
    return out;
  }

  // Canonical key for an instantiation (for dedupe + membership).
  function instKey(inst) {
    var tk = [];
    var t = inst.textMap || {};
    for (var i = 0; i < ownKeys(t).length; i++) { var k = ownKeys(t)[i]; tk.push(k + '=' + t[k]); }
    tk.sort();
    var nk = [];
    var n = inst.numberMap || {};
    for (var j = 0; j < ownKeys(n).length; j++) { var m = ownKeys(n)[j]; nk.push(m + '=' + n[m]); }
    nk.sort();
    return tk.join(',') + '||' + nk.join(',');
  }

  function dedupeInsts(instances) {
    var seen = Object.create(null);
    var out = [];
    for (var i = 0; i < instances.length; i++) {
      var key = instKey(instances[i]);
      if (!seen[key]) { seen[key] = 1; out.push(instances[i]); }
    }
    return out;
  }

  // The current host's runtime (resolved lazily so setEntity is called in the
  // calling context).
  function hostRt() {
    if (globalThis.hostApi && globalThis.hostApi.runtime) return globalThis.hostApi.runtime;
    if (globalThis.host) return globalThis.host;
    return null;
  }

  // A number value wrapper: comparison/transform surface over a concrete
  // number, carrying _raw/value for extraction.
  function numWrap(v) {
    v = Number(v);
    function numOf(x) {
      return (typeof x === 'object' && x !== null)
        ? (x._raw !== undefined ? x._raw : x.value) : x;
    }
    var self = {
      _raw: v,
      value: v,
      valueOf: function () { return v; },
      of: function (x) { return numWrap(Number(numOf(x))); },
      toString: function () { return String(v); },
      toJSON: function () { return v; },
      isGreaterThan: function (t) { return conditionOf(v > Number(numOf(t)), self); },
      isLessThan: function (t) { return conditionOf(v < Number(numOf(t)), self); },
      isGreaterOrEqualTo: function (t) { return conditionOf(v >= Number(numOf(t)), self); },
      isLessOrEqualTo: function (t) { return conditionOf(v <= Number(numOf(t)), self); },
      isEqualTo: function (t) { return conditionOf(v === Number(numOf(t)), self); },
      isNotEqualTo: function (t) { return conditionOf(v !== Number(numOf(t)), self); },
      sum: function (d) { return numWrap(v + Number(numOf(d))); },
      subtract: function (d) { return numWrap(v - Number(numOf(d))); },
      multiply: function (d) { return numWrap(v * Number(numOf(d))); },
      divide: function (d) { return numWrap(v / Number(numOf(d))); },
      modulo: function (d) { return numWrap(v % Number(numOf(d))); }
    };
    return self;
  }

  // A string value wrapper: comparison/transform surface over a concrete
  // string, carrying _raw/value for extraction.
  function strWrap(v) {
    v = String(v);
    function strOf(x) {
      return (typeof x === 'object' && x !== null)
        ? (x._raw !== undefined ? x._raw : x.value) : x;
    }
    var self = {
      _raw: v,
      value: v,
      of: function (x) { return strWrap(String(strOf(x))); },
      toString: function () { return String(v); },
      toJSON: function () { return v; },
      isContaining: function (o) { return conditionOf(String(v).indexOf(String(strOf(o))) >= 0, self); },
      isContainingExactly: function (o) { return conditionOf(String(v) === String(strOf(o)), self); },
      map: function (s) { return strWrap(String(strOf(s))); },
      concat: function (o) { return strWrap(String(v) + String(strOf(o))); }
    };
    return self;
  }

  // Build a read-only EntityExpression over a concrete entity (id + plain
  // field maps). Self-contained: it does not depend on the host's
  // __entityProxy helper, which is not exposed to the module context. The
  // plain maps are also carried (__textMap/__numberMap) so isGeneratedBy can
  // resolve a candidate without round-tripping through __entityData.
  function makeEntityProxy(id, textMap, numberMap) {
    var tm = textMap || {};
    var nm = numberMap || {};
    var proxy = {
      __id: id,
      __textMap: textMap,
      __numberMap: numberMap,
      id: {
        isContaining: function (o) { return conditionOf(String(id).indexOf(String(o)) >= 0, { _raw: id }); },
        isContainingExactly: function (o) { return conditionOf(String(id) === String(o), { _raw: id }); }
      },
      number_map: {
        get: function (key) {
          if (!Object.prototype.hasOwnProperty.call(nm, key)) return maybeNone();
          return presentMaybe(numWrap(nm[key]));
        }
      },
      text_map: {
        get: function (key) {
          if (!Object.prototype.hasOwnProperty.call(tm, key)) return maybeNone();
          return presentMaybe(strWrap(tm[key]));
        }
      }
    };
    return proxy;
  }

  // Register a concrete instantiation as a new entity and return a read-only
  // EntityExpression over it. Plain field values (raw strings/numbers) are
  // stored so both the entity proxy and the Rust sync read clean values.
  function instantiateEntity(inst) {
    globalThis.__lxmEntityGenSeq = (globalThis.__lxmEntityGenSeq || 0) + 1;
    var id = 'gen_' + globalThis.__lxmEntityGenSeq;
    var textMap = {};
    var t = inst.textMap || {};
    for (var i = 0; i < ownKeys(t).length; i++) { var k = ownKeys(t)[i]; textMap[k] = t[k]; }
    var numberMap = {};
    var n = inst.numberMap || {};
    for (var j = 0; j < ownKeys(n).length; j++) { var m = ownKeys(n)[j]; numberMap[m] = n[m]; }
    var rt = hostRt();
    if (rt && typeof rt.setEntity === 'function') rt.setEntity({ id: id, textMap: textMap, numberMap: numberMap });
    return makeEntityProxy(id, textMap, numberMap);
  }

  // Resolve a field value (plain or expression) to its concrete value.
  function resolveFieldValue(e) {
    if (e && typeof e === 'object') {
      if (e._raw !== undefined) return e._raw;
      if (e.value !== undefined) return e.value;
    }
    return e;
  }

  // Resolve a field map (plain values or expressions) to concrete values.
  function resolveMap(map) {
    var out = {};
    var o = map || {};
    for (var i = 0; i < ownKeys(o).length; i++) { var k = ownKeys(o)[i]; out[k] = resolveFieldValue(o[k]); }
    return out;
  }

  // Resolve a candidate (a generated entity proxy, or a field-map spec) to a
  // comparable shape. A proxy built by this generator carries its plain field
  // maps (__textMap/__numberMap); a host proxy carries __id (data read from
  // __entityData); a plain spec carries textMap/numberMap.
  function resolveCandidate(candidate) {
    if (!candidate || typeof candidate !== 'object') return null;
    if (candidate.__textMap !== undefined || candidate.__numberMap !== undefined) {
      return { textMap: resolveMap(candidate.__textMap), numberMap: resolveMap(candidate.__numberMap) };
    }
    if (candidate.__id && globalThis.__entityData && globalThis.__entityData[candidate.__id]) {
      var data = globalThis.__entityData[candidate.__id];
      return { textMap: resolveMap(data.textMap), numberMap: resolveMap(data.numberMap) };
    }
    var textMap = resolveMap(candidate.textMap || candidate.text_map);
    var numberMap = resolveMap(candidate.numberMap || candidate.number_map);
    return { textMap: textMap, numberMap: numberMap };
  }

  // Exact field-by-field equality between an instantiation and a resolved candidate.
  function instMatches(inst, resolved) {
    if (resolved == null) return false;
    var t = inst.textMap || {};
    var rt = resolved.textMap || {};
    for (var i = 0; i < ownKeys(t).length; i++) {
      var k = ownKeys(t)[i];
      if (String(t[k]) !== String(rt[k])) return false;
    }
    var n = inst.numberMap || {};
    var rn = resolved.numberMap || {};
    for (var j = 0; j < ownKeys(n).length; j++) {
      var m = ownKeys(n)[j];
      if (Number(n[m]) !== Number(rn[m])) return false;
    }
    return true;
  }

  // ---------------------------------------------------------------------------
  // Generator.
  // ---------------------------------------------------------------------------
  function makeGenerator(instances) {
    var insts = dedupeInsts(instances);
    return {
      __entityGenerator: true,
      __insts: insts,
      generateValue: function () {
        if (insts.length === 0) return maybeNone();
        var idx = Math.floor(globalThis.__lxmNext() * insts.length);
        if (idx >= insts.length) idx = insts.length - 1;
        var proxy = instantiateEntity(insts[idx]);
        return proxy ? maybeOf(proxy) : maybeNone();
      },
      isGeneratedBy: function (candidate) {
        var resolved = resolveCandidate(candidate);
        var ok = false;
        for (var i = 0; i < insts.length; i++) {
          if (instMatches(insts[i], resolved)) { ok = true; break; }
        }
        return conditionOf(ok, candidate);
      }
    };
  }

  // L(oneOf(arr)) = union of the elements' languages.
  function oneOf(blueprints) {
    var acc = [];
    var arr = blueprints || [];
    for (var i = 0; i < arr.length; i++) {
      var el = arr[i];
      var insts = (el && el.__entityGenerator) ? el.__insts : blueprintInstantiations(el);
      for (var j = 0; j < insts.length; j++) acc.push(insts[j]);
    }
    return makeGenerator(acc);
  }

  return {
    create: function () {
      return {
        oneOf: oneOf,
        type: { __entityGeneratorApi: true }
      };
    }
  };
});
