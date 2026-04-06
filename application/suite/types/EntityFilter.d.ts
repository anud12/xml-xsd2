import { StringExpression } from "./primitives/stringExpression";
import { NumberExpression } from "./primitives/numberExpression";
import { ConditionExpression } from "./primitives/conditionExpression";
import { ContainerExpression } from "./Contaier";

/** API surface for building composable entity filters used by queries/rules/effects. */
export type EntityFilterApi = {
  /** Create an empty filter builder */
  create: () => EntityFilter;
  asRule?: (ruleName: string, filter: EntityFilter) => EntityFilterApi;
  getRule?: (ruleName: string) => EntityFilter;
  type: EntityFilterType;
}

export type EntityFilterType = {
  // marker for HostApi typing
}

/** A composable, immutable filter expression that evaluates to a list of entities. */
export type EntityFilter = {
  /** Narrow by evaluating a predicate over the entity id. */
  byId: (fn: (id: StringExpression) => ConditionExpression) => EntityFilter;

  /** Match entities that have the given classification string (convenience). */
  byClassification?: (classification: StringExpression) => EntityFilter;

  /** Match where an entity has a text_map entry for `key` and at least one value satisfies `fn`. */
  hasTextValue: (key: StringExpression, fn: (value: StringExpression) => ConditionExpression) => EntityFilter;

  /** Match where an entity has a number_map entry for `key` and at least one value satisfies `fn`. */
  hasNumberValue: (key: StringExpression, fn: (value: NumberExpression) => ConditionExpression) => EntityFilter;

  /** Match entities that are members of containers matched by the given container expression. */
  hasContainer: (containerFilter: ContainerExpression) => EntityFilter;

  /** Logical negation (set complement relative to the chosen source). */
  not: (entityFilter: EntityFilter) => EntityFilter;

  /** Intersection/union helpers. */
  and: (...others: EntityFilter[]) => EntityFilter;
  or: (...others: EntityFilter[]) => EntityFilter;
}

/** Helpers available while evaluating predicates in the context of a single entity. */
export type EntityEvaluationContext = {
  id: StringExpression;
  text: (key: StringExpression) => StringExpression | null | undefined;
  number: (key: StringExpression) => NumberExpression | null | undefined;
  classifications: StringExpression[];
  containers: ContainerExpression[];
}
