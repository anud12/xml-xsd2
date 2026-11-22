/**
 * NumberBlueprint — a composable grammar over a set of integers.
 *
 * Unlike a string language (a finite list), an integer language is represented
 * as a union of inclusive integer ranges ("pieces"). It is built with:
 *   - oneOf(makeList) : L = union of the elements' integer sets
 *   - range(from, to) : L = the single inclusive interval [from, to]
 *
 * oneOf's `makeList` is a lambda () => Array<element>, evaluated at
 * generateValue()/isGeneratedBy() time, so the set (and any generated values)
 * can be produced dynamically (it can reach the live hostApi through its
 * enclosing scope). An element is a fixed number, a NumberReadOnlyExpression
 * (including a sub-blueprint's generateValue() grammar), or a sub-blueprint
 * directly.
 *
 * A blueprint exposes:
 *   - generateValue()  -> MaybeExpression<NumberReadOnlyExpression>
 *       Present (maybe.of) with a random integer from L (drawn from the shared
 *       seeded LXM stream) when L is non-empty; absent (maybe.none) when L is
 *       empty (oneOf([]) or an empty range).
 *   - isGeneratedBy(candidate) -> ConditionExpression
 *       Membership: true iff `candidate` (evaluated to a concrete integer) is a
 *       member of L.
 *
 * This file is self-contained (no dependencies) and UMD: in Node it sets
 * module.exports; when eval'd by the embedded runtime it sets
 * globalThis.__numberBlueprintFactory.
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.__numberBlueprintFactory = factory();
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
  // Number extraction.
  // ---------------------------------------------------------------------------
  function numOf(x) {
    if (x && typeof x === 'object') {
      if (x._raw !== undefined) return Number(x._raw);
      if (x.value !== undefined) return Number(x.value);
    }
    return Number(x);
  }

  // ---------------------------------------------------------------------------
  // Piece helpers. A piece is { lo, hi } (inclusive integers, lo <= hi).
  // ---------------------------------------------------------------------------
  function normalizePieces(pieces) {
    var valid = [];
    for (var i = 0; i < pieces.length; i++) {
      var p = pieces[i];
      if (!p || typeof p !== 'object') continue;
      var lo = Math.floor(Number(p.lo));
      var hi = Math.floor(Number(p.hi));
      if (!isFinite(lo) || !isFinite(hi)) continue;
      if (lo > hi) { var t = lo; lo = hi; hi = t; }
      valid.push({ lo: lo, hi: hi });
    }
    valid.sort(function (a, b) { return a.lo - b.lo; });
    var merged = [];
    for (var k = 0; k < valid.length; k++) {
      var cur = valid[k];
      if (merged.length > 0 && cur.lo <= merged[merged.length - 1].hi + 1) {
        if (cur.hi > merged[merged.length - 1].hi) merged[merged.length - 1].hi = cur.hi;
      } else {
        merged.push({ lo: cur.lo, hi: cur.hi });
      }
    }
    return merged;
  }

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

  // Resolve an element to its integer pieces. An element is one of:
  //   - a number / NumberExpression ({value|_raw})  -> a singleton piece
  //   - a number blueprint (__numberBlueprint)       -> its pieces
  //   - (defensively) a Maybe-wrapped value         -> recurse into .value
  function piecesOfElement(e) {
    if (e && typeof e === 'object') {
      if (e.__numberBlueprint) return e.pieces();
      if (e.__isMaybe) return piecesOfElement(e.value);
      if (e._raw !== undefined) { var n = Math.floor(numOf(e)); if (isFinite(n)) return [{ lo: n, hi: n }]; return []; }
      if (typeof e.value === 'number') { var m = Math.floor(numOf(e)); if (isFinite(m)) return [{ lo: m, hi: m }]; return []; }
    }
    var nn = numOf(e);
    if (isFinite(nn)) return [{ lo: Math.floor(nn), hi: Math.floor(nn) }];
    return [];
  }

  // A number value wrapper: a self-contained NumberReadOnlyExpression surface
  // over a concrete integer, carrying _raw/value for extraction.
  function numWrap(v) {
    v = Math.floor(Number(v));
    function selfOf(x) { return numOf(x); }
    var self = {
      _raw: v,
      value: v,
      valueOf: function () { return v; },
      of: function (x) { return numWrap(selfOf(x)); },
      toString: function () { return String(v); },
      toJSON: function () { return v; },
      isGreaterThan: function (t) { return conditionOf(v > numOf(t), self); },
      isLessThan: function (t) { return conditionOf(v < numOf(t), self); },
      isGreaterOrEqualTo: function (t) { return conditionOf(v >= numOf(t), self); },
      isLessOrEqualTo: function (t) { return conditionOf(v <= numOf(t), self); },
      isEqualTo: function (t) { return conditionOf(v === numOf(t), self); },
      isNotEqualTo: function (t) { return conditionOf(v !== numOf(t), self); },
      sum: function (d) { return numWrap(v + numOf(d)); },
      subtract: function (d) { return numWrap(v - numOf(d)); },
      multiply: function (d) { return numWrap(v * numOf(d)); },
      divide: function (d) { return numWrap(v / numOf(d)); },
      modulo: function (d) { return numWrap(v % numOf(d)); }
    };
    return self;
  }

  // ---------------------------------------------------------------------------
  // Blueprint (lazy). The piece set (and hence the language) is produced by
  // computePieces and re-evaluated at every generateValue()/isGeneratedBy()
  // call so it can produce values dynamically.
  // ---------------------------------------------------------------------------
  function makeLazyBlueprint(computePieces) {
    return {
      __numberBlueprint: true,
      pieces: function () {
        return normalizePieces(computePieces());
      },
      generateValue: function () {
        var p = normalizePieces(computePieces());
        if (piecesTotal(p) === 0) return maybeNone();
        return maybeOf(numWrap(randomInt(p)));
      },
      isGeneratedBy: function (candidate) {
        return conditionOf(inPieces(numOf(candidate), normalizePieces(computePieces())), candidate);
      }
    };
  }

  // L(oneOf(makeList)) = union of the elements' integer sets. makeList is a
  // lambda () => Array<element>, evaluated at generateValue()/isGeneratedBy()
  // time for dynamic generation (it can reach the live hostApi through its
  // scope).
  function oneOf(makeList) {
    return makeLazyBlueprint(function () {
      var acc = [];
      var arr = (typeof makeList === 'function') ? (makeList() || []) : (makeList || []);
      for (var i = 0; i < arr.length; i++) {
        var p = piecesOfElement(arr[i]);
        for (var j = 0; j < p.length; j++) acc.push(p[j]);
      }
      return acc;
    });
  }

  // L(range(from, to)) = the single inclusive integer interval [from, to],
  // drawn from the shared seeded LXM stream. An empty interval (from > to)
  // yields an empty language.
  function range(fromInclusive, toInclusive) {
    return makeLazyBlueprint(function () {
      var lo = numOf(fromInclusive);
      var hi = numOf(toInclusive);
      if (!isFinite(lo) || !isFinite(hi)) return [];
      lo = Math.floor(lo);
      hi = Math.floor(hi);
      if (lo > hi) return [];
      return [{ lo: lo, hi: hi }];
    });
  }

  return {
    create: function () {
      return {
        oneOf: oneOf,
        range: range,
        type: { __numberBlueprintApi: true }
      };
    }
  };
});
