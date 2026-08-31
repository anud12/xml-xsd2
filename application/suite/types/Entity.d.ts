import {NumberMapExpression} from "./numberMap";
import {TextMapExpression} from "./textMap";
import {ContainerExpression} from "./Contaier";
import {StringExpression} from "./primitives/stringExpression";
import {ListExpression} from "./primitives/ListExpression";
import {NumberExpression} from "./primitives/numberExpression";
import {MaybeExpression} from "./primitives/maybeExpression";
import {EntityFilterApi} from "./EntityFilter";
import {BehaviorReference} from "./behavior";

export type EntityExpressionApi = {
  /** Create an empty entity builder */
  create: () => EntityExpression,

  /** Optional rule registration helpers (follow repository pattern) */
  asRule?: (ruleName: string, expr: EntityExpression) => EntityExpressionApi,
  getRule?: (ruleName: string) => EntityExpression,

  type: EntityExpressionType,
  filter: EntityFilterApi,
}

export type EntityCreationArguments = {
  textMap?: Record<string, StringExpression | string>
  numberMap?: Record<string, NumberExpression | number>
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
  /** Replace the entity's text_map with the supplied TextMapExpression */
  withTextMap: (textMap: TextMapExpression) => EntityExpression,
  /** Replace the entity's number_map with the supplied NumberMapExpression */
  withNumberMap: (numberMap: NumberMapExpression) => EntityExpression,
  /** Append a container membership (ContainerExpression or ContainerReference) */
  withContainer: (container: ContainerExpression) => EntityExpression,
}

export type Entity = {
  getId:() => StringExpression,
  getText: (key: StringExpression) => MaybeExpression<StringExpression>,
  getNumber: (key: StringExpression) => MaybeExpression<NumberExpression>,
  getTextKeys: () => ListExpression<string>,
  getNumberKeys: () => ListExpression<string>,
  containers: ListExpression<ContainerExpression>
}