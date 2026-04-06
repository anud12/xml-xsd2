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
  getText: (key: StringExpression) => MaybeExpression<StringExpression>,
  getNumber: (key: StringExpression) => MaybeExpression<NumberExpression>,
  getTextKeys: () => ListExpression<string>,
  getNumberKeys: () => ListExpression<string>,
  containers: ListExpression<ContainerExpression>
}