import {NumberMap, MutableNumberMap} from "./numberMap";
import {TextMap, MutableTextMap} from "./textMap";
import {ContainerExpression} from "./Contaier";
import {StringExpression} from "./primitives/stringExpression";
import {ListExpression} from "./primitives/ListExpression";
import {NumberExpression, MutableNumberExpression} from "./primitives/numberExpression";
import {MaybeExpression, MutableMaybeExpression} from "./primitives/maybeExpression";
import {MutableStringExpression} from "./primitives/stringExpression";
import {BehaviorReference} from "./behavior";

export type EntityExpressionApi = {
  /** Create an empty entity builder */
  create: () => MutableEntityExpression,

  type: EntityExpressionType,
}

export type EntityCreationArguments = {
  textMap?: TextMap
  numberMap?: NumberMap
  /** Optional behavior reference attached to the entity. */
  behavior?: BehaviorReference | StringExpression
}

/** Runtime entity view passed to container position/span callbacks. */
export type EntityProxy = {
  id: string,
  /** Number-map accessor; `orElse` falls back to the supplied default when the key is absent. */
  number_map: { get: (key: string) => { orElse: <T>(defaultValue: T) => T } },
  /** Text-map accessor. */
  text_map: { get: (key: string) => string },
  getNumber: (key: string) => number,
  getText: (key: string) => string,
}

export type EntityExpressionType = {
  // marker for dynamic HostApi typing
}

export type EntityExpression = {
  /** Read the entity's text_map for a key */
  getText: (key: StringExpression) => MaybeExpression<StringExpression>,
  /** Read the entity's number_map for a key */
  getNumber: (key: StringExpression) => MaybeExpression<NumberExpression>,
  /** Get the entity's text_map as a read-only map */
  textMap: () => TextMap,
  /** Get the entity's number_map as a read-only map */
  numberMap: () => NumberMap,
  /** List the entity's text_map keys */
  getTextKeys: () => ListExpression<string>,
  /** List the entity's number_map keys */
  getNumberKeys: () => ListExpression<string>,
  /** Containers the entity belongs to */
  containers: ListExpression<ContainerExpression>,
}

export type MutableEntityExpression = Omit<EntityExpression, "textMap" | "numberMap"> & {
  /** Set the entity's text_map value for a key */
  setText: (key: StringExpression, value: StringExpression) => MutableEntityExpression,
  /** Set the entity's number_map value for a key */
  setNumber: (key: StringExpression, value: NumberExpression) => MutableEntityExpression,
  /** Get the entity's text_map as a mutable map whose values write back to the entity */
  textMap: () => MutableTextMap,
  /** Get the entity's number_map as a mutable map whose values write back to the entity */
  numberMap: () => MutableNumberMap,
  /** Append a container membership (ContainerExpression or ContainerReference) */
  withContainer: (container: ContainerExpression) => MutableEntityExpression,
}

/**
 * Entity view yielded by repository queries. Map accessors return mutable
 * expressions; whether mutation is allowed in the current phase is enforced by
 * the phase context (PrepareContext/SchedulingContext yield read-only views at
 * runtime, ApplyContext yields mutable views).
 */
export type Entity = {
  getId: () => StringExpression,
  getText: (key: StringExpression) => MutableMaybeExpression<MutableStringExpression>,
  getNumber: (key: StringExpression) => MutableMaybeExpression<MutableNumberExpression>,
  /** Get the entity's text_map; values write back to the entity in the apply phase */
  textMap: () => MutableTextMap,
  /** Get the entity's number_map; values write back to the entity in the apply phase */
  numberMap: () => MutableNumberMap,
  getTextKeys: () => ListExpression<string>,
  getNumberKeys: () => ListExpression<string>,
  containers: ListExpression<ContainerExpression>
}
