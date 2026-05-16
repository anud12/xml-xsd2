# Plan: Compile-Time Extraction — Move Execution from QuickJS to Rust

## 1. Problem Statement

QuickJS is currently re-instantiated on every action trigger and effect processing cycle. The module's user-provided closures (`prepare`, `apply`) are stored as JS function references inside the QuickJS VM and executed there each time. This means:

- Every action trigger creates a fresh QuickJS `Runtime` + `Context`
- All bridge scripts (`sim_template.js`, `effect_context.js`, etc.) are re-evaluated
- The user's `index.js` is re-patched and re-evaluated on every invocation
- Entity queries, mutations, and expression evaluation happen inside QuickJS

**Target**: QuickJS runs once at module load. The module's logic is extracted as a Rust data structure (AST). QuickJS is torn down. All subsequent execution happens in Rust.

---

## 2. Core Insight — Closures Are Declarative Builders, Not Imperative Code

The user-written `prepare` and `apply` closures do not contain arbitrary imperative logic. They are sequences of **builder-pattern calls** that construct expression trees:

```javascript
// This closure builds an expression tree — it does NOT execute game logic
apply: (context) => {
  context.getEntityBy(hostApi.entity.filter.create()
    .byId(id => id.isContainingExactly(string.of("entityId"))))
    .map(elementExpr => {
      elementExpr.getNumber(string.of("value"))
        .map(v => v.sum(number.of(1)))
    });
}
```

Each method in this chain returns a wrapper object that carries a reference to a node in a growing AST. The chain:

```
getEntityBy(...) → .map(...) → .getNumber(...) → .map(...) → .sum(...)
```

is a sequence of builder calls that assembles:

```
EntityQuery(Filter(byId(...)))
  → Map(callback)
    → NumberEntityRef(key="value")
      → Sum(addend=Literal(1))
```

**At load time**, the closures are invoked once inside QuickJS with an instrumented context. Each builder call pushes a node into a global node registry. After the closure returns, the root node ID is known, and the entire tree is serialized to JSON, then deserialized into Rust enums.

This is not tracing or interception — it is **construction**. The closure's job is to call builder methods. Each builder method appends to the AST. When the closure finishes, the AST is complete.

---

## 3. What Is "The Algorithm"

The module's "algorithm" is the set of declarations that define:

| Component | Declared via | Contains |
|-----------|-------------|----------|
| **Actions** | `hostApi.registerAction({ name, apply, guard, cooldown, ... })` | Name, input schema, `prepare`/`apply` expression trees, emit targets |
| **Effects** | `hostApi.registerEffect({ name, prepare, apply, ... })` | Name, input/output schema, `prepare`/`apply` expression trees |
| **Entities** | `hostApi.setEntity(id, { textMap, numberMap })` | Entity ID, textMap entries, numberMap entries |
| **Entity creation** | `hostApi.createEntity({ firstName, textMap, numberMap })` | Template data |
| **Panels** | `hostApi.registerPanel({ id, anchor, offset, ... })` | Panel config |

---

## 4. Overall Architecture Shift

### Before

```
Module load:    QuickJS → extract declarations (names, entity data) → Rust state
Action trigger: QuickJS (fresh VM) → run sim_template.js + user closures → Rust state
Effect tick:    QuickJS (fresh VM) → run effect closures → Rust state
```

### After

```
Module load:    QuickJS → invoke closures with builder context → AST node registry → serialize to Rust enums → tear down VM
Action trigger: Rust AST interpreter → evaluate expression trees → Rust state
Effect tick:    Rust AST interpreter → evaluate expression trees → Rust state
```

---

## 5. Phase 1 — Expression AST Design (Rust)

Define the Rust enum hierarchy that represents all expression types the module API can produce.

### 5.1. Primitive Expressions

Each node carries a unique ID assigned by the JS builder at construction time.

```rust
// NumberExpression AST nodes
#[derive(Debug, Clone, Deserialize, Serialize)]
pub enum NumberExprNode {
    Literal(i64),
    Sum(Box<NumberExprNode>, Box<NumberExprNode>),
    Subtract(Box<NumberExprNode>, Box<NumberExprNode>),
    Multiply(Box<NumberExprNode>, Box<NumberExprNode>),
    Divide(Box<NumberExprNode>, Box<NumberExprNode>),
    Random(Box<NumberExprNode>, Box<NumberExprNode>),
    EntityRef { entity_query: EntityQueryNode, key: StringExprNode },
    RuleRef(String),  // reference to a named rule
}

// StringExpression AST nodes
#[derive(Debug, Clone, Deserialize, Serialize)]
pub enum StringExprNode {
    Literal(String),
    Concat(Box<StringExprNode>, Box<StringExprNode>),
    Join(Vec<StringExprNode>, Option<Box<StringExprNode>>),
    OneOf(Vec<StringExprNode>),
    EntityRef { entity_query: EntityQueryNode, key: StringExprNode },
    RuleRef(String),
}

// ConditionExpression AST nodes
#[derive(Debug, Clone, Deserialize, Serialize)]
pub enum ConditionExprNode {
    Literal(bool),
    And(Box<ConditionExprNode>, Box<ConditionExprNode>),
    Or(Box<ConditionExprNode>, Box<ConditionExprNode>),
    Negate(Box<ConditionExprNode>),
    StringContains {
        haystack: Box<StringExprNode>,
        needle: Box<StringExprNode>,
        exact: bool,
    },
    NumberCompare {
        left: Box<NumberExprNode>,
        right: Box<NumberExprNode>,
        op: NumberCmpOp,
    },
    IfTrue {
        condition: Box<ConditionExprNode>,
        then: Box<ConditionExprNode>,
    },
    IfFalse {
        condition: Box<ConditionExprNode>,
        then: Box<ConditionExprNode>,
    },
}
```

### 5.2. Entity Query AST

```rust
#[derive(Debug, Clone, Deserialize, Serialize)]
pub enum EntityQueryNode {
    Filter(EntityFilterNode),
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub enum EntityFilterNode {
    All,
    ById { predicate: ConditionExprNode },
    HasTextValue {
        key: StringExprNode,
        predicate: ConditionExprNode,
    },
    HasNumberValue {
        key: StringExprNode,
        predicate: ConditionExprNode,
    },
    HasContainer(Box<EntityQueryNode>),
    Not(Box<EntityFilterNode>),
    And(Vec<EntityFilterNode>),
    Or(Vec<EntityFilterNode>),
}
```

### 5.3. Mutation AST

```rust
#[derive(Debug, Clone, Deserialize, Serialize)]
pub enum MutationNode {
    SetTextMapValue {
        entity: EntityQueryNode,
        key: StringExprNode,
        value: StringExprNode,
    },
    SetNumberMapValue {
        entity: EntityQueryNode,
        key: StringExprNode,
        value: NumberExprNode,
    },
    ConcatText {
        entity: EntityQueryNode,
        key: StringExprNode,
        suffix: StringExprNode,
    },
    SumNumber {
        entity: EntityQueryNode,
        key: StringExprNode,
        addend: NumberExprNode,
    },
    CreateEntity {
        text_map: Vec<(StringExprNode, StringExprNode)>,
        number_map: Vec<(StringExprNode, NumberExprNode)>,
    },
    EmitEvent {
        event_name: StringExprNode,
        payload: serde_json::Value,
    },
    Log {
        message: StringExprNode,
    },
}
```

### 5.4. Top-Level Compiled Module

```rust
#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct CompiledModule {
    pub actions: Vec<CompiledAction>,
    pub effects: Vec<CompiledEffect>,
    pub entities: Vec<CompiledEntity>,
    pub panels: Vec<CompiledPanel>,
    pub created_by: HashMap<String, Vec<String>>,
    pub emits_map: HashMap<String, Vec<String>>,
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct CompiledAction {
    pub name: String,
    pub prepare: Vec<MutationNode>,
    pub apply: Vec<MutationNode>,
    pub guard: Option<ConditionExprNode>,
    pub cooldown: Option<TemporalExprNode>,
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct CompiledEffect {
    pub name: String,
    pub prepare: Vec<MutationNode>,
    pub apply: Vec<MutationNode>,
    pub reoccur_after_ms: Option<NumberExprNode>,
    pub is_reoccurrence_applicable: Option<ConditionExprNode>,
}
```

**Files to create**: `application/runtime/src/module/compiled_ast/` (new directory)
- `mod.rs` — re-exports
- `expr.rs` — `NumberExprNode`, `StringExprNode`, `ConditionExprNode`
- `query.rs` — `EntityQueryNode`, `EntityFilterNode`
- `mutation.rs` — `MutationNode`
- `module.rs` — `CompiledModule`, `CompiledAction`, `CompiledEffect`

---

## 6. Phase 2 — Expression Evaluator (Rust)

Implement the Rust evaluator that interprets the AST nodes against the world state.

### 6.1. Evaluator Trait

```rust
pub struct ExecutionContext {
    pub world_seed: u64,
    pub tick_id: u64,
    pub source_entity_id: String,
    pub action_id: String,
    pub call_index: u32,
}

pub struct WorldState<'a> {
    pub entities: &'a HashMap<String, EntityData>,
    pub containers: &'a HashMap<String, ContainerData>,
}

pub trait Evaluate {
    type Output;
    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<Self::Output>;
}
```

### 6.2. Primitive Evaluators

```rust
impl Evaluate for NumberExprNode {
    type Output = i64;
    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<i64> {
        match self {
            NumberExprNode::Literal(v) => Ok(*v),
            NumberExprNode::Sum(a, b) => Ok(a.evaluate(ctx, world)? + b.evaluate(ctx, world)?),
            NumberExprNode::EntityRef { entity_query, key } => {
                let entity = entity_query.evaluate(ctx, world)?;
                let key_val = key.evaluate(ctx, world)?;
                Ok(entity.number_map.get(&key_val).copied().unwrap_or(0))
            }
            // ... other variants
        }
    }
}

impl Evaluate for StringExprNode {
    type Output = String;
    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<String> { /* ... */ }
}

impl Evaluate for ConditionExprNode {
    type Output = bool;
    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<bool> { /* ... */ }
}
```

### 6.3. Mutation Execution

```rust
pub fn apply_mutation(
    mutation: &MutationNode,
    ctx: &ExecutionContext,
    world: &WorldState,
    write_buffer: &mut WriteBuffer,
) -> Result<()> {
    match mutation {
        MutationNode::SumNumber { entity, key, addend } => {
            let target = entity.evaluate(ctx, world)?;
            let key_val = key.evaluate(ctx, world)?;
            let delta = addend.evaluate(ctx, world)?;
            let current = world.entities.get(&target)
                .and_then(|e| e.number_map.get(&key_val)).copied().unwrap_or(0);
            write_buffer.set_number(target, key_val, current + delta);
        }
        MutationNode::EmitEvent { event_name, payload } => {
            let name = event_name.evaluate(ctx, world)?;
            ctx.emit_effect(&name, payload.clone());
        }
        // ... other variants
    }
}
```

**Files to create**: `application/runtime/src/module/evaluator/` (new directory)
- `mod.rs` — `Evaluate` trait, `ExecutionContext`, `WorldState`, `WriteBuffer`
- `number.rs` — `NumberExprNode` evaluator
- `string.rs` — `StringExprNode` evaluator
- `condition.rs` — `ConditionExprNode` evaluator
- `mutation.rs` — `apply_mutation`

---

## 7. Phase 3 — QuickJS Builder Bridge (The Core Change)

### 7.1. Conceptual Model

The module's `prepare` and `apply` closures call **builder methods** that return wrapper objects. Each wrapper method pushes a node into a global AST node registry (`__astNodes`). When the closure finishes, the accumulated node registry is serialized to JSON and sent to Rust.

The bridge does not "trace" calls. It provides each builder method with the ability to register its AST node.

### 7.2. Node Registry (JS side)

```javascript
// compiler_bridge.js

// Global registry: nodeId -> AST node description
globalThis.__astNodes = {};
var __nextNodeId = 0;

function newId() { return __nextNodeId++; }

function registerNode(node) {
  var id = newId();
  node.id = id;
  globalThis.__astNodes[id] = node;
  return id;
}
```

### 7.3. Builder Wrappers — Number Expression

```javascript
// number.of(1) creates a literal node
function numberOf(value) {
  return registerNode({ type: "NumberLiteral", value: value });
}

// .sum(other) creates a Sum node referencing child IDs
function numberWrapper(childId) {
  return {
    sum: function(otherId) {
      return registerNode({
        type: "Sum",
        left: childId,
        right: otherId
      });
    },
    // ... subtract, multiply, divide, random, isGreaterThan, etc.
  };
}
```

### 7.4. Builder Wrappers — Entity Query and Mutation

This is the most important part. The `apply` closure's chain of calls must each produce an AST node.

```javascript
// getEntityBy(filter) produces an EntityQuery node
function instrumentedGetEntityBy(filterId) {
  var queryId = registerNode({
    type: "EntityQuery",
    filter: filterId
  });
  return listWrapper(queryId);
}

// .map(callback) records a Map node, then invokes the callback with
// an instrumented element wrapper that itself produces mutation nodes
function listWrapper(queryId) {
  return {
    map: function(callback) {
      // Create a synthetic element wrapper that records mutations
      var elementId = registerNode({
        type: "MapElementRef",
        sourceQuery: queryId
      });
      var elementWrapper = instrumentedElement(elementId);

      // Invoke the user's callback with the instrumented element
      // This causes the callback to build nested AST nodes
      callback(elementWrapper);

      // The mutations recorded inside the callback are already in __astNodes
      // They are linked to this Map node
      return registerNode({
        type: "Map",
        query: queryId,
        element: elementId
      });
    },
    randomElement: function() {
      return registerNode({
        type: "RandomElement",
        query: queryId
      });
    }
  };
}

// The instrumented entity element — getNumber/getText return number/string wrappers
// whose methods (sum, concat) produce mutation nodes
function instrumentedElement(entityRefId) {
  return {
    getNumber: function(keyId) {
      return numberWrapper(registerNode({
        type: "NumberEntityRef",
        entity: entityRefId,
        key: keyId
      }));
    },
    getText: function(keyId) {
      return stringWrapper(registerNode({
        type: "StringEntityRef",
        entity: entityRefId,
        key: keyId
      }));
    }
  };
}
```

### 7.5. Example Walkthrough

For this module code:

```javascript
apply: (context) => {
  context.getEntityBy(hostApi.entity.filter.create()
    .byId(id => id.isContainingExactly(string.of("entityId"))))
    .map(elementExpr => {
      elementExpr.getNumber(string.of("value"))
        .map(v => v.sum(number.of(1)));
    });
}
```

The builder bridge produces this node registry:

| ID | Node |
|----|------|
| 0 | `{ type: "StringLiteral", value: "entityId" }` |
| 1 | `{ type: "StringLiteral", value: "value" }` |
| 2 | `{ type: "NumberLiteral", value: 1 }` |
| 3 | `{ type: "EntityFilterById", predicate: { type: "StringContains", haystackRef: "self", needle: 0, exact: true } }` |
| 4 | `{ type: "EntityQuery", filter: 3 }` |
| 5 | `{ type: "MapElementRef", sourceQuery: 4 }` |
| 6 | `{ type: "NumberEntityRef", entity: 5, key: 1 }` |
| 7 | `{ type: "SumNumber", entity: 5, key: 1, addend: 2 }` |
| 8 | `{ type: "Map", query: 4, mutation: 7 }` |

After the closure returns, Rust receives the full registry as JSON and reconstructs the tree.

### 7.6. Closure Invocation and Extraction (Rust side)

In `js_executor/clean.rs`, the compile path:

```rust
pub fn compile_module(source: &str) -> Result<CompiledModule> {
    // 1. Create QuickJS runtime + context
    let (rt, ctx) = prepare_runtime_and_ctx()?;
    install_compiler_bridge(&ctx)?;  // loads compiler_bridge.js

    // 2. Evaluate user source (patched: export default -> var __module_default)
    //    This registers actions/effects but does NOT execute their closures yet

    // 3. For each registered action/effect:
    for action in registered_actions {
        // Clear the node registry
        clear_ast_nodes(&ctx)?;

        // Invoke the action's prepare closure with the instrumented builder context
        invoke_closure_with_builder_context(&ctx, &action.name, ClosureKind::Prepare)?;

        // Extract the node registry as JSON
        let prepare_nodes_json = extract_ast_nodes_json(&ctx)?;

        // Parse into Rust structs
        let prepare_mutations = parse_mutation_nodes(&prepare_nodes_json)?;

        // Repeat for apply
        clear_ast_nodes(&ctx)?;
        invoke_closure_with_builder_context(&ctx, &action.name, ClosureKind::Apply)?;
        let apply_nodes_json = extract_ast_nodes_json(&ctx)?;
        let apply_mutations = parse_mutation_nodes(&apply_nodes_json)?;

        compiled.actions.push(CompiledAction {
            name: action.name,
            prepare: prepare_mutations,
            apply: apply_mutations,
            guard: action.guard,
            cooldown: action.cooldown,
        });
    }

    // 4. Same for effects
    for effect in registered_effects {
        // ... same pattern ...
    }

    // 5. Return CompiledModule — QuickJS runtime is dropped here
    Ok(compiled)
}
```

### 7.7. New JS Bridge Script

**File**: `application/runtime/src/js/scripts/compiler_bridge.js`

This replaces the current `bridge.js` during the compile phase. It provides:

- `__astNodes` registry and `newId()` / `registerNode()`
- Instrumented `hostApi` that wraps every builder method
- Instrumented `EventContext` (the `context` passed to `apply`) with proxy `getEntityBy`, `emitEvent`, `createEntity`, `log`
- `__flushAstNodes()` — returns `JSON.stringify(__astNodes)` for Rust to consume
- `__clearAstNodes()` — resets registry between closure invocations

---

## 8. Phase 4 — Rust Execution Engine

Replace the QuickJS-based `simulate_action` and `process_pending_effects` with Rust execution.

### 8.1. Action Execution

```rust
pub fn rust_simulate_action(
    compiled: &CompiledModule,
    action_name: &str,
    tick_id: u64,
    source_entity_id: String,
) -> Result<ExecutionResult> {
    let action = compiled.actions.iter()
        .find(|a| a.name == action_name)
        .ok_or_else(|| anyhow!("action not found: {}", action_name))?;

    let ctx = ExecutionContext::new(tick_id, source_entity_id, &action.name);
    let world = WorldState::snapshot(&state::get_world());
    let mut write_buffer = WriteBuffer::new();

    // Evaluate prepare mutations
    for mutation in &action.prepare {
        apply_mutation(mutation, &ctx, &world, &mut write_buffer)?;
    }

    // Evaluate apply mutations
    for mutation in &action.apply {
        apply_mutation(mutation, &ctx, &world, &mut write_buffer)?;
    }

    // Commit (or abort on error)
    write_buffer.commit()?;
    Ok(ExecutionResult::from(write_buffer))
}
```

### 8.2. Effect Processing

```rust
pub fn rust_process_effects(
    compiled: &CompiledModule,
    pending: &[PendingEffect],
    tick_id: u64,
) -> Result<()> {
    for pending in pending {
        let effect = compiled.effects.iter()
            .find(|e| e.name == pending.name)
            .ok_or_else(|| anyhow!("effect not found: {}", pending.name))?;

        let ctx = ExecutionContext::new(tick_id, pending.source, &effect.name);
        let world = WorldState::snapshot(&state::get_world());
        let mut write_buffer = WriteBuffer::new();

        for mutation in &effect.apply {
            apply_mutation(mutation, &ctx, &world, &mut write_buffer)?;
        }

        write_buffer.commit()?;
    }
    Ok(())
}
```

### 8.3. FFI Changes

Update `ffi_mod/debug/debug_simulate_action.rs`:

```rust
// Before: crate::js_executor::simulate_action(&files_map, name, &current_entities)
// After:  rust_simulate_action(&compiled_module, name, tick_id, source_entity_id)
```

Remove the `files_map` rebuild — it is no longer needed after compilation.

---

## 9. Phase 5 — State Migration

### 9.1. Compiled Module Storage

Replace the current per-row state caches with a single compiled module reference:

```rust
// In state.rs, replace:
//   static mut LAST_ACTION_ROWS: ...
//   static mut LAST_ENTITY_ROWS: ...
//   static mut LAST_ENTITY_PATTERNS: ...
//   etc.

// With:
static mut COMPILED_MODULE: Option<&'static Mutex<CompiledModule>> = None;
```

### 9.2. Export Struct Update

`export_state_struct.rs` should derive entities, actions, events from the compiled module rather than from row-based caches.

---

## 10. Phase 6 — Cleanup

### 10.1. Remove QuickJS Dependencies from Runtime Path

Files that become dead code after the transition:
- `js_executor/clean.rs` — `simulate_action`, `process_pending_effects` (compile-only variant remains)
- `js/scripts/sim_template.js` — deleted
- `js/scripts/effect_script.js` — deleted
- `js/scripts/debug_effect.js` — deleted
- `js/scripts/effect_context.js` — deleted
- `js/scripts/entity_store.js` — deleted
- `js/scripts/entity_store_sync.js` — deleted

The `compile_module` function and `compiler_bridge.js` remain for module load time.

### 10.2. Cargo.toml Changes

`rquickjs` stays as a dependency (needed for module load and hot-reload), but is no longer on the hot path.

---

## 11. Risk Analysis

| Risk | Mitigation |
|------|-----------|
| **Builder pattern doesn't cover all closure patterns** — some closures use dynamic control flow (`if/for/switch`) rather than pure builder chains | The spec defines the API surface. The builder bridge only needs to cover the declared API. If a closure does something outside the API, the compiler rejects the module at load time with a clear error |
| **Nested callbacks (`.map(cb => ...)`) are hard to serialize** — the callback is invoked inside the builder, producing nodes that reference a synthetic element | The `MapElementRef` node type solves this. The element wrapper is instrumented to produce mutation nodes that reference back to the query |
| **Existing tests break** — 50+ tests depend on the QuickJS execution path | Run tests against both backends during transition. Use a feature flag (`--features quickjs-execution`) to toggle |
| **Performance regression** — Rust AST interpretation may be slower than QuickJS for simple cases | Benchmark early. QuickJS has VM startup overhead per invocation; the Rust path should be faster for repeated execution |

---

## 12. Implementation Order

1. **Phase 1** — Define the AST types (Rust enums). Low risk, pure data structures.
2. **Phase 2** — Implement the evaluator against static test data. No QuickJS involved yet.
3. **Phase 2b** — Write Rust unit tests for the evaluator: construct AST nodes manually, verify evaluation.
4. **Phase 3** — Build the compiler bridge (`compiler_bridge.js`) and extraction logic (`compile_module`). This is the hardest part.
5. **Phase 4** — Wire the Rust execution engine into the FFI path. Feature-flag it behind the existing path.
6. **Phase 5** — Migrate state management.
7. **Phase 6** — Remove QuickJS execution path, clean up dead files.

---

## 13. Verification Criteria

- All existing JUnit tests pass with the Rust execution backend
- `runtime_debug_simulate_action` produces identical state for the same inputs
- Entity mutations (textMap concat, numberMap sum) produce correct values
- Effect processing chains produce correct pending effects
- Module load time is acceptable (compilation is a one-time cost)
- Per-action latency is lower than the QuickJS path (no VM bootstrap)
