/**
 * StringGenerator — a composable grammar over a regular language of strings.
 *
 * See specification/expressions/stringGenerator.md.
 *
 * A generator denotes a *language* L(gen): the set of concrete strings it can
 * produce. It is built with:
 *   - oneOf(elements)  : L = union of the elements' languages
 *   - concat(elements) : L = ordered concatenation (product) of the elements'
 *
 * A generator exposes exactly two operations:
 *   - generateValue()  -> MaybeExpression<StringExpression>
 *       The generator -> StringExpression conversion. Present (maybe.of) with the
 *       language grammar when L is non-empty; absent (maybe.none) when L is empty
 *       (the oneOf([]) case). The concrete random string emerges when the returned
 *       StringExpression is evaluated.
 *   - isGeneratedBy(candidate) -> ConditionExpression
 *       Full-match membership: true iff `candidate` (evaluated to a concrete
 *       string) is a member of L. NOT a substring test.
 *
 * Chaining: because oneOf/concat take Array<StringExpression>, a sub-generator is
 * spliced in by converting it (sub.generateValue().orElse(fallback)) and passing
 * the resulting StringExpression into the outer array. The language-bearing
 * grammar carries the full set of strings, so composition keeps the full language.
 *
 * This file is self-contained (no dependencies) and UMD: in Node it sets
 * module.exports; when eval'd by the embedded runtime it sets
 * globalThis.__stringGeneratorFactory.
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.__stringGeneratorFactory = factory();
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

  function conditionOf(v) {
    var b = !!v;
    return {
      __isCondition: true,
      value: b,
      and: function (o) { return conditionOf(b && asBool(o)); },
      or: function (o) { return conditionOf(b || asBool(o)); },
      negate: function () { return conditionOf(!b); },
      ifTrue: function (cb) { if (b && typeof cb === 'function') cb(); },
      ifFalse: function (cb) { if (!b && typeof cb === 'function') cb(); }
    };
  }

  // ---------------------------------------------------------------------------
  // Language helpers.
  // ---------------------------------------------------------------------------

  // Resolve an element to its language (array of concrete strings).
  // An element is one of:
  //   - a plain string                      -> [that string]
  //   - a language-bearing grammar (__langExpr)  -> its full language
  //   - a generator (__generator)                 -> its full language
  //   - (defensively) a Maybe-wrapped value -> recurse into .value
  function languageOfElement(e) {
    if (e && typeof e === 'object') {
      if (e.__langExpr) return e.__lang.slice();
      if (e.__generator) return e.__lang.slice();
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
  // strings (__lang) for composition, plus a representative concrete `value`
  // (a valid member) for downstream use / evaluation.
  function makeLangExpr(strings) {
    var lang = strings.slice();
    return {
      __langExpr: true,
      __lang: lang,
      value: lang.length ? lang[0] : undefined,
      toString: function () { return lang.length ? lang[0] : ''; },
      toJSON: function () { return lang.length ? lang[0] : ''; }
    };
  }

  function makeGenerator(language) {
    var lang = dedupe(language);
    return {
      __generator: true,
      __lang: lang,
      generateValue: function () {
        if (lang.length === 0) return maybeNone();
        return maybeOf(makeLangExpr(lang));
      },
      isGeneratedBy: function (candidate) {
        var s;
        if (typeof candidate === 'string') {
          s = candidate;
        } else if (candidate && typeof candidate === 'object' && candidate.value !== undefined) {
          s = String(candidate.value);
        } else {
          s = String(candidate);
        }
        return conditionOf(lang.indexOf(s) !== -1);
      }
    };
  }

  // L(oneOf(arr)) = union of the elements' languages.
  function oneOf(oneOfArray) {
    var acc = [];
    var arr = oneOfArray || [];
    for (var i = 0; i < arr.length; i++) {
      var l = languageOfElement(arr[i]);
      for (var j = 0; j < l.length; j++) acc.push(l[j]);
    }
    return makeGenerator(acc);
  }

  // L(concat(arr)) = ordered product of the elements' languages.
  function concat(elements) {
    var arr = elements || [];
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
    return makeGenerator(result);
  }

  return {
    create: function () {
      return {
        oneOf: oneOf,
        concat: concat,
        type: { __stringGeneratorApi: true }
      };
    }
  };
});
