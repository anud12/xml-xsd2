/**
 * StringBlueprint — a composable grammar over a regular language of strings.
 *
 * See specification/expressions/stringGenerator.md.
 *
 * A blueprint denotes a *language* L(blueprint): the set of concrete strings it
 * can produce. It is built with:
 *   - oneOf(makeList)  : L = union of the elements' languages
 *   - concat(elements) : L = ordered concatenation (product) of the elements'
 *
 * oneOf's `makeList` is a lambda () => Array<element>, evaluated at
 * generateValue()/isGeneratedBy() time, so the element set (and any generated
 * values) can be produced dynamically (it can reach the live hostApi through its
 * enclosing scope). concat takes a plain array (its sequence is fixed).
 *
 * A blueprint exposes exactly two operations:
 *   - generateValue()  -> MaybeExpression<StringExpression>
 *       The blueprint -> StringExpression conversion. Present (maybe.of) with the
 *       language grammar when L is non-empty; absent (maybe.none) when L is empty
 *       (the oneOf([]) case). The concrete random string emerges when the returned
 *       StringExpression is evaluated.
 *   - isGeneratedBy(candidate) -> ConditionExpression
 *       Full-match membership: true iff `candidate` (evaluated to a concrete
 *       string) is a member of L. NOT a substring test.
 *
 * Chaining: because oneOf/concat take a list of StringExpression (inside the
 * makeList lambda), a sub-blueprint is spliced in by converting it
 * (sub.generateValue().orElse(fallback)) and passing the resulting
 * StringExpression into the outer list. The language-bearing grammar carries the
 * full set of strings, so composition keeps the full language. A sub-blueprint
 * may also be passed directly as an element.
 *
 * This file is self-contained (no dependencies) and UMD: in Node it sets
 * module.exports; when eval'd by the embedded runtime it sets
 * globalThis.__stringBlueprintFactory.
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.__stringBlueprintFactory = factory();
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
  // Language helpers.
  // ---------------------------------------------------------------------------

  // Resolve an element to its language (array of concrete strings).
  // An element is one of:
  //   - a plain string                      -> [that string]
  //   - a language-bearing grammar (__langExpr)  -> its full language
  //   - a blueprint (__stringBlueprint)             -> its full language
  //   - (defensively) a Maybe-wrapped value -> recurse into .value
   function languageOfElement(e) {
    if (e && typeof e === 'object') {
      if (e.__langExpr) return e.__lang.slice();
      if (e.__stringBlueprint) return e.language();
      if (e.__isMaybe) return languageOfElement(e.value);
    }
    if (typeof e === 'string') return [e];
    return [];
  }

  function dedupe(arr) {
    var seen = Object.create(null);
    var out = [];
    for (var i = 0; i < arr.length; i++) {
      var s = arr[i];
      if (!seen[s]) { seen[s] = 1; out.push(s); }
    }
    return out;
  }

  // The StringExpression that carries a language: it exposes the full set of
  // strings (__lang) for composition, plus a concrete `value` (a valid member)
  // for downstream use / evaluation. The concrete member is chosen at random
  // from the language via the shared seeded LXM stream (__lxmNext), so the
  // pick is governed by the host seed and reproducible. Empty language ->
  // undefined.
  function makeLangExpr(strings) {
    var lang = strings.slice();
    var concrete;
    if (lang.length > 0) {
      var idx = Math.floor(globalThis.__lxmNext() * lang.length);
      if (idx >= lang.length) idx = lang.length - 1;
      concrete = lang[idx];
    }
    return {
      __langExpr: true,
      __lang: lang,
      value: concrete,
      toString: function () { return concrete !== undefined ? concrete : ''; },
      toJSON: function () { return concrete !== undefined ? concrete : ''; }
    };
  }

  // A lazy blueprint: the language is produced by computeLanguage and is
  // re-evaluated at every generateValue() / isGeneratedBy() call so it can
  // produce values dynamically.
  function makeLazyBlueprint(computeLanguage) {
    return {
      __stringBlueprint: true,
      language: function () {
        return dedupe(computeLanguage());
      },
      generateValue: function () {
        var lang = dedupe(computeLanguage());
        if (lang.length === 0) return maybeNone();
        return maybeOf(makeLangExpr(lang));
      },
      isGeneratedBy: function (candidate) {
        var lang = dedupe(computeLanguage());
        var s;
        if (typeof candidate === 'string') {
          s = candidate;
        } else if (candidate && typeof candidate === 'object' && candidate.value !== undefined) {
          s = String(candidate.value);
        } else {
          s = String(candidate);
        }
        return conditionOf(lang.indexOf(s) !== -1, candidate);
      }
    };
  }

  // L(oneOf(makeList)) = union of the elements' languages. makeList is a lambda
  // () => Array<element>, evaluated at generateValue()/isGeneratedBy() time for
  // dynamic generation (it can reach the live hostApi through its scope).
  function oneOf(makeList) {
    return makeLazyBlueprint(function () {
      var acc = [];
      var arr = (typeof makeList === 'function') ? (makeList() || []) : (makeList || []);
      for (var i = 0; i < arr.length; i++) {
        var l = languageOfElement(arr[i]);
        for (var j = 0; j < l.length; j++) acc.push(l[j]);
      }
      return acc;
    });
  }

  // L(concat(elements)) = ordered product of the elements' languages. Takes a
  // plain array of elements (their languages are resolved at use time). Unlike
  // oneOf it does not take a hostApi list-maker — the sequence is fixed, not a
  // host-driven choice.
  function concat(elements) {
    var arr = elements || [];
    return makeLazyBlueprint(function () {
      var langs = [];
      for (var i = 0; i < arr.length; i++) langs.push(languageOfElement(arr[i]));

      var result = [''];
      for (var k = 0; k < langs.length; k++) {
        var next = [];
        for (var a = 0; a < result.length; a++) {
          for (var b = 0; b < langs[k].length; b++) {
            next.push(result[a] + langs[k][b]);
          }
        }
        result = next;
      }
      return result;
    });
  }

  return {
    create: function () {
      return {
        oneOf: oneOf,
        concat: concat,
        type: { __stringBlueprintApi: true }
      };
    }
  };
});
