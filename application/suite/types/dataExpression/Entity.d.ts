import {ContainerReadOnlyExpression} from "./Contaier";
import {StringReadOnlyExpression} from "../primitives/stringExpression";
import {ListReadOnlyExpression} from "../primitives/ListExpression";
import {NumberReadOnlyExpression} from "../primitives/numberExpression";
import {MaybeReadOnlyExpression} from "../primitives/maybeExpression";
import {Expression} from "../Expression";

export type EntityExpressionApi = {
  /** Create an empty entity builder */
  create: () => EntityReadOnlyExpression,

  /** Optional rule registration helpers (follow repository pattern) */
  asRule?: (ruleName: string, expr: EntityReadOnlyExpression) => EntityExpressionApi,
  getRule?: (ruleName: string) => EntityReadOnlyExpression,

  type: EntityExpressionType,
}

/** A point in entity-local area units. y increases downward. */
export type AreaPoint = [number, number]

/**
 * The polygonal area an entity occupies in the simulation, declared in
 * entity-local units (y down), implicitly closed. Like `numberMap`, it is
 * intrinsic to the entity: it moves wherever the entity goes. An entity with
 * no area has no presence — it cannot contain or be contained by another
 * area, and is itself treated as the point at its `(getX, getY)`.
 */
export type Area = {
  /** The polygon vertices in entity-local units (>= 3 distinct points). */
  polygon: Array<AreaPoint>
}

export type EntityCreationArguments = {
  textMap?: Record<string, StringReadOnlyExpression>
  numberMap?: Record<string, NumberReadOnlyExpression>
  /**
   * The entity's named areas (entityMap): area name -> polygon. Areas are
   * always declared here (there is no unnamed area). Each named area can be
   * queried independently via
   * {@link EntityReadOnlyExpression.getEntitiesInsideArea}.
   */
  areaMap?: Record<string, Area>
}

export type EntityExpressionType = {
  // marker for dynamic HostApi typing
}

export type EntityReadOnlyExpression = Expression & {
  /** The containers this entity belongs to. */
  containers: ListReadOnlyExpression<ContainerReadOnlyExpression>;
  /** The entity's id as a StringReadOnlyExpression (isContainingExactly, isContaining, ...). */
  id: StringReadOnlyExpression;
  /** The entity's number_map accessor (keyed lookups returning MaybeReadOnlyExpressions). */
  number_map: { get: (key: string | StringReadOnlyExpression) => MaybeReadOnlyExpression<NumberReadOnlyExpression> };
  /** The entity's text_map accessor (keyed lookups returning MaybeReadOnlyExpressions). */
  text_map: { get: (key: string | StringReadOnlyExpression) => MaybeReadOnlyExpression<StringReadOnlyExpression> };
  /**
   * The entities inside one *named* area of this entity. The area name is
   * required: it is resolved first, then the containment/overlap test runs.
   * Only members of a container this entity belongs to are considered (areas
   * have no global space); the entity itself is excluded; the result is
   * deduped by id and ordered by id ascending. An entity with no such area
   * yields an empty list (not an error). Each element is a minimal entity
   * reference exposing `getId`.
   */
  getEntitiesInsideArea: (areaName: string | StringReadOnlyExpression) => ListReadOnlyExpression<EntityInsideAreaReference>
}

/** A minimal entity reference returned by {@link EntityReadOnlyExpression.getEntitiesInsideArea}. */
export type EntityInsideAreaReference = {
  /** The contained entity's id. */
  getId: () => string
}

export type Entity = {
  /** The entity's id (plain string, stable across ticks). */
  id: string;
  getId:() => StringReadOnlyExpression,
  getText: (key: StringReadOnlyExpression) => MaybeReadOnlyExpression<StringReadOnlyExpression>,
  getNumber: (key: StringReadOnlyExpression) => MaybeReadOnlyExpression<NumberReadOnlyExpression>,
  getTextKeys: () => ListReadOnlyExpression<string>,
  getNumberKeys: () => ListReadOnlyExpression<string>,
  containers: ListReadOnlyExpression<ContainerReadOnlyExpression>,
  /**
   * The entities inside one *named* area of this entity (see
   * {@link EntityReadOnlyExpression.getEntitiesInsideArea}). The area name is
   * required; an entity with no such area yields an empty list.
   */
  getEntitiesInsideArea: (areaName: string) => ListReadOnlyExpression<EntityInsideAreaReference>
}