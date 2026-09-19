import {NumberMapExpression} from "./numberMap";
import {TextMapExpression} from "./textMap";
import {ContainerExpression} from "./Contaier";
import {StringExpression} from "./primitives/stringExpression";
import {ListExpression} from "./primitives/ListExpression";
import {NumberExpression} from "./primitives/numberExpression";
import {MaybeExpression} from "./primitives/maybeExpression";
import {EntityFilterApi} from "./EntityFilter";

export type EntityExpressionApi = {
  /** Create an empty entity builder */
  create: () => EntityExpression,

  /** Optional rule registration helpers (follow repository pattern) */
  asRule?: (ruleName: string, expr: EntityExpression) => EntityExpressionApi,
  getRule?: (ruleName: string) => EntityExpression,

  type: EntityExpressionType,
  filter: EntityFilterApi,
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
  textMap?: Record<string, StringExpression>
  numberMap?: Record<string, NumberExpression>
  /** The entity's declared area (see {@link Area}). */
  area?: Area
}

export type EntityExpressionType = {
  // marker for dynamic HostApi typing
}

export type EntityExpression = {
  /** Replace the entity's text_map with the supplied TextMapExpression */
  withTextMap: (textMap: TextMapExpression) => EntityExpression,
  /** Replace the entity's number_map with the supplied NumberMapExpression */
  withNumberMap: (numberMap: NumberMapExpression) => EntityExpression,
  /** Append a container membership (ContainerExpression or ContainerReference) */
  withContainer: (container: ContainerExpression) => EntityExpression,
  /** The containers this entity belongs to. */
  containers: ListExpression<ContainerExpression>;
  /** The entity's id. */
  id: string;
  /** The entity's number_map accessor (keyed lookups returning MaybeExpressions). */
  number_map: { get: (key: string) => MaybeExpression<NumberExpression> };
  /** The entity's text_map accessor (keyed lookups returning MaybeExpressions). */
  text_map: { get: (key: string) => MaybeExpression<StringExpression> };
  /**
   * The entities inside this entity's area. Only members of a container this
   * entity belongs to are considered (areas have no global space); the entity
   * itself is excluded; the result is deduped by id and ordered by id
   * ascending. An entity with no area yields an empty list (not an error).
   * Each element is a minimal entity reference exposing `getId`.
   */
  getEntitiesInsideArea: () => ListExpression<EntityInsideAreaReference>
}

/** A minimal entity reference returned by {@link EntityExpression.getEntitiesInsideArea}. */
export type EntityInsideAreaReference = {
  /** The contained entity's id. */
  getId: () => string
}

export type Entity = {
  /** The entity's id (plain string, stable across ticks). */
  id: string;
  getId:() => StringExpression,
  getText: (key: StringExpression) => MaybeExpression<StringExpression>,
  getNumber: (key: StringExpression) => MaybeExpression<NumberExpression>,
  getTextKeys: () => ListExpression<string>,
  getNumberKeys: () => ListExpression<string>,
  containers: ListExpression<ContainerExpression>,
  /**
   * The entities inside this entity's area (see
   * {@link EntityExpression.getEntitiesInsideArea}). An entity with no area
   * yields an empty list.
   */
  getEntitiesInsideArea: () => ListExpression<EntityInsideAreaReference>
}