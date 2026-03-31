import type { ConditionExpressionApi } from './primitives/conditionExpression';
import type { NumberExpressionApi } from './primitives/numberExpression';
import type { StringExpressionApi } from './primitives/stringExpression';
import type { MaybeExpressionApi } from './primitives/maybeExpression';
import type { TemporalExpressionApi } from './primitives/temporalExpression';
import type { ListExpressionApi } from './primitives/listExpression';
import type { EntityExpressionApi } from './data-model/entity';
import type { ContainerExpressionApi } from './data-model/container';
import type { TextMapExpressionApi, NumberMapExpressionApi } from './data-model/textMapNumberMap';
import type { EntityApi } from './filters/entityFilter';
import type { ContainerApi } from './filters/containerFilter';
import type { RegisterActionArgs } from './actions/actions';
import type { RegisterEventArgs } from './effects/effects';
import type { UIApi } from './user-interface';

/**
 * The complete HostApi surface exposed to module scripts.
 *
 * Each module receives a `HostApi` instance as the argument to its default
 * export function. The API is available **only during module initialization**
 * (sandbox phase). After initialization the sandbox is torn down and the
 * runtime operates on the extracted AST.
 *
 * ## Module entry pattern
 * ```ts
 * export default (hostApi: HostApi): void => {
 *   // declare rules, events, actions here
 * };
 * ```
 *
 * ## HostApi member overview
 *
 * | Member        | Type                                           | Description                              |
 * |---------------|------------------------------------------------|------------------------------------------|
 * | `condition`   | `ConditionExpressionApi`                       | Boolean expression factory & rule registry |
 * | `number`      | `NumberExpressionApi`                          | 64-bit integer expression factory        |
 * | `string`      | `StringExpressionApi`                          | String expression factory                |
 * | `maybe`       | `MaybeExpressionApi<any>`                      | Optional value expression factory        |
 * | `temporal`    | `TemporalExpressionApi`                        | In-game duration factory & clock config  |
 * | `list`        | `ListExpressionApi`                            | Ordered sequence expression factory      |
 * | `entity`      | `EntityExpressionApi & EntityApi`              | Entity builder + filter API              |
 * | `container`   | `ContainerExpressionApi & ContainerApi`        | Container builder + filter API           |
 * | `textMap`     | `TextMapExpressionApi`                         | Text map builder factory                 |
 * | `numberMap`   | `NumberMapExpressionApi`                       | Number map builder factory               |
 * | `registerAction` | `(args: RegisterActionArgs) => void`        | Register a client-facing action          |
 * | `registerEvent`  | `<I, O>(args: RegisterEventArgs<I,O>) => void` | Register a named effect/event            |
 * | `loadModule`  | `<T>(name: string) => T`                       | Load a named submodule                   |
 * | `ui`          | `UIApi`                                        | Panel registration and per-client UI state |
 *
 * @see modules.md
 * @see runtime.md
 */
export type HostApi = {
  /**
   * Boolean / condition expression factory and rule registry.
   *
   * Build lazy boolean expression trees for guards, filter predicates, and
   * condition-based branching.
   *
   * @see conditionExpression.md
   */
  condition: ConditionExpressionApi;

  /**
   * 64-bit signed integer expression factory and rule registry.
   *
   * Build lazy arithmetic expressions representing game numbers. All values
   * are host `long` (two's-complement, wrap-on-overflow).
   *
   * @see numberExpression.md
   */
  number: NumberExpressionApi;

  /**
   * String expression factory and rule registry.
   *
   * Build composable, deterministic string expressions with literals,
   * concatenation, refs, and deterministic `oneOf` choices.
   *
   * @see stringExpression.md
   */
  string: StringExpressionApi;

  /**
   * Optional value expression factory and rule registry.
   *
   * Model computations that may or may not produce a value using
   * `Some`/`None` semantics. Prefer explicit unwrapping via `orElse`.
   *
   * @see maybeExpression.md
   */
  maybe: MaybeExpressionApi<any>;

  /**
   * In-game time duration factory, clock configuration, and rule registry.
   *
   * Express cooldowns and reoccurrence delays in module-defined named units
   * (e.g. `"round"`, `"day"`). Configure the GTU clock with `tickAdvancesBy`.
   *
   * @see temporalExpression.md
   */
  temporal: TemporalExpressionApi;

  /**
   * Ordered sequence expression factory and rule registry.
   *
   * Build lazy list expressions supporting element access, concatenation,
   * transformation, and deterministic random element selection.
   *
   * @see listExpression.md
   */
  list: ListExpressionApi;

  /**
   * Entity expression builder, named template registry, and entity filter API.
   *
   * Combined surface of {@link EntityExpressionApi} (create/asRule/getRule/type)
   * and {@link EntityApi} (filter).
   *
   * @see entities.md
   * @see entityFilter.md
   */
  entity: EntityExpressionApi & EntityApi;

  /**
   * Container expression builder, named template registry, and container filter
   * API.
   *
   * Combined surface of {@link ContainerExpressionApi}
   * (create/asRule/getRule/type/dimension) and {@link ContainerApi} (filter).
   *
   * @see containers.md
   * @see containerFilter.md
   */
  container: ContainerExpressionApi & ContainerApi;

  /**
   * TextMap expression builder factory.
   *
   * Create and compose keyed string maps for entity and container attributes.
   *
   * @see textMap&numberMap.md
   */
  textMap: TextMapExpressionApi;

  /**
   * NumberMap expression builder factory.
   *
   * Create and compose keyed numeric maps for entity and container attributes.
   *
   * @see textMap&numberMap.md
   */
  numberMap: NumberMapExpressionApi;

  /**
   * Register a named Action — the sole external entrypoint into the runtime.
   *
   * Clients trigger registered actions over WebSocket. The runtime validates
   * the declaration at module load time (including pipeline DAG cycle
   * detection).
   *
   * @param args - Action declaration including name, targetType, guard,
   *               cooldown, and effect pipeline.
   * @see actions.md
   */
  registerAction: (args: RegisterActionArgs) => void;

  /**
   * Register a named Effect / Event with `prepare` and `apply` hooks.
   *
   * Effects are invoked by action pipelines (`PipelineNode.effect`) and by
   * `context.emitEvent`. The runtime enforces the prepare → apply → commit
   * ordering and recursion guard.
   *
   * @param args - Effect declaration including name, input/output schema,
   *               prepare, apply, and optional reoccurrence hooks.
   * @see effects.md
   */
  registerEvent: <Input, Output>(args: RegisterEventArgs<Input, Output>) => void;

  /**
   * Load a named submodule and return its exported type.
   *
   * Allows composing behavior across modules within the same sandbox
   * initialization phase.
   *
   * @param name - Name of the submodule to load.
   * @see modules.md
   */
  loadModule: <SubmoduleType>(name: string) => SubmoduleType;

  /**
   * User interface API.
   *
   * Register panels, declare per-client UI state values, and bind UI actions.
   * Available during module initialization only.
   *
   * @see overview.md
   * @see panel.md
   * @see ui-state.md
   */
  ui: UIApi;
};
