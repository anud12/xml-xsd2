import {EntityExpression} from "./Entity";
import {NumberExpression} from "./primitives/numberExpression";
import {TextMapExpression} from "./textMap";
import {NumberMapExpression} from "./numberMap";

export type ContainerExpressionApi = {
  /** Create an empty container builder */
  create: () => ContainerExpression,
  /** Register and retrieve named container templates */
  asRule?: (ruleName: string, expr: ContainerExpression) => ContainerExpressionApi,
  getRule?: (ruleName: string) => ContainerExpression,
  type: ContainerExpressionType,
  /** Dimension expression builder factory for labeling container axes */
  dimension?: DimensionExpressionApi,
}

export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
}

export type DimensionExpressionApi = {
  create: () => DimensionExpression,
  asRule?: (ruleName: string, expr: DimensionExpression) => DimensionExpressionApi,
  getRule?: (ruleName: string) => DimensionExpression,
}

export type OutOfBoundsRule = "unbound" | "clamp" | "wrap"

export type DimensionExpression = {
  withName: (name: string) => DimensionExpression,
}

export type ContainerExpression = {
  /** Append an inline member entity built using EntityExpression */
  withEntity: (entity: EntityExpression) => ContainerExpression,
  /** Replace the entity's text_map with the supplied TextMapExpression */
  withTextMap: (textMap: TextMapExpression) => EntityExpression,
  /** Replace the entity's number_map with the supplied NumberMapExpression */
  withNumberMap: (numberMap: NumberMapExpression) => EntityExpression,
  /** Declare the x-coordinate function */
  withGetX: (getX: (entity: EntityExpression) => NumberExpression) => ContainerExpression,
  /** Declare the y-coordinate function */
  withGetY: (getY: (entity: EntityExpression) => NumberExpression) => ContainerExpression,
  /** Declare the span function */
  withGetSpan: (getSpan: (entity: EntityExpression) => NumberExpression) => ContainerExpression,
  /** Declare the size bounds */
  withSize: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => ContainerExpression,
}