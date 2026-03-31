# Repository API

## Purpose
The repository is a global, read-only querying facade provided through HostApi. It exposes fast, indexed lookups for runtime model entries (entities, containers, zones, regions, and rule entries) so modules and host code can discover and inspect the world model.

## Indexing & consistency
- Repositories are populated by the engine's index() functions. Each repository typically builds an id -> Entry map for O(1) id lookups and may maintain additional indexes for common query axes (zone, region, classification, name rule, etc.).
- Indexes are rebuilt atomically when the world model changes (module load, boot, or data reload). Callers may treat repository results as an immutable snapshot for the duration of a tick.
- Reads are thread-safe; mutation of repository state is performed only by the host runtime.

## Primary query primitive
- getEntities(filter)
  - Query the repository for entities using an EntityFilter.
  - Accepts an EntityFilter object. See `entityFilter.md` for field-level details.
  - Returns: an array (possibly empty) of Entity runtime model objects. Implementations SHOULD always return a consistent, array-based result to avoid binding-specific surprises.
  - Semantics: when the filter provides an explicit id or set of ids, the repository will use the id index to resolve matches quickly. Field-based matching follows the rules documented in `entityFilter.md`.

- getContainers(filter)
  - Query the repository for containers using a ContainerFilter.
  - Accepts a ContainerFilter object. See `containerFilter.md` for field-level details.
  - Returns: an array (possibly empty) of Container runtime model objects.
  - Semantics: when the filter provides an explicit id or set of ids, the repository will use the id index to resolve matches quickly. Field-based matching follows the rules documented in `containerFilter.md`.

- Note: Implementations may provide additional, specialized query entry points, but host bindings SHOULD expose the two canonical functions above. Both functions MUST be snapshot-consistent and deterministic for the current ExecutionContext.

## Result shapes & immutability
- Results are typed according to the filter used. Do not assume underlying implementation classes beyond the documented model interfaces.
- Returned entries are authoritative runtime objects and SHOULD be treated as read-only by callers. If modifications are required, clone or construct a new object.

## Performance guidance
- Id-based lookups are O(1). Queries that can be satisfied by existing indexes (id, zone, region, classification) are fast.
- Complex predicate combinations that are not covered by precomputed indexes may degrade to scanning the relevant index; prefer explicit indexed fields for high-throughput code.

- After loading scripts, the runtime MUST analyze each module's AST to extract all statically-known query and predicate patterns used across the application and MUST generate composite indexes (multi-field indexes, reverse mappings, and predicate lookup tables) so commonly used predicate combinations resolve in O(1).

- Implementations MUST expose available index signatures and their estimated cost and memory footprint, and MUST only fall back to scans when no supporting index exists.

- Implementations MUST enforce safeguards to prevent index explosion (configurable maximum index arity, cardinality thresholds, and a total index memory budget).

## Error handling
- Missing results are represented as an empty array; the repository does not throw for "no matches".
- Malformed filters or unsupported filter types are host errors — callers should validate filter shapes before invoking get(filter).


## Best practices
- Prefer id-based filters when possible for performance and determinism.
- Treat repository results as snapshot, read-only data.
- Keep queries simple; push complex selection logic to host-side helpers or precomputed indexes if needed.

For field-level filter options and semantics, see the filter reference pages: [`entityFilter.md`](./entityFilter.md) and [`containerFilter.md`](./containerFilter.md).

## Example

The following conceptual TypeScript shows common repository usage patterns. Prefer id-based lookups for performance, pin snapshots for multi-call consistency, and use the filter builders exposed by hostApi.

```ts
// Fast id-based lookup (preferred when you know the id)
const byId = hostApi.entity.filter.create().byId(id => id.equals(hostApi.string.of("entity-123")));
const single = hostApi.repository.getEntities(byId);

// Combine container and entity filters: find NPCs in zone "forest-entrance"
const zone = hostApi.container.filter.create().byId(hostApi.string.of("forest-entrance"));
const inZoneNpcs = hostApi.entity.filter.create()
  .hasContainer(zone)
  .and(hostApi.entity.filter.create().byClassification(hostApi.string.of("npc")));
const npcs = hostApi.repository.getEntities(inZoneNpcs);

```

Notes:
- Use hostApi.entity.filter and hostApi.container.filter builder helpers (see entityFilter.md and containerFilter.md).
- Favor indexed fields (id, zone, region, classification) for performance and determinism.
- Treat repository results as read-only snapshot objects.

## Host API

The TypeScript surface below describes the HostApi declarations relevant to repository usage. These are conceptual bindings; concrete host implementations may refine types for each language/platform.

```ts
// Partial HostApi surface (repository-related types)
export type HostApi = {
  // other host APIs omitted for brevity
  repository: RepositoryApi;
}

export type RepositoryApi = {
  /**
   * Query the repository for entities/containers using typed filters.
   * Returns an array (possibly empty) of runtime model objects matching the filter.
   */
  getEntities(filter: EntityFilter): Array<any>;
  getContainers(filter: ContainerFilter): Array<any>;

}

```


Notes:
- EntityFilter and ContainerFilter types are defined in entityFilter.md and containerFilter.md and are exposed via hostApi.entity.filter and hostApi.container.filter helper factories.
- Implementations MUST keep getEntities()/getContainers() snapshot-consistent and deterministic for a given ExecutionContext.
- listIndexes() helps modules understand which predicate combinations are supported in O(1) and choose efficient query forms; implementations SHOULD surface estimated costs so modules can make informed choices.




