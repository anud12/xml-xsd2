#![allow(dead_code)]
use anyhow::Result;
use rquickjs::Context;
use std::collections::HashMap;

use crate::js_host_api::Declarations;
use crate::js_runtime::{create_context, create_runtime};
use crate::module::compiled_ast::expr::{ConditionExprNode, NumberExprNode, NumberCmpOp, StringExprNode};
use crate::module::compiled_ast::module::{CompiledAction, CompiledEffect, CompiledEntity, CompiledModule, CompiledPanel};
use crate::module::compiled_ast::query::{EntityFilterNode, EntityQueryNode};
use crate::module::compiled_ast::mutation::MutationNode;

// ---- Script includes ----

fn compiler_bridge_js() -> &'static str {
    include_str!("../js/scripts/compiler_bridge.js")
}

fn globals_js() -> &'static str {
    include_str!("../js/scripts/globals.js")
}

fn module_call_js() -> &'static str {
    include_str!("../js/scripts/module_call.js")
}

fn entity_store_js() -> &'static str {
    include_str!("../js/scripts/entity_store.js")
}

fn compile_action_template() -> &'static str {
    include_str!("../js/scripts/compile_action.js")
}

fn compile_effect_template() -> &'static str {
    include_str!("../js/scripts/compile_effect.js")
}

fn compile_effect_prepare_template() -> &'static str {
    include_str!("../js/scripts/compile_effect_prepare.js")
}

fn compile_panels_template() -> &'static str {
    include_str!("../js/scripts/compile_panels.js")
}

// ---- Source patching (same as extraction) ----

fn patch_user_source(source: &str) -> String {
    let result = source
        .replace("({string, number, ...hostApi})", "({...hostApi})")
        .replace("({ string, number, ...hostApi })", "({...hostApi})");
    result
        .replace("({string, ...hostApi})", "({...hostApi})")
        .replace("({number, ...hostApi})", "({...hostApi})")
}

// ---- Compilation entry point ----

pub fn compile_module(module_src: &str, dec: &Declarations) -> Result<CompiledModule> {
    let rt = create_runtime()?;
    let ctx = create_context(&rt)?;

    // Install host API first (needed by user source)
    crate::js_host_api::install_host_api(&ctx)?;

    // Step 1: Set up compiler bridge
    ctx.with(|c| c.eval::<(), _>(compiler_bridge_js()))
        .map_err(|e| anyhow::anyhow!("compiler_bridge eval error: {}", e))?;

    // Step 2: Set up globals (string, number)
    ctx.with(|c| c.eval::<(), _>(globals_js()))
        .map_err(|e| anyhow::anyhow!("globals eval error: {}", e))?;

   // Step 2b: Instrument hostApi methods IN PLACE BEFORE module evaluates
    // This ensures closures capture instrumented methods
    ctx.with(|c| c.eval::<(), _>("instrumentHostApiInPlace();"))
        .map_err(|e| anyhow::anyhow!("instrumentHostApiInPlace error: {}", e))?;

    // Step 3: Evaluate user source (patched)
    let patched = patch_user_source(module_src);
    let transformed = if patched.contains("export default") {
        patched.replace("export default", "var __module_default =")
    } else {
        patched
    };
    ctx.with(|c| c.eval::<(), _>(transformed))
        .map_err(|e| anyhow::anyhow!("user source eval error: {}", e))?;

    // Step 4: Call __module_default with hostApi
    ctx.with(|c| c.eval::<(), _>(module_call_js()))
        .map_err(|e| anyhow::anyhow!("module call eval error: {}", e))?;

    // Step 5: Build entity store from __entityData
    ctx.with(|c| c.eval::<(), _>(entity_store_js()))
        .map_err(|e| anyhow::anyhow!("entity store eval error: {}", e))?;

    // Step 6: Compile each action
    let actions = compile_actions(&ctx, &dec.actions)?;

    // Step 7: Compile each effect
    let effects = compile_effects(&ctx, &dec.events)?;

    // Step 7b: Compile panels (flush compiled panels + AST nodes)
    let (compiled_panels, ast_nodes) = compile_panels(&ctx)?;

    // Step 8: Build entities from entity_data
    let entities = build_entities_from_declarations(dec);

    // Build compiled module
    let compiled = CompiledModule {
        actions,
        effects,
        entities,
        panels: compiled_panels.clone(),
        created_by: dec.creators.clone(),
        emits_map: dec.emits.clone(),
    };

    // Merge compiled panels into the panels cache so C# reads structured content
    merge_compiled_panels_into_cache(&compiled_panels, &ast_nodes);

    // Persist compiled AST registry for runtime FFI evaluation
    crate::state::set_compiled_ast_nodes(ast_nodes.clone());

    // ctx is dropped here, cleaning up QuickJS resources
    Ok(compiled)
}

fn compile_actions(ctx: &Context, action_names: &[String]) -> Result<Vec<CompiledAction>> {
    let mut compiled_actions = Vec::new();
    for name in action_names {
        let script = compile_action_template().replace("ACTION_NAME_PLACEHOLDER", &format!("'{}'", name));
        let json_str = ctx.with(|c| c.eval::<String, _>(script.as_str()))
            .map_err(|e| anyhow::anyhow!("compile action '{}' error: {}", name, e))?;
        let mutations = parse_ast_nodes(&json_str)?;
        compiled_actions.push(CompiledAction {
            name: name.clone(),
            prepare: Vec::new(),
            apply: mutations,
            guard: None,
        });
    }
    Ok(compiled_actions)
}

 fn compile_effects(ctx: &Context, effect_names: &[String]) -> Result<Vec<CompiledEffect>> {
    let mut compiled_effects = Vec::new();
    for name in effect_names {
        // Compile prepare mutations
        let prepare_script = compile_effect_prepare_template()
            .replace("EFFECT_NAME_PLACEHOLDER", &format!("'{}'", name));
        let prepare_json = ctx.with(|c| c.eval::<String, _>(prepare_script.as_str()))
            .map_err(|e| anyhow::anyhow!("compile effect '{}' prepare error: {}", name, e))?;
        let prepare_mutations = parse_ast_nodes(&prepare_json)?;

        // Compile apply mutations
        let apply_script = compile_effect_template()
            .replace("EFFECT_NAME_PLACEHOLDER", &format!("'{}'", name));
        let apply_json = ctx.with(|c| c.eval::<String, _>(apply_script.as_str()))
            .map_err(|e| anyhow::anyhow!("compile effect '{}' apply error: {}", name, e))?;
        let apply_mutations = parse_ast_nodes(&apply_json)?;

        compiled_effects.push(CompiledEffect {
            name: name.clone(),
            prepare: prepare_mutations,
            apply: apply_mutations,
            reoccur_after_ms: None,
            is_reoccurrence_applicable: None,
        });
    }
    Ok(compiled_effects)
}

fn build_entities_from_declarations(dec: &Declarations) -> Vec<CompiledEntity> {
    let mut entities = Vec::new();
    if let serde_json::Value::Object(entities_obj) = &dec.entity_data {
        for (id, val) in entities_obj {
            let mut text_map = HashMap::new();
            let mut number_map = HashMap::new();
            if let Some(tm) = val.get("textMap").and_then(|v| v.as_object()) {
                for (k, v) in tm {
                    if let Some(s) = v.as_str() {
                        text_map.insert(k.clone(), s.to_string());
                    }
                }
            }
            if let Some(nm) = val.get("numberMap").and_then(|v| v.as_object()) {
                for (k, v) in nm {
                    if let Some(n) = v.as_f64() {
                        number_map.insert(k.clone(), n as i64);
                    }
                }
            }
            entities.push(CompiledEntity {
                id: id.clone(),
                text_map,
                number_map,
            });
        }
    }
    entities
}

// ---- AST Node Parser ----

/// Parse JSON AST nodes from the compiler bridge registry into Rust mutation nodes.
/// The registry is a flat map of id -> node. We need to find mutation nodes (top-level actions)
/// and build expression nodes from their references.
fn parse_ast_nodes(json_str: &str) -> Result<Vec<MutationNode>> {
    let nodes: HashMap<u64, serde_json::Value> =
        serde_json::from_str(json_str).map_err(|e| anyhow::anyhow!("parse AST JSON error: {}", e))?;

    let mut mutation_nodes = Vec::new();

    // Identify mutation node types
    let mutation_types = [
        "EmitEvent", "Log", "CreateEntity", "SumNumber", "SetNumberMapValue",
        "SetTextMapValue", "ConcatText",
    ];

    // First pass: find all mutation nodes
    for (_id, node_val) in &nodes {
        if let Some(node_type) = node_val.get("type").and_then(|v| v.as_str()) {
            if mutation_types.contains(&node_type) {
                if let Ok(mutation) = parse_mutation_node(node_val, &nodes) {
                    mutation_nodes.push(mutation);
                }
            }
        }
    }

    // If no mutation nodes found, add a no-op Log for empty apply closures
    if mutation_nodes.is_empty() {
        // This is fine — empty apply closures produce no mutations
    }

    Ok(mutation_nodes)
}

fn parse_mutation_node(node: &serde_json::Value, _registry: &HashMap<u64, serde_json::Value>) -> Result<MutationNode> {
    let node_type = node.get("type").and_then(|v| v.as_str())
        .ok_or_else(|| anyhow::anyhow!("missing node type"))?;

    match node_type {
        "EmitEvent" => {
            let event_name = parse_string_expr(node.get("eventName"), _registry)?;
            let payload = node.get("payload")
                .cloned()
                .unwrap_or_else(|| serde_json::Value::Object(Default::default()));
            Ok(MutationNode::EmitEvent { event_name, payload })
        }
        "Log" => {
            let message = parse_string_expr(node.get("message"), _registry)?;
            Ok(MutationNode::Log { message })
        }
        "CreateEntity" => {
            let text_map = parse_text_map(node.get("textMap"), _registry)?;
            let number_map = parse_number_map(node.get("numberMap"), _registry)?;
            Ok(MutationNode::CreateEntity { text_map, number_map })
        }
        "SumNumber" => {
            // SumNumber node in the registry has: entity (query), key, addend
            let entity = parse_entity_query(node.get("entity"), _registry)?;
            let key = parse_string_expr(node.get("key"), _registry)?;
            let addend = parse_number_expr(node.get("addend"), _registry)?;
            Ok(MutationNode::SumNumber { entity, key, addend })
        }
        "SetNumberMapValue" => {
            let entity = parse_entity_query(node.get("entity"), _registry)?;
            let key = parse_string_expr(node.get("key"), _registry)?;
            let value = parse_number_expr(node.get("value"), _registry)?;
            Ok(MutationNode::SetNumberMapValue { entity, key, value })
        }
        _ => Err(anyhow::anyhow!("unknown mutation node type: {}", node_type)),
    }
}

fn parse_string_expr(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<StringExprNode> {
    match val {
        Some(v) => {
            // Could be an ID reference or an inline node
            if let Some(id) = v.get("id").and_then(|id| id.as_u64()) {
                parse_string_expr_from_id(id, registry)
            } else if let Some(id) = v.as_u64() {
                parse_string_expr_from_id(id, registry)
            } else if let Some(type_str) = v.get("type").and_then(|t| t.as_str()) {
                parse_string_expr_inline(v, type_str, registry)
            } else {
                // Raw string value
                let s = v.as_str().unwrap_or("").to_string();
                Ok(StringExprNode::Literal(s))
            }
        }
        None => Ok(StringExprNode::Literal(String::new())),
    }
}

fn parse_string_expr_inline(node: &serde_json::Value, type_str: &str, registry: &HashMap<u64, serde_json::Value>) -> Result<StringExprNode> {
    match type_str {
        "StringLiteral" => {
            let val = node.get("value").and_then(|v| v.as_str()).unwrap_or("").to_string();
            Ok(StringExprNode::Literal(val))
        }
        "StringConcat" => {
            let left = parse_string_expr(node.get("left"), registry)?;
            let right = parse_string_expr(node.get("right"), registry)?;
            Ok(StringExprNode::Concat(Box::new(left), Box::new(right)))
        }
        "StringEntityRef" => {
            let query = parse_entity_query_from_elem(node.get("element"), registry)?;
            let key = parse_string_expr(node.get("key"), registry)?;
            Ok(StringExprNode::EntityRef {
                entity_query: Box::new(query),
                key: Box::new(key),
            })
        }
        _ => {
            let s = node.get("value").and_then(|v| v.as_str()).unwrap_or("").to_string();
            Ok(StringExprNode::Literal(s))
        }
    }
}

fn parse_string_expr_from_id(id: u64, registry: &HashMap<u64, serde_json::Value>) -> Result<StringExprNode> {
    let node = registry.get(&id).ok_or_else(|| anyhow::anyhow!("string expr id {} not found", id))?;
    let type_str = node.get("type").and_then(|v| v.as_str()).unwrap_or("StringLiteral");
    parse_string_expr_inline(node, type_str, registry)
}

fn parse_number_expr(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<NumberExprNode> {
    match val {
        Some(v) => {
            if let Some(id) = v.get("id").and_then(|id| id.as_u64()) {
                parse_number_expr_from_id(id, registry)
            } else if let Some(id) = v.as_u64() {
                parse_number_expr_from_id(id, registry)
            } else if let Some(type_str) = v.get("type").and_then(|t| t.as_str()) {
                parse_number_expr_inline(v, type_str, registry)
            } else {
                let n = v.as_i64().unwrap_or(0);
                Ok(NumberExprNode::Literal(n))
            }
        }
        None => Ok(NumberExprNode::Literal(0)),
    }
}

fn parse_number_expr_inline(node: &serde_json::Value, type_str: &str, registry: &HashMap<u64, serde_json::Value>) -> Result<NumberExprNode> {
    match type_str {
        "NumberLiteral" => {
            let val = node.get("value").and_then(|v| v.as_f64()).unwrap_or(0.0) as i64;
            Ok(NumberExprNode::Literal(val))
        }
        "NumberSum" => {
            let left = parse_number_expr(node.get("left"), registry)?;
            let right = parse_number_expr(node.get("right"), registry)?;
            Ok(NumberExprNode::Sum(Box::new(left), Box::new(right)))
        }
        "NumberSubtract" => {
            let left = parse_number_expr(node.get("left"), registry)?;
            let right = parse_number_expr(node.get("right"), registry)?;
            Ok(NumberExprNode::Subtract(Box::new(left), Box::new(right)))
        }
        "NumberMultiply" => {
            let left = parse_number_expr(node.get("left"), registry)?;
            let right = parse_number_expr(node.get("right"), registry)?;
            Ok(NumberExprNode::Multiply(Box::new(left), Box::new(right)))
        }
        "NumberDivide" => {
            let left = parse_number_expr(node.get("left"), registry)?;
            let right = parse_number_expr(node.get("right"), registry)?;
            Ok(NumberExprNode::Divide(Box::new(left), Box::new(right)))
        }
        "NumberEntityRef" => {
            let query = parse_entity_query_from_elem(node.get("element"), registry)?;
            let key = parse_string_expr(node.get("key"), registry)?;
            Ok(NumberExprNode::EntityRef {
                entity_query: Box::new(query),
                key: Box::new(key),
            })
        }
        _ => {
            let val = node.get("value").and_then(|v| v.as_f64()).unwrap_or(0.0) as i64;
            Ok(NumberExprNode::Literal(val))
        }
    }
}

fn parse_number_expr_from_id(id: u64, registry: &HashMap<u64, serde_json::Value>) -> Result<NumberExprNode> {
    let node = registry.get(&id).ok_or_else(|| anyhow::anyhow!("number expr id {} not found", id))?;
    let type_str = node.get("type").and_then(|v| v.as_str()).unwrap_or("NumberLiteral");
    parse_number_expr_inline(node, type_str, registry)
}

fn parse_entity_query(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityQueryNode> {
    match val {
        Some(v) => {
            if let Some(id) = v.get("id").and_then(|id| id.as_u64()) {
                parse_entity_query_from_id(id, registry)
            } else if let Some(id) = v.as_u64() {
                parse_entity_query_from_id(id, registry)
            } else {
                parse_entity_query_inline(v, registry)
            }
        }
        None => Ok(EntityQueryNode::Filter(EntityFilterNode::All)),
    }
}

fn parse_entity_query_inline(node: &serde_json::Value, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityQueryNode> {
    let type_str = node.get("type").and_then(|v| v.as_str()).unwrap_or("EntityQuery");
    match type_str {
        "EntityQuery" => {
            let filter = parse_entity_filter(node.get("filter"), registry)?;
            Ok(EntityQueryNode::Filter(filter))
        }
        _ => Ok(EntityQueryNode::Filter(EntityFilterNode::All)),
    }
}

fn parse_entity_query_from_id(id: u64, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityQueryNode> {
    let node = registry.get(&id).ok_or_else(|| anyhow::anyhow!("query id {} not found", id))?;
    parse_entity_query_inline(node, registry)
}

fn parse_entity_query_from_elem(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityQueryNode> {
    match val {
        Some(v) => {
            if let Some(id) = v.get("id").and_then(|id| id.as_u64()) {
                parse_entity_query_from_elem_id(id, registry)
            } else if let Some(id) = v.as_u64() {
                parse_entity_query_from_elem_id(id, registry)
            } else {
                parse_entity_query_inline(v, registry)
            }
        }
        None => Ok(EntityQueryNode::Filter(EntityFilterNode::All)),
    }
}

fn parse_entity_query_from_elem_id(id: u64, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityQueryNode> {
    let node = registry.get(&id).ok_or_else(|| anyhow::anyhow!("elem id {} not found", id))?;
    let type_str = node.get("type").and_then(|v| v.as_str()).unwrap_or("");

    match type_str {
        "ElementRef" => {
            // ElementRef has a `query` field pointing to an EntityQuery
            let query = parse_entity_query(node.get("query"), registry)?;
            Ok(query)
        }
        "EntityQuery" => {
            parse_entity_query_inline(node, registry)
        }
        _ => Ok(EntityQueryNode::Filter(EntityFilterNode::All)),
    }
}

fn parse_entity_filter(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityFilterNode> {
    match val {
        Some(v) => {
            if let Some(id) = v.get("id").and_then(|id| id.as_u64()) {
                parse_entity_filter_from_id(id, registry)
            } else if let Some(id) = v.as_u64() {
                parse_entity_filter_from_id(id, registry)
            } else {
                parse_entity_filter_inline(v, registry)
            }
        }
        None => Ok(EntityFilterNode::All),
    }
}

fn parse_entity_filter_inline(node: &serde_json::Value, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityFilterNode> {
    let type_str = node.get("type").and_then(|v| v.as_str()).unwrap_or("All");
    match type_str {
        "FilterById" => {
            let predicate = parse_condition_expr(node.get("predicate"), registry)?;
            Ok(EntityFilterNode::ById {
                predicate: Box::new(predicate),
            })
        }
        "FilterAll" => Ok(EntityFilterNode::All),
        _ => Ok(EntityFilterNode::All),
    }
}

fn parse_entity_filter_from_id(id: u64, registry: &HashMap<u64, serde_json::Value>) -> Result<EntityFilterNode> {
    let node = registry.get(&id).ok_or_else(|| anyhow::anyhow!("filter id {} not found", id))?;
    parse_entity_filter_inline(node, registry)
}

fn parse_condition_expr(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<ConditionExprNode> {
    match val {
        Some(v) => {
            if let Some(id) = v.get("id").and_then(|id| id.as_u64()) {
                parse_condition_expr_from_id(id, registry)
            } else if let Some(id) = v.as_u64() {
                parse_condition_expr_from_id(id, registry)
            } else {
                parse_condition_expr_inline(v, registry)
            }
        }
        None => Ok(ConditionExprNode::Literal(true)),
    }
}

fn parse_condition_expr_inline(node: &serde_json::Value, registry: &HashMap<u64, serde_json::Value>) -> Result<ConditionExprNode> {
    let type_str = node.get("type").and_then(|v| v.as_str()).unwrap_or("Literal");
    match type_str {
        "StringContains" => {
            let haystack = parse_string_expr(node.get("haystack"), registry)?;
            let needle = parse_string_expr(node.get("needle"), registry)?;
            let exact = node.get("exact").and_then(|v| v.as_bool()).unwrap_or(true);
            Ok(ConditionExprNode::StringContains {
                haystack: Box::new(haystack),
                needle: Box::new(needle),
                exact,
            })
        }
        "ConditionAnd" => {
            let left = parse_condition_expr(node.get("left"), registry)?;
            let right = parse_condition_expr(node.get("right"), registry)?;
            Ok(ConditionExprNode::And(Box::new(left), Box::new(right)))
        }
        "ConditionOr" => {
            let left = parse_condition_expr(node.get("left"), registry)?;
            let right = parse_condition_expr(node.get("right"), registry)?;
            Ok(ConditionExprNode::Or(Box::new(left), Box::new(right)))
        }
        "ConditionNegate" => {
            let inner = parse_condition_expr(node.get("inner"), registry)?;
            Ok(ConditionExprNode::Negate(Box::new(inner)))
        }
        "ConditionLiteral" => {
            let val = node.get("value").and_then(|v| v.as_bool()).unwrap_or(true);
            Ok(ConditionExprNode::Literal(val))
        }
        "NumberCompare" => {
            let left = parse_number_expr(node.get("left"), registry)?;
            let right = parse_number_expr(node.get("right"), registry)?;
            let op = parse_number_cmp_op(node.get("op"))?;
            Ok(ConditionExprNode::NumberCompare {
                left: Box::new(left),
                right: Box::new(right),
                op,
            })
        }
        _ => Ok(ConditionExprNode::Literal(true)),
    }
}

fn parse_condition_expr_from_id(id: u64, registry: &HashMap<u64, serde_json::Value>) -> Result<ConditionExprNode> {
    let node = registry.get(&id).ok_or_else(|| anyhow::anyhow!("condition id {} not found", id))?;
    parse_condition_expr_inline(node, registry)
}

fn parse_number_cmp_op(val: Option<&serde_json::Value>) -> Result<NumberCmpOp> {
    match val.and_then(|v| v.as_str()) {
        Some("GreaterThan") | Some("Greater") => Ok(NumberCmpOp::Greater),
        Some("LessThan") | Some("Less") => Ok(NumberCmpOp::Less),
        Some("GreaterEqual") | Some("GreaterOrEqual") => Ok(NumberCmpOp::GreaterEqual),
        Some("LessEqual") | Some("LessOrEqual") => Ok(NumberCmpOp::LessEqual),
        Some("Equal") => Ok(NumberCmpOp::Equal),
        Some("NotEqual") => Ok(NumberCmpOp::NotEqual),
        _ => Ok(NumberCmpOp::Equal),
    }
}

fn parse_text_map(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<Vec<(StringExprNode, StringExprNode)>> {
    let mut pairs = Vec::new();
    if let Some(arr) = val.and_then(|v| v.as_array()) {
        for item in arr {
            if let Some(pair) = item.as_array() {
                if pair.len() >= 2 {
                    let key = parse_string_expr(Some(&pair[0]), registry)?;
                    let value = parse_string_expr(Some(&pair[1]), registry)?;
                    pairs.push((key, value));
                }
            }
        }
    }
    Ok(pairs)
}

fn parse_number_map(val: Option<&serde_json::Value>, registry: &HashMap<u64, serde_json::Value>) -> Result<Vec<(StringExprNode, NumberExprNode)>> {
    let mut pairs = Vec::new();
    if let Some(arr) = val.and_then(|v| v.as_array()) {
        for item in arr {
            if let Some(pair) = item.as_array() {
                if pair.len() >= 2 {
                    let key = parse_string_expr(Some(&pair[0]), registry)?;
                    let value = parse_number_expr(Some(&pair[1]), registry)?;
                    pairs.push((key, value));
                }
            }
        }
    }
    Ok(pairs)
}

// ---- Panel Compilation ----

fn compile_panels(ctx: &Context) -> Result<(Vec<CompiledPanel>, HashMap<u64, serde_json::Value>)> {
    let ast_json = ctx.with(|c| c.eval::<String, _>("__flushAstNodes()"))
        .map_err(|e| anyhow::anyhow!("flush AST nodes error: {}", e))?;
    let ast_nodes: HashMap<u64, serde_json::Value> =
        serde_json::from_str(&ast_json).map_err(|e| anyhow::anyhow!("parse AST nodes error: {}", e))?;

    let panels_json = ctx.with(|c| c.eval::<String, _>(compile_panels_template()))
        .map_err(|e| anyhow::anyhow!("compile panels error: {}", e))?;
    let panels: Vec<serde_json::Value> =
        serde_json::from_str(&panels_json).map_err(|e| anyhow::anyhow!("parse panels error: {}", e))?;

    let mut compiled_panels = Vec::new();
    for panel_val in panels {
        let id = panel_val.get("id").and_then(|v| v.as_str()).unwrap_or("unknown").to_string();
        let anchor = panel_val.get("anchor").and_then(|a| {
            Some(crate::module::compiled_ast::module::CompiledAnchor {
                x: a.get("x").and_then(|v| v.as_f64()).unwrap_or(0.0),
                y: a.get("y").and_then(|v| v.as_f64()).unwrap_or(0.0),
            })
        });
        let offset = panel_val.get("offset").and_then(|o| {
            Some(crate::module::compiled_ast::module::CompiledOffset {
                top: o.get("top").and_then(|v| v.as_f64()).unwrap_or(0.0),
                bottom: o.get("bottom").and_then(|v| v.as_f64()).unwrap_or(0.0),
                left: o.get("left").and_then(|v| v.as_f64()).unwrap_or(0.0),
                right: o.get("right").and_then(|v| v.as_f64()).unwrap_or(0.0),
            })
        });
        let content = parse_compiled_panel_content(panel_val.get("content"), &ast_nodes)?;
        // Preserve raw content JSON for non-EntityNumberValue content types
        let content_json = panel_val.get("content")
            .map(|cv| serde_json::to_string(cv).unwrap_or_default());

        compiled_panels.push(CompiledPanel {
            id,
            anchor,
            offset,
            content,
            content_json,
        });
    }

    Ok((compiled_panels, ast_nodes))
}

fn parse_compiled_panel_content(
    val: Option<&serde_json::Value>,
    _registry: &HashMap<u64, serde_json::Value>,
) -> Result<Option<crate::module::compiled_ast::module::CompiledPanelContent>> {
    match val {
        Some(v) => {
            if let Some(content_obj) = v.get("contentEntityNumberValue") {
                let entity_id = content_obj.get("entityId").and_then(|v| v.as_str()).unwrap_or("").to_string();
                let align = content_obj.get("align").and_then(|v| v.as_str()).unwrap_or("center").to_string();
                let expr_id = content_obj.get("exprId").and_then(|v| v.as_u64()).unwrap_or(0);
                let fallback_id = content_obj.get("fallbackId").and_then(|v| v.as_u64()).unwrap_or(0);
                let fallback = resolve_string_literal(fallback_id, _registry);
                Ok(Some(crate::module::compiled_ast::module::CompiledPanelContent::EntityNumberValue {
                    entity_id,
                    align,
                    expr_id,
                    fallback,
                }))
            } else {
                Ok(None)
            }
        }
        None => Ok(None),
    }
}

fn resolve_string_literal(id: u64, registry: &HashMap<u64, serde_json::Value>) -> String {
    if let Some(node) = registry.get(&id) {
        if node.get("type").map(|v| v.as_str() == Some("StringLiteral")).unwrap_or(false) {
            return node.get("value").and_then(|v| v.as_str()).unwrap_or("").to_string();
        }
    }
    String::new()
}

fn merge_compiled_panels_into_cache(
    compiled_panels: &[CompiledPanel],
    _ast_nodes: &HashMap<u64, serde_json::Value>,
) {
    let mut cache = crate::state::last_panels().lock().unwrap();
    let mut new_panels = Vec::new();

    for panel in compiled_panels {
        // Find original panel JSON in cache to preserve all fields
        let orig_json = cache.iter().find(|p| {
            p.trim_start().starts_with('{') && p.contains(&format!("\"id\"")) && p.contains(&format!("\"{}\"", panel.id))
        });

        let mut panel_obj = if let Some(orig) = orig_json {
            // Start with the original JSON to preserve ALL fields (anchor, offset, etc.)
            serde_json::from_str::<serde_json::Map<String, serde_json::Value>>(orig)
                .unwrap_or_else(|_| serde_json::Map::new())
        } else {
            let mut m = serde_json::Map::new();
            m.insert("id".to_string(), serde_json::Value::String(panel.id.clone()));
            m
        };

        // Ensure id is set
        panel_obj.insert("id".to_string(), serde_json::Value::String(panel.id.clone()));

        // Always ensure anchor/offset/size exist with defaults
        if panel_obj.get("anchor").is_none() {
            let mut anchor_map = serde_json::Map::new();
            if let Some(ref anchor) = panel.anchor {
                anchor_map.insert("x".to_string(), serde_json::Value::Number(serde_json::Number::from_f64(anchor.x).unwrap()));
                anchor_map.insert("y".to_string(), serde_json::Value::Number(serde_json::Number::from_f64(anchor.y).unwrap()));
            } else {
                anchor_map.insert("x".to_string(), serde_json::Value::Number(serde_json::Number::from(0)));
                anchor_map.insert("y".to_string(), serde_json::Value::Number(serde_json::Number::from(0)));
            }
            panel_obj.insert("anchor".to_string(), serde_json::Value::Object(anchor_map));
        }
        if panel_obj.get("offset").is_none() {
            let mut offset_map = serde_json::Map::new();
            if let Some(ref offset) = panel.offset {
                offset_map.insert("top".to_string(), serde_json::Value::Number(serde_json::Number::from_f64(offset.top).unwrap()));
                offset_map.insert("bottom".to_string(), serde_json::Value::Number(serde_json::Number::from_f64(offset.bottom).unwrap()));
                offset_map.insert("left".to_string(), serde_json::Value::Number(serde_json::Number::from_f64(offset.left).unwrap()));
                offset_map.insert("right".to_string(), serde_json::Value::Number(serde_json::Number::from_f64(offset.right).unwrap()));
            } else {
                offset_map.insert("top".to_string(), serde_json::Value::Number(serde_json::Number::from(0)));
                offset_map.insert("bottom".to_string(), serde_json::Value::Number(serde_json::Number::from(0)));
                offset_map.insert("left".to_string(), serde_json::Value::Number(serde_json::Number::from(0)));
                offset_map.insert("right".to_string(), serde_json::Value::Number(serde_json::Number::from(0)));
            }
            panel_obj.insert("offset".to_string(), serde_json::Value::Object(offset_map));
        }
        if panel_obj.get("size").is_none() {
            let mut size_map = serde_json::Map::new();
            size_map.insert("width".to_string(), serde_json::Value::Number(serde_json::Number::from(100)));
            size_map.insert("height".to_string(), serde_json::Value::Number(serde_json::Number::from(100)));
            panel_obj.insert("size".to_string(), serde_json::Value::Object(size_map));
        }

        // Override content only if we have compiled content
        if let Some(content) = &panel.content {
            match content {
                crate::module::compiled_ast::module::CompiledPanelContent::EntityNumberValue { entity_id, align, expr_id, .. } => {
                    let mut content_map = serde_json::Map::new();
                    content_map.insert("type".to_string(), serde_json::Value::String("entityNumberValue".to_string()));
                    content_map.insert("entityId".to_string(), serde_json::Value::String(entity_id.clone()));
                    content_map.insert("align".to_string(), serde_json::Value::String(align.clone()));
                    content_map.insert("astRootId".to_string(), serde_json::Value::Number(serde_json::Number::from(*expr_id)));
                    panel_obj.insert("content".to_string(), serde_json::Value::Object(content_map));
                }
            }
        } else if let Some(ref cj) = panel.content_json {
            // Use raw content JSON for non-EntityNumberValue content (e.g., constant text, entity text value)
            if let Ok(content_val) = serde_json::from_str::<serde_json::Value>(cj) {
                panel_obj.insert("content".to_string(), content_val);
            }
        }

        new_panels.push(serde_json::to_string(&serde_json::Value::Object(panel_obj)).unwrap_or_default());
    }

    let compiled_ids: std::collections::HashSet<&str> = compiled_panels.iter().map(|p| p.id.as_str()).collect();
    cache.retain(|p| {
        if p.trim_start().starts_with('{') {
            !p.split(':').any(|segment| {
                let trimmed = segment.trim();
                if trimmed.starts_with('"') && trimmed.ends_with('"') {
                    let inner = &trimmed[1..trimmed.len()-1];
                    compiled_ids.contains(inner)
                } else {
                    false
                }
            })
        } else {
            !compiled_ids.contains(p.as_str())
        }
    });
    cache.extend(new_panels);
}
