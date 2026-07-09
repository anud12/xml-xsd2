import {EntityExpression} from "./Entity";
import {NumberExpression} from "./primitives/numberExpression";
import {StringExpression} from "./primitives/stringExpression";
import {TextMapExpression} from "./textMap";
import {NumberMapExpression} from "./numberMap";

export type ContainerCreationArguments = {
  /** Keyed string metadata for the container */
  textMap?: Record<string, StringExpression>
  /** Keyed numeric metadata for the container */
  numberMap?: Record<string, NumberExpression>
  /** Maps a member entity to its x-coordinate as a NumberExpression */
  getX?: (entity: EntityExpression) => NumberExpression
  /** Maps a member entity to its y-coordinate as a NumberExpression */
  getY?: (entity: EntityExpression) => NumberExpression
  /** Maps a member entity to the number of cells it occupies along the x-axis. Defaults to 1 when not set */
  getSpanX?: (entity: EntityExpression) => NumberExpression
  /** Maps a member entity to the number of cells it occupies along the y-axis. Defaults to 1 when not set */
  getSpanY?: (entity: EntityExpression) => NumberExpression
  /** Optional size bounds along the x-axis */
  sizeX?: { value: NumberExpression, outOfBounds: OutOfBoundsRule }
  /** Optional size bounds along the y-axis */
  sizeY?: { value: NumberExpression, outOfBounds: OutOfBoundsRule }
}

export type ContainerExpressionApi = {
  /** Create an empty container builder */
  create: () => ContainerExpression,
  /** Register and retrieve named container templates */
  asRule?: (ruleName: string, expr: ContainerExpression) => ContainerExpressionApi,
  getRule?: (ruleName: string) => ContainerExpression,
  type: ContainerExpressionType,
}

export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
}

export type OutOfBoundsRule = "unbound" | "clamp" | "wrap"

export type ContainerExpression = {
  /** Append an inline member entity built using EntityExpression */
  withEntity: (entity: EntityExpression) => ContainerExpression,
  /** Replace the entity's text_map with the supplied TextMapExpression */
  withTextMap: (textMap: TextMapExpression) => ContainerExpression,
  /** Replace the entity's number_map with the supplied NumberMapExpression */
  withNumberMap: (numberMap: NumberMapExpression) => ContainerExpression,
  /** Declare the x-coordinate function */
  withGetX: (getX: (entity: EntityExpression) => NumberExpression) => ContainerExpression,
  /** Declare the y-coordinate function */
  withGetY: (getY: (entity: EntityExpression) => NumberExpression) => ContainerExpression,
  /** Declare the x-span function */
  withGetSpanX: (getSpanX: (entity: EntityExpression) => NumberExpression) => ContainerExpression,
  /** Declare the y-span function */
  withGetSpanY: (getSpanY: (entity: EntityExpression) => NumberExpression) => ContainerExpression,
  /** Declare the x-axis size bounds */
  withSizeX: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => ContainerExpression,
  /** Declare the y-axis size bounds */
  withSizeY: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => ContainerExpression,
}