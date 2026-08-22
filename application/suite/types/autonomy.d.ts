import { NumberExpression } from "./primitives/numberExpression";
import { StringExpression } from "./primitives/stringExpression";
import { ConditionExpression } from "./primitives/conditionExpression";
import { Entity } from "./Entity";

/**
 * A single atomic step within an autonomy rule's execution script.
 *
 * The script is a pure declaration of what happens: the executor runs
 * the steps in order, with `wait` steps suspending across ticks.
 *
 * Algorithm: linear step machine. `action` steps dispatch to a
 * registered action (which may invoke effects internally and hold its
 * own executor state, e.g. an in-flight move); `wait` steps suspend
 * the script for the given duration in game time units.
 */
export type AutonomyStep =
  | {
      /** Execute a named action (which may invoke effects internally). */
      action: StringExpression;
      /** Arbitrary payload passed to the action at emit time. */
      payload?: Record<string, any>;
    }
  | {
      /** Suspend the script for a duration (in game time units). */
      wait: NumberExpression;
    };

/**
 * The restricted context handed to a rule's `do` lambda.
 *
 * It can only declare steps (actions and waits) — all world access
 * (queries, state, rng) is deliberately out of reach.
 */
export type DoContext = {
  /** Declare a step that executes the named action. */
  action: (name: StringExpression, payload?: Record<string, any>) => AutonomyStep;
  /** Declare a step that waits for the given duration. */
  wait: (duration: NumberExpression) => AutonomyStep;
};

/**
 * A rule inside a `utility` select: scored against the entity, best score wins.
 */
export type UtilityRule = {
  /** Human-readable name for the debug trace; required. */
  label: string;
  /**
   * Score, evaluated against the entity only (read-only).
   * Re-evaluated only when an entity key the score reads is written.
   */
  score: (entity: Entity) => NumberExpression;
  /** Builds the execution script for this rule. */
  do: (ctx: DoContext) => AutonomyStep[];
};

/**
 * A branch of a `priority` select: the first branch whose condition
 * is true is selected; its `utility` array then resolves the action.
 * Lower branches are not evaluated (short-circuit).
 */
export type PriorityRule = {
  /** Human-readable name for the debug trace; required. */
  label: string;
  /** Enable gate, evaluated against the entity only (read-only). */
  condition: (entity: Entity) => ConditionExpression;
  /** The fuzzy decision made within this branch. */
  utility: UtilityRule[];
};

/**
 * A node in the autonomy behavior graph.
 *
 * - `priority`: ordered branches; first true `condition` wins, then its
 *   `utility` resolves. Rule-based conflict resolution (short-circuit).
 * - `utility`: all rules scored, best score wins (argmax; ties broken
 *   by array order). Utility AI.
 */
export type AutonomyNode =
  | { priority: PriorityRule[] }
  | { utility: UtilityRule[] };

/**
 * An opaque handle to a registered autonomy.
 * Attach to entities via `setAutonomy`.
 */
export type Autonomy = {
  readonly name: string;
};

/**
 * API for creating autonomy graphs and attaching them to entities.
 *
 * Exposed as `hostApi.runtime.autonomy` / `hostApi.runtime.setAutonomy`
 * inside module scripts.
 */
export type AutonomyApi = {
  /**
   * Create and register an autonomy from a declarative definition.
   *
   * Autonomy — per-entity decision making.
   *
   * Paradigm: a **blackboard system** (entity = shared blackboard:
   * numberMap/textMap) whose control shell is a **dataflow rule graph**:
   *
   * - Rules are gated by conditions / scores and connected by explicit
   *   data dependencies on entity keys. Execution order follows data
   *   dependencies (Kahn-process-network style), which makes the result
   *   deterministic and independent of evaluation order.
   * - **`priority`** is classic **rule-based conflict resolution**:
   *   first matching rule wins, evaluated top-down with short-circuit
   *   (lower branches are never evaluated). For "laws" — hard gates the
   *   author knows outrank everything else.
   * - **`utility`** is **utility AI**: every rule's `score` is evaluated
   *   and the maximum wins (argmax; ties broken by array order). Scores
   *   are re-evaluated only when an entity key they read was written
   *   (dependency-driven invalidation), so idle agents cost ~zero.
   *   For "taste" — continuous trade-offs and per-instance personality.
   * - The selected rule's `do` builds a **step script** (actions and
   *   waits) — a pure declaration. A small built-in **executor** (low
   *   level state machine) performs the script across ticks; the graph
   *   itself is stateless per tick.
   *
   * The definition is pure data plus the rule lambdas; JS runs at load
   * time only. The runtime never re-interprets the graph.
   *
   * Validation (throws on failure):
   * - `name` is present and globally unique
   * - the node is well-formed (priority/utility shapes)
   * - `label` is present on every rule
   *
   * @param definition - Declarative autonomy definition: a name plus
   *   the behavior graph root (`priority` or `utility`).
   * @returns Opaque autonomy handle for `setAutonomy`.
   */
  autonomy: (definition: { name: StringExpression } & AutonomyNode) => Autonomy;

  /**
   * Attach an autonomy to an entity by ID.
   * Replaces any autonomy previously attached to that entity.
   *
   * @param entityId - The entity to attach the autonomy to.
   * @param autonomy - Autonomy handle from `autonomy`.
   */
  setAutonomy: (entityId: StringExpression, autonomy: Autonomy) => void;
};
