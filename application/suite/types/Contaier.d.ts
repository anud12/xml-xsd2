import {EntityExpression, EntityProxy} from "./Entity";
import {NumberExpression} from "./primitives/numberExpression";
import {StringExpression} from "./primitives/stringExpression";
import {TextMap, MutableTextMap} from "./textMap";
import {NumberMap, MutableNumberMap} from "./numberMap";

export type ContainerCreationArguments = {
  /** Keyed string metadata for the container */
  textMap?: TextMap
  /** Keyed numeric metadata for the container */
  numberMap?: NumberMap
  /** Maps a member entity to its x-coordinate as a NumberExpression */
  getX?: (entity: EntityProxy) => NumberExpression
  /** Maps a member entity to its y-coordinate as a NumberExpression */
  getY?: (entity: EntityProxy) => NumberExpression
  /** Maps a member entity to the number of cells it occupies along the x-axis. Defaults to 1 when not set */
  getSpanX?: (entity: EntityProxy) => NumberExpression
  /** Maps a member entity to the number of cells it occupies along the y-axis. Defaults to 1 when not set */
  getSpanY?: (entity: EntityProxy) => NumberExpression
  /** Optional size bounds along the x-axis */
  sizeX?: { value: NumberExpression, outOfBounds: OutOfBoundsRule }
  /** Optional size bounds along the y-axis */
  sizeY?: { value: NumberExpression, outOfBounds: OutOfBoundsRule }
  
  /** Member entities of the container. Optional when the container holds no members. */
  entities?: StringExpression[]
}

export type ContainerExpressionApi = {
  /** Create an empty container builder */
  create: () => MutableContainerExpression,
  type: ContainerExpressionType,
}

export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
}

export type OutOfBoundsRule = "unbound" | "clamp" | "wrap"

export type ContainerExpression = {
  /** Get the container's text_map as a read-only map */
  textMap: () => TextMap,
  /** Get the container's number_map as a read-only map */
  numberMap: () => NumberMap,
  /** List the member entities of the container */
  getEntities: () => StringExpression[],
}

export type MutableContainerExpression = ContainerExpression & {
  /** Append an inline member entity built using EntityExpression */
  withEntity: (entity: EntityExpression) => MutableContainerExpression,
  /** Get the container's text_map as a mutable map whose writes persist to the container */
  textMap: () => MutableTextMap,
  /** Get the container's number_map as a mutable map whose writes persist to the container */
  numberMap: () => MutableNumberMap,
  /** Declare the x-coordinate function */
  withGetX: (getX: (entity: EntityExpression) => NumberExpression) => MutableContainerExpression,
  /** Declare the y-coordinate function */
  withGetY: (getY: (entity: EntityExpression) => NumberExpression) => MutableContainerExpression,
  /** Declare the x-span function */
  withGetSpanX: (getSpanX: (entity: EntityExpression) => NumberExpression) => MutableContainerExpression,
  /** Declare the y-span function */
  withGetSpanY: (getSpanY: (entity: EntityExpression) => NumberExpression) => MutableContainerExpression,
  /** Declare the x-axis size bounds */
  withSizeX: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => MutableContainerExpression,
  /** Declare the y-axis size bounds */
  withSizeY: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => MutableContainerExpression,
}
