import {StringReadOnlyExpression} from "../primitives/stringExpression";
import {ListReadOnlyExpression} from "../primitives/ListExpression";
import {NumberReadOnlyExpression} from "../primitives/numberExpression";
import {MaybeReadOnlyExpression} from "../primitives/maybeExpression";
import {Expression} from "../Expression";
import {EntityFilter} from "../EntityFilter";
import {ConditionReadOnlyExpression} from "../primitives/conditionExpression";

export type EntityExpressionApi = {
  /** Create an empty entity builder */
  create: () => EntityReadOnlyExpression,

  /** Optional rule registration helpers (follow repository pattern) */
  asRule?: (ruleName: string, expr: EntityReadOnlyExpression) => EntityExpressionApi,
  getRule?: (ruleName: string) => EntityReadOnlyExpression,

  type: EntityExpressionType,
}

import {SectorDeclaration} from "./Sector";
import {BehaviorReadOnlyExpression} from "./behavior";

export type EntityDeclaration = {
  id: StringReadOnlyExpression;
  /** Intrinsic numbers including x/y, spanX/spanY, or group sizeX/sizeY. */
  numberMap?: Record<string, number | NumberReadOnlyExpression>;
  /** Intrinsic strings including optional position key names, alignment and bounds rule metadata. */
  stringMap?: Record<string, string | StringReadOnlyExpression>;
  /** Named, implicitly closed polygons in entity-local units (y down). */
  polygons?: Record<string, [number, number][]>;
  /** The group role: ordered, named lists of entity references, with no placement. */
  entityListMap?: Record<string, readonly EntityReadOnlyExpression[]>;
  behavior?: BehaviorReadOnlyExpression;
  sector?: SectorDeclaration;
}

export type EntityExpressionType = {
  // marker for dynamic HostApi typing
}

/** A named relationship list with entity-specific filtering. */
export type EntityListReadOnlyExpression = ListReadOnlyExpression<EntityReadOnlyExpression> & {
  /** Return matching members in declaration order; chainable and empty on no match. */
  findBy: (filter: EntityFilter) => EntityListReadOnlyExpression;
  /** True if at least one member matches; false for an absent or empty list. */
  hasEntity: (filter: EntityFilter) => ConditionReadOnlyExpression<Expression>;
};

export type EntityReadOnlyExpression = Expression & {
  /** Read a named membership list, in declaration order. */
  members: (name: string) => EntityListReadOnlyExpression;
  /** The entity's id as a StringReadOnlyExpression (isContainingExactly, isContaining, ...). */
  id: StringReadOnlyExpression;
  /** The entity's number_map accessor (keyed lookups returning MaybeReadOnlyExpressions). */
  number_map: { get: (key: string | StringReadOnlyExpression) => MaybeReadOnlyExpression<NumberReadOnlyExpression> };
  /** The entity's string_map accessor (keyed lookups returning MaybeReadOnlyExpressions). */
  string_map: { get: (key: string | StringReadOnlyExpression) => MaybeReadOnlyExpression<StringReadOnlyExpression> };
  /**
   * The entities inside one *named* area of this entity. The area name is
   * required: it is resolved first, then the containment/overlap test runs.
   * Only peers in a shared named list are considered (areas
   * have no global space); the entity itself is excluded; the result is
   * deduped by id and ordered by id ascending. An entity with no such area
   * yields an empty list (not an error).
   */
  getEntitiesInsideArea: (areaName: string | StringReadOnlyExpression) => ListReadOnlyExpression<EntityReadOnlyExpression>
}

/**
 * A mutable reference to an entity, returned by `EventApplyContext.getMutableEntityBy`.
 * Extends the read-only expression with in-place map mutation. Available only
 * during the `apply` phase.
 */
export type EntityMutableExpression = EntityReadOnlyExpression & {
  /** Add to a declared list without duplicating the member; return the group as a read-only expression. */
  addMember: (list: string, member: EntityReadOnlyExpression) => EntityReadOnlyExpression;
  removeMember: (list: string, entity: EntityReadOnlyExpression) => EntityMutableExpression;
  /** The entity's number_map accessor with in-place mutation. */
  number_map: EntityReadOnlyExpression["number_map"] & {
    put: (key: string | StringReadOnlyExpression, transform: (current: MaybeReadOnlyExpression<NumberReadOnlyExpression>) => MaybeReadOnlyExpression<NumberReadOnlyExpression>) => EntityMutableExpression;
    remove: (key: string | StringReadOnlyExpression) => EntityMutableExpression;
  };
  /** The entity's string_map accessor with in-place mutation. */
  string_map: EntityReadOnlyExpression["string_map"] & {
    put: (key: string | StringReadOnlyExpression, transform: (current: MaybeReadOnlyExpression<StringReadOnlyExpression>) => MaybeReadOnlyExpression<StringReadOnlyExpression>) => EntityMutableExpression;
    remove: (key: string | StringReadOnlyExpression) => EntityMutableExpression;
  };
}
