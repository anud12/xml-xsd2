import type { MaybeExpression } from '../primitives/maybeExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';
import type { EntityExpression } from '../data-model/entity';
import type { ContainerExpression } from '../data-model/container';

/**
 * An expression handle for a named per-client UI state value.
 *
 * A value holds either an `EntityExpression`, a `ContainerExpression`, or is
 * absent. Use the narrowing accessors to work with the current value.
 *
 * Values are set and cleared by UI actions (`set-value` / `clear-value`).
 *
 * @see ui-state.md
 */
export type UiValueExpression = {
  /**
   * Narrows to EntityExpression.
   * Resolves to None if the value holds a container or is absent.
   */
  asEntity: MaybeExpression<EntityExpression>;

  /**
   * Narrows to ContainerExpression.
   * Resolves to None if the value holds an entity or is absent.
   */
  asContainer: MaybeExpression<ContainerExpression>;

  /**
   * Evaluates to true if the value is currently populated (entity or container).
   */
  isPresent: ConditionExpression;
};

/**
 * World data access API passed as the second argument to a panel's `child`
 * function.
 *
 * Provides expression-based access to world entities and containers at load
 * time. Like `UiStateApi`, this is a proxy — calls build the expression DAG
 * rather than returning live values.
 *
 * The full surface of this API is defined in `ui-data.md` (future).
 *
 * @see panel.md
 */
export type UiDataApi = {
  // To be expanded — world entity/container query API
};

/**
 * Per-client UI state API.
 *
 * Provides access to the actor and to named per-client UI state values.
 * Received as the first argument to a panel's `render` function.
 *
 * State values are per-client — each connected client maintains its own
 * independent set of values. They are never part of shared world state.
 *
 * ## Ownership
 *
 * Use `declare` in the module that owns the value. Use `value` to reference a
 * value declared by another module.
 *
 * @see ui-state.md
 */
export type UiStateApi = {
  /**
   * The entity owned by this client (authenticated actor).
   * Always present — no narrowing needed.
   */
  actor: EntityExpression;

  /**
   * Declares a named UI state value at module load time and returns an
   * expression handle usable in UI bindings.
   *
   * Calling `declare` with the same name across modules registers a single
   * shared value. Only one module should call `declare` for a given name;
   * others should use `value`.
   */
  declare: (name: string) => UiValueExpression;

  /**
   * References a named UI state value by name.
   *
   * Intended for cross-module access where another module owns the declaration
   * via `declare`. Returns the same expression handle as `declare` for the
   * same name.
   */
  value: (name: string) => UiValueExpression;
};

/**
 * UI action effect types for mutating named UI state values.
 *
 * Used inside `hostApi.ui.action.register({ effect: ... })`.
 *
 * @see ui-state.md — Updating values
 */
export type UiValueEffect =
  | {
      /** Places the interaction target into the named state value. */
      type: 'set-value';
      value: string;
    }
  | {
      /** Clears the named state value. */
      type: 'clear-value';
      value: string;
    };
