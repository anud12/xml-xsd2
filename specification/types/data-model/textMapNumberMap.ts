import type { StringExpression } from '../primitives/stringExpression';
import type { NumberExpression } from '../primitives/numberExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';

/**
 * Data-model snapshot of a text map: a keyed collection of StringExpression
 * values attached to an Entity or Container.
 *
 * Keys are plain strings; values are StringExpression wrappers.
 *
 * @see textMap&numberMap.md
 */
export type TextMap = {
  [name: string]: StringExpression;
};

/**
 * Data-model snapshot of a number map: a keyed collection of NumberExpression
 * values attached to an Entity or Container.
 *
 * Keys are plain strings; values are NumberExpression wrappers.
 *
 * @see textMap&numberMap.md
 */
export type NumberMap = {
  [name: string]: NumberExpression;
};

/**
 * HostApi factory for creating {@link TextMapExpression} builders.
 *
 * Exposed as `hostApi.textMap` inside module scripts.
 *
 * @see TextMapExpression
 * @see textMap&numberMap.md
 */
export type TextMapExpressionApi = {
  /**
   * Create an empty TextMapExpression builder.
   */
  create: () => TextMapExpression;
};

/**
 * An immutable, lazily-evaluated builder for a keyed map of string values.
 *
 * Methods return a new TextMapExpression (immutable). Existence and equality
 * checks return lazy {@link ConditionExpression} nodes.
 *
 * @see TextMapExpressionApi
 * @see textMap&numberMap.md
 */
export type TextMapExpression = {
  /**
   * Insert or replace the value at `key` with the provided StringExpression.
   *
   * Returns a new TextMapExpression with the updated entry. Overwrites any
   * existing value for that key.
   *
   * @param key   - Map key.
   * @param value - String value expression to associate with the key.
   */
  put: (key: string, value: StringExpression) => TextMapExpression;

  /**
   * Remove the entry for `key`.
   *
   * Optional — not all implementations need to support removal.
   */
  remove?: (key: string) => TextMapExpression;

  /**
   * Retrieve the StringExpression associated with `key`.
   *
   * If the key is absent, implementations should return an empty string
   * expression (fail-soft) and log the access.
   */
  get: (key: string) => StringExpression;

  /**
   * Returns a ConditionExpression that is true when `key` is present in the
   * map.
   */
  has: (key: string) => ConditionExpression;

  /**
   * Returns a ConditionExpression that is true when the value at `key` equals
   * `value`.
   *
   * Evaluates lazily; if `key` is absent, the condition is false.
   */
  equals: (key: string, value: StringExpression) => ConditionExpression;
};

/**
 * HostApi factory for creating {@link NumberMapExpression} builders.
 *
 * Exposed as `hostApi.numberMap` inside module scripts.
 *
 * @see NumberMapExpression
 * @see textMap&numberMap.md
 */
export type NumberMapExpressionApi = {
  /**
   * Create an empty NumberMapExpression builder.
   */
  create: () => NumberMapExpression;
};

/**
 * An immutable, lazily-evaluated builder for a keyed map of numeric values.
 *
 * Mirrors the semantics of {@link TextMapExpression} but for
 * {@link NumberExpression} values.
 *
 * @see NumberMapExpressionApi
 * @see textMap&numberMap.md
 */
export type NumberMapExpression = {
  /**
   * Insert or replace the value at `key` with the provided NumberExpression.
   *
   * Returns a new NumberMapExpression with the updated entry.
   *
   * @param key   - Map key.
   * @param value - Numeric value expression to associate with the key.
   */
  put: (key: string, value: NumberExpression) => NumberMapExpression;

  /**
   * Remove the entry for `key`.
   *
   * Optional — not all implementations need to support removal.
   */
  remove?: (key: string) => NumberMapExpression;

  /**
   * Retrieve the NumberExpression associated with `key`.
   *
   * If the key is absent, implementations should return a zero expression
   * (fail-soft) and log the access.
   */
  get: (key: string) => NumberExpression;

  /**
   * Returns a ConditionExpression that is true when `key` is present in the
   * map.
   */
  has: (key: string) => ConditionExpression;

  /**
   * Returns a ConditionExpression that is true when the value at `key` equals
   * `value`.
   *
   * Evaluates lazily; if `key` is absent, the condition is false.
   */
  equals: (key: string, value: NumberExpression) => ConditionExpression;
};
