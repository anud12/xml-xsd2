/**
 * Root barrel — re-exports every public type from the specification.
 *
 * Import individual domain modules for tree-shaking, or import from
 * this root for convenience.
 *
 * @module types
 */

// ── Primitives ────────────────────────────────────────────────────────────────
export type {
  ConditionExpressionType,
  ConditionExpression,
  ConditionExpressionApi,
} from './primitives/conditionExpression';

export type {
  NumberExpressionType,
  NumberExpression,
  NumberExpressionApi,
} from './primitives/numberExpression';

export type {
  StringExpressionType,
  StringExpression,
  StringExpressionApi,
} from './primitives/stringExpression';

export type {
  MaybeExpressionType,
  MaybeExpression,
  MaybeExpressionApi,
} from './primitives/maybeExpression';

export type {
  TemporalExpressionType,
  TemporalExpression,
  TemporalExpressionApi,
} from './primitives/temporalExpression';

export type {
  ListExpressionType,
  ListExpression,
  ListExpressionApi,
} from './primitives/listExpression';

// ── Data model ────────────────────────────────────────────────────────────────
export type {
  UniqueGlobalEntityId,
  UniqueGlobalContainerId,
} from './data-model/ids';

export type {
  TextMap,
  NumberMap,
  TextMapExpression,
  TextMapExpressionApi,
  NumberMapExpression,
  NumberMapExpressionApi,
} from './data-model/textMapNumberMap';

export type {
  ContainerList,
  Entity,
  ContainerReference,
  EntityExpressionType,
  EntityExpression,
  EntityExpressionApi,
} from './data-model/entity';

export type {
  OutOfBoundsRule,
  DimensionSize,
  Dimension,
  EntityReference,
  Container,
  ContainerExpressionType,
  DimensionExpression,
  DimensionExpressionApi,
  ContainerExpression,
  ContainerExpressionApi,
} from './data-model/container';

// ── Filters ───────────────────────────────────────────────────────────────────
export type {
  EntityFilterType,
  EntityFilter,
  EntityFilterApi,
  EntityApi,
} from './filters/entityFilter';

export type {
  ContainerFilterType,
  ContainerFilter,
  ContainerFilterApi,
  ContainerApi,
} from './filters/containerFilter';

// ── Actions ───────────────────────────────────────────────────────────────────
export type {
  ContainerPoint,
  ActionTarget,
  ActionMessage,
  ActionContext,
  PipelineNode,
  RegisterActionArgs,
} from './actions/actions';

// ── Effects ───────────────────────────────────────────────────────────────────
export type {
  EventArgType,
  EventContext,
  RegisterEventArgs,
} from './effects/effects';

// ── HostApi ───────────────────────────────────────────────────────────────────
export type { HostApi } from './host-api';

// ── User Interface ────────────────────────────────────────────────────────────
export type { UiValueExpression, UiStateApi, UiDataApi, UiValueEffect } from './user-interface/ui-state';

export type {
  SizeConstraint,
  ChildSize,
  TrackDefinition,
  GridLayout,
  PanelOptions,
  BoxOptions,
  TextOptions,
  NumberFormat,
  NumberOptions,
  Child,
  UIApi,
  UIActionApi,
  UIActionArgs,
} from './user-interface/ui';
