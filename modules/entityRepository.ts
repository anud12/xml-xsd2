/**
 * Entity Repository types — declarative query API for runtime
 *
 * This file defines lightweight TypeScript types that modules can import to
 * declare and execute entity queries. The runtime decides the concrete
 * implementation (in-memory, persistent, hybrid). Keep these types minimal
 * and implementation-agnostic.
 */

export type EntityId = string;

export interface Entry {
  id: EntityId;
  type?: string;
  tags?: string[];
  // Arbitrary properties; implementations may use typed schemas.
  properties?: { [k: string]: unknown };
  // Allow extra shape for extension
  [k: string]: unknown;
}

export enum Selection {
  ALL = 'ALL',
  FIRST = 'FIRST',
  RANDOM_ONE = 'RANDOM_ONE',
  WEIGHTED_RANDOM = 'WEIGHTED_RANDOM',
}

// Discriminated union of filter primitives. Extend as needed.
export type FilterSpec =
  | { kind: 'type'; type: string }
  | { kind: 'tag'; tag: string }
  | { kind: 'propEq'; prop: string; value: string | number | boolean }
  | { kind: 'propRange'; prop: string; min?: number; max?: number }
  | { kind: 'contains'; prop: string; value: string }
  | { kind: 'regex'; prop: string; pattern: string };

export interface OrderSpec {
  prop: string;
  direction?: 'asc' | 'desc';
}

export interface EntityQuery {
  /** Unique query id (qualified) */
  id: string;
  description?: string;
  /** Conjunction (AND) of filters */
  filters?: FilterSpec[];
  selection?: Selection;
  order?: OrderSpec[];
  limit?: number;
  /** Optional id referring to runtime's RandomizationTable for deterministic RNG */
  randomizationTableId?: string;
  /** Optional scope (zone/region id) */
  scope?: string;
}

export interface EntityQueryDescriptor {
  id: string;
  sourcePath?: string; // model path for diagnostics
  query: EntityQuery;
}

export interface QueryExecutionContext {
  /** Runtime instance (opaque) — typed as unknown to avoid tight coupling */
  world?: unknown;
  parameters?: { [k: string]: unknown };
  /** Deterministic RNG helper (0<=x<1). If absent, runtime must provide one. */
  rng?: () => number;
}

export interface QueryIndexStats {
  candidateCount: number;
  lastBuildMillis?: number;
  planSummary?: string;
}

export interface QueryPlan {
  // Opaque planner representation; implementations can extend.
  summary?: string;
  [k: string]: unknown;
}

/**
 * Declarative execution surface for the runtime. Implementations should be
 * immutable-view based and swap views atomically on rebuild.
 */
export interface EntityRepository {
  /**
   * Register declared queries discovered in the model. Called before buildIndexes.
   */
  registerDeclaredQueries(descriptors: Iterable<EntityQueryDescriptor>): void;

  /**
   * Build or refresh indexes using the provided entities. Implementations should
   * produce an immutable view and swap it atomically.
   */
  buildIndexes(allEntities: Iterable<Entry>, context?: unknown): void;

  /** Execute a declared query by id and return a lazy iterable of matches. */
  execute(queryId: string, ctx?: QueryExecutionContext): Iterable<Entry>;

  /** Convenience lookup by id. */
  findById(id: EntityId): Entry | undefined;

  /** Return all registered query descriptors. */
  declaredQueries(): EntityQueryDescriptor[];

  /** Per-query index statistics for diagnostics. */
  indexStats(): Record<string, QueryIndexStats>;
}

// Named export for convenience
export default EntityRepository;
