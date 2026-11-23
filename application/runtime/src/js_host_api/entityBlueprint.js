/**
 * EntityBlueprint — a composable grammar over a set of entity templates.
 *
 * A template is the field maps an entity blueprint instantiates — it carries no
 * id; the caller supplies one at registration. Any textMap field value may
 * itself be a StringBlueprint's generateValue() grammar, and any numberMap field
 * may be a NumberBlueprint's generateValue() grammar (or a fixed number), so a
 * single template can denote many concrete entities — its *language*
 * L(template) = the cartesian product of its generated fields' languages.
 *
 * A blueprint is built with oneOf(makeList), where makeList is a lambda
 * () => Array<template | subBlueprint>. The lambda is evaluated at
 * generateValue()/isGeneratedBy() time, so the template set — and any generated
 * field values — can be produced dynamically (it can reach the live hostApi
 * through its enclosing scope).
 *
 * A blueprint exposes:
 *   - oneOf(makeList)   : L = union of the templates' (or sub-blueprints')
 *       languages.
 *   - generateValue()   -> EntityReadOnlyExpression
 *       Instantiates a new entity from a random concrete template (drawn from
 *       the shared seeded LXM stream), registers it in the world via setEntity
 *       under a fresh id, and returns a read-only EntityExpression over it.
 *       Throws when the language is empty.
 *   - isGeneratedBy(candidate) -> ConditionExpression
 *       Exact field membership: true iff `candidate` (an entity or template)
 *       matches a member of the language (id is not considered).
 *
 * This file is self-contained (no dependencies) and UMD: in Node it sets
 * module.exports; when eval'd by the embedded runtime it sets
 * globalThis.__entityBlueprintFactory.
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.__entityBlueprintFactory = factory();
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';

  // ---------------------------------------------------------------------------
  // Maybe — minimal MaybeExpression with the methods callers need to unwrap.
  // ---------------------------------------------------------------------------
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
  // Own string keys of a plain object, in insertion order.
  // ---------------------------------------------------------------------------
  function ownKeys(o) {
    var out = [];
    for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) out.push(k);
    return out;
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
      if (e.__stringBlueprint) return e.language();
      if (Array.isArray(e.__lang)) return e.__lang.slice();
      if (typeof e.value === 'string') return [e.value];
    }
    if (typeof e === 'string') return [e];
    return [];
  }

  // Resolve a number-ish field to its integer pieces. A field is one of:
  //   - a fixed number / NumberExpression ({value|_raw})  -> a singleton piece
  //   - a NumberBlueprint (__numberBlueprint)             -> its pieces
  //   - (defensively) a Maybe-wrapped value              -> recurse into .value
  function numberPieces(e) {
    if (e && typeof e === 'object') {
      if (e.__numberBlueprint) return e.pieces();
      if (e.__isMaybe) return numberPieces(e.value);
      if (e._raw !== undefined) { var n = Math.floor(Number(e._raw)); if (isFinite(n)) return [{ lo: n, hi: n }]; return []; }
      if (typeof e.value === 'number') { var m = Math.floor(Number(e.value)); if (isFinite(m)) return [{ lo: m, hi: m }]; return []; }
    }
    var nn = Number(e);
    if (isFinite(nn)) return [{ lo: Math.floor(nn), hi: Math.floor(nn) }];
    return [];
  }

  // ---------------------------------------------------------------------------
  // Integer-piece helpers. A piece is { lo, hi } (inclusive, lo <= hi).
  // ---------------------------------------------------------------------------
  function piecesTotal(pieces) {
    var total = 0;
    for (var i = 0; i < pieces.length; i++) total += (pieces[i].hi - pieces[i].lo + 1);
    return total;
  }

  function inPieces(n, pieces) {
    n = Math.floor(Number(n));
    if (!isFinite(n)) return false;
    for (var i = 0; i < pieces.length; i++) {
      if (n >= pieces[i].lo && n <= pieces[i].hi) return true;
    }
    return false;
  }

  // A random integer drawn uniformly across all pieces (weighted by piece size)
  // using the shared seeded LXM stream.
  function randomInt(pieces) {
    var total = piecesTotal(pieces);
    if (total <= 0) return 0;
    var r = globalThis.__lxmNext() * total;
    if (r < 0) r = 0;
    var acc = 0;
    for (var i = 0; i < pieces.length; i++) {
      var size = pieces[i].hi - pieces[i].lo + 1;
      if (r < acc + size) return pieces[i].lo + Math.floor(r - acc);
      acc += size;
    }
    return pieces[pieces.length - 1].hi;
  }

  // A template: { textMap: {k: string[]}, numberMap: {k: pieces[]} }.
  function parseTemplate(spec) {
    if (!spec || typeof spec !== 'object') return null;
    var textMap = spec.textMap || {};
    var tkeys = ownKeys(textMap);
    var t = { textMap: {}, numberMap: {} };
    for (var i = 0; i < tkeys.length; i++) t.textMap[tkeys[i]] = fieldLanguage(textMap[tkeys[i]]);
    var numberMap = spec.numberMap || {};
    var nkeys = ownKeys(numberMap);
    for (var j = 0; j < nkeys.length; j++) t.numberMap[nkeys[j]] = numberPieces(numberMap[nkeys[j]]);
    return t;
  }

  // A template with an empty dimension (a text field with no strings, or a
  // number field with no integers) can never instantiate, so it is excluded.
  function templateIsEmpty(t) {
    var tkeys = ownKeys(t.textMap || {});
    for (var i = 0; i < tkeys.length; i++) {
      if ((t.textMap[tkeys[i]] || []).length === 0) return true;
    }
    var nkeys = ownKeys(t.numberMap || {});
    for (var j = 0; j < nkeys.length; j++) {
      if (piecesTotal(t.numberMap[nkeys[j]] || []) === 0) return true;
    }
    return false;
  }

  // Canonical key for a template (for dedupe).
  function templateKey(t) {
    var parts = [];
    var tkeys = ownKeys(t.textMap || {}).slice().sort();
    for (var i = 0; i < tkeys.length; i++) {
      var arr = (t.textMap[tkeys[i]] || []).slice().sort();
      parts.push('t' + tkeys[i] + '=' + arr.join('~'));
    }
    var nkeys = ownKeys(t.numberMap || {}).slice().sort();
    for (var j = 0; j < nkeys.length; j++) {
      var ps = (t.numberMap[nkeys[j]] || []).slice().sort(function (a, b) { return a.lo - b.lo || a.hi - b.hi; });
      var s = '';
      for (var k = 0; k < ps.length; k++) s += '[' + ps[k].lo + ',' + ps[k].hi + ']';
      parts.push('n' + nkeys[j] + '=' + s);
    }
    parts.sort();
    return parts.join('|');
  }

  function dedupeTemplates(templates) {
    var seen = Object.create(null);
    var out = [];
    for (var i = 0; i < templates.length; i++) {
      if (templates[i] == null) continue;
      var key = templateKey(templates[i]);
      if (!seen[key]) { seen[key] = 1; out.push(templates[i]); }
    }
    return out;
  }

  // Resolve lazily so setEntity runs in the calling context.
  function hostApiObj() {
    if (globalThis.hostApi) return globalThis.hostApi;
    if (globalThis.host) return { runtime: globalThis.host };
    return {};
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

  function emptyEntityList() {
    return {
      forEach: function () {},
      map: emptyEntityList,
      findBy: emptyEntityList,
      hasEntity: function () { return conditionOf(false); },
      size: function () { return 0; },
      count: function () { return numWrap(0); },
      length: function () { return numWrap(0); },
      get: maybeNone,
      exactlyOne: maybeNone,
      randomElement: maybeNone
    };
  }

  // A generated read-only expression keeps its concrete field values.
  // Membership and area queries use the registered entity when hosted.
  function makeEntityProxy(id, textMap, numberMap, registered) {
    var tm = textMap || {};
    var nm = numberMap || {};
    var proxy = {
      __id: id,
      __textMap: textMap,
      __numberMap: numberMap,
      // String coercion and serialization match the hosted entity expression.
      value: id,
      toString: function () { return id; },
      toJSON: function () { return id; },
      id: strWrap(id),
      members: function (list) {
        return registered ? registered.members(list) : emptyEntityList();
      },
      parentsInKey: function () { return emptyEntityList(); },
      getEntitiesInsideArea: function (area) {
        return registered ? registered.getEntitiesInsideArea(area) : emptyEntityList();
      },
      number_map: {
        get: function (key) {
          if (!Object.prototype.hasOwnProperty.call(nm, key)) return maybeNone();
          return presentMaybe(numWrap(nm[key]));
        }
      },
      string_map: {
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
  function instantiateEntity(concrete) {
    globalThis.__lxmEntityGenSeq = (globalThis.__lxmEntityGenSeq || 0) + 1;
    var id = 'gen_' + globalThis.__lxmEntityGenSeq;
    var textMap = {};
    var t = concrete.textMap || {};
    for (var i = 0; i < ownKeys(t).length; i++) { var k = ownKeys(t)[i]; textMap[k] = t[k]; }
    var numberMap = {};
    var n = concrete.numberMap || {};
    for (var j = 0; j < ownKeys(n).length; j++) { var m = ownKeys(n)[j]; numberMap[m] = n[m]; }
    var rt = hostApiObj().runtime;
    var registered = rt && typeof rt.setEntity === 'function'
      ? rt.setEntity({ id: rt.string.of(id), stringMap: textMap, numberMap: numberMap })
      : undefined;
    return makeEntityProxy(id, textMap, numberMap, registered);
  }

  // Pick one concrete value per field from a (non-empty) template.
  function randomInstantiate(t) {
    var textMap = {};
    var tkeys = ownKeys(t.textMap || {});
    for (var i = 0; i < tkeys.length; i++) {
      var arr = t.textMap[tkeys[i]] || [];
      var idx = Math.floor(globalThis.__lxmNext() * arr.length);
      if (idx >= arr.length) idx = arr.length - 1;
      textMap[tkeys[i]] = arr[idx];
    }
    var numberMap = {};
    var nkeys = ownKeys(t.numberMap || {});
    for (var j = 0; j < nkeys.length; j++) {
      numberMap[nkeys[j]] = randomInt(t.numberMap[nkeys[j]] || []);
    }
    return { textMap: textMap, numberMap: numberMap };
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
  // comparable shape. Standalone expressions carry their concrete maps;
  // hosted expressions resolve by id; a plain spec carries textMap/numberMap.
  function resolveCandidate(candidate) {
    if (!candidate || typeof candidate !== 'object') return null;
    if (candidate.__textMap !== undefined || candidate.__numberMap !== undefined) {
      return { textMap: resolveMap(candidate.__textMap), numberMap: resolveMap(candidate.__numberMap) };
    }
    var id = globalThis.__entityId && globalThis.__entityId(candidate);
    var data = id && globalThis.__readEntity(id);
    if (data) {
      return { textMap: resolveMap(data.stringMap), numberMap: resolveMap(data.numberMap) };
    }
    var textMap = resolveMap(candidate.textMap || candidate.string_map);
    var numberMap = resolveMap(candidate.numberMap || candidate.number_map);
    return { textMap: textMap, numberMap: numberMap };
  }

  // Field-by-field membership of a resolved candidate against a template.
  function matchesTemplate(t, resolved) {
    if (resolved == null) return false;
    var rt = resolved.textMap || {};
    var tkeys = ownKeys(t.textMap || {});
    for (var i = 0; i < tkeys.length; i++) {
      var k = tkeys[i];
      var arr = t.textMap[k] || [];
      var v = String(rt[k]);
      var found = false;
      for (var a = 0; a < arr.length; a++) {
        if (String(arr[a]) === v) { found = true; break; }
      }
      if (!found) return false;
    }
    var rn = resolved.numberMap || {};
    var nkeys = ownKeys(t.numberMap || {});
    for (var j = 0; j < nkeys.length; j++) {
      var m = nkeys[j];
      if (!inPieces(rn[m], t.numberMap[m] || [])) return false;
    }
    return true;
  }

  // ---------------------------------------------------------------------------
  // Blueprint (lazy). The template list (and hence the language) is produced by
  // computeTemplates and re-evaluated at every generateValue()/isGeneratedBy()
  // call so it can produce values dynamically.
  // ---------------------------------------------------------------------------
  function makeLazyBlueprint(computeTemplates) {
    var self = { __entityBlueprint: true };
    self.templates = function () {
      var ts = computeTemplates();
      var nonEmpty = [];
      for (var i = 0; i < ts.length; i++) {
        if (ts[i] != null && !templateIsEmpty(ts[i])) nonEmpty.push(ts[i]);
      }
      return dedupeTemplates(nonEmpty);
    };
    self.generateValue = function () {
      var ts = self.templates();
      if (ts.length === 0) throw new Error('Entity blueprint has an empty language');
      var idx = Math.floor(globalThis.__lxmNext() * ts.length);
      if (idx >= ts.length) idx = ts.length - 1;
      return instantiateEntity(randomInstantiate(ts[idx]));
    };
    self.isGeneratedBy = function (candidate) {
      var ts = self.templates();
      var resolved = resolveCandidate(candidate);
      var ok = false;
      for (var i = 0; i < ts.length; i++) {
        if (matchesTemplate(ts[i], resolved)) { ok = true; break; }
      }
      return conditionOf(ok, candidate);
    };
    return self;
  }

  // L(oneOf(makeList)) = union of the templates' (or sub-blueprints') languages.
  // makeList is a lambda () => Array<template | subBlueprint>, evaluated at
  // generateValue()/isGeneratedBy() time for dynamic generation (it can reach
  // the live hostApi through its enclosing scope).
  function oneOf(makeList) {
    return makeLazyBlueprint(function () {
      var acc = [];
      var arr = (typeof makeList === 'function') ? (makeList() || []) : (makeList || []);
      if (!Array.isArray(arr) || arr.length === 0) throw new Error('entityBlueprint.oneOf requires a non-empty array');
      for (var i = 0; i < arr.length; i++) {
        var el = arr[i];
        if (el && el.__entityBlueprint) {
          var sub = el.templates();
          for (var j = 0; j < sub.length; j++) acc.push(sub[j]);
        } else {
          var t = parseTemplate(el);
          if (t) acc.push(t);
        }
      }
      return acc;
    });
  }

  return {
    create: function () {
      return {
        oneOf: oneOf,
        type: { __entityBlueprintApi: true }
      };
    }
  };
});
