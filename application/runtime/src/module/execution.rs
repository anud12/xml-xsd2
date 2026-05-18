#![allow(dead_code)]
use crate::module::compiled_ast::module::CompiledModule;
use crate::module::evaluator::*;
use anyhow::Result;
use std::collections::HashMap;

pub struct ExecutionResult {
    pub created_entities: Vec<String>,
    pub emitted_effects: Vec<String>,
    pub logs: Vec<String>,
    pub text_mutations: Vec<(String, String, String)>,
    pub number_mutations: Vec<(String, String, i64)>,
}

fn build_world_entities(
    entities: &HashMap<String, HashMap<String, String>>,
    number_entities: &HashMap<String, HashMap<String, i64>>,
) -> HashMap<String, EntityData> {
    let mut world_entities = HashMap::new();
    for (id, text_map) in entities {
        let number_map = number_entities.get(id).cloned().unwrap_or_default();
        world_entities.insert(id.clone(), EntityData {
            text_map: text_map.clone(),
            number_map,
        });
    }
    for (id, number_map) in number_entities {
        if !world_entities.contains_key(id) {
            world_entities.insert(id.clone(), EntityData {
                text_map: HashMap::new(),
                number_map: number_map.clone(),
            });
        }
    }
    world_entities
}

pub fn execute_action(
    compiled: &CompiledModule,
    action_name: &str,
    tick_id: u64,
    source_entity_id: &str,
    entities: &HashMap<String, HashMap<String, String>>,
    number_entities: &HashMap<String, HashMap<String, i64>>,
) -> Result<ExecutionResult> {
    let action = compiled.actions.iter()
        .find(|a| a.name == action_name)
        .ok_or_else(|| anyhow::anyhow!("action not found: {}", action_name))?;

    let ctx = ExecutionContext::new(tick_id, source_entity_id, &action.name);
    let world_entities = build_world_entities(entities, number_entities);
    let world = WorldState { entities: &world_entities };
    let mut write_buffer = WriteBuffer::new();

    // Evaluate apply mutations
    for mutation in &action.apply {
        crate::module::evaluator::mutation::apply_mutation(mutation, &ctx, &world, &mut write_buffer)?;
    }

    // Commit returns Ok(()) and consumes the buffer, so extract fields first
    let created_entities = write_buffer.created_entities.clone();
    let emitted_effects = write_buffer.emitted_effects.clone();
    let logs = write_buffer.logs.clone();
    let text_mutations = write_buffer.text_mutations.clone();
    let number_mutations = write_buffer.number_mutations.clone();
    write_buffer.commit()?;

    Ok(ExecutionResult {
        created_entities,
        emitted_effects,
        logs,
        text_mutations,
        number_mutations,
    })
}

pub fn execute_effect(
    compiled: &CompiledModule,
    effect_name: &str,
    tick_id: u64,
    source_entity_id: &str,
    entities: &HashMap<String, HashMap<String, String>>,
    number_entities: &HashMap<String, HashMap<String, i64>>,
) -> Result<ExecutionResult> {
    let effect = compiled.effects.iter()
        .find(|e| e.name == effect_name)
        .ok_or_else(|| anyhow::anyhow!("effect not found: {}", effect_name))?;

    let ctx = ExecutionContext::new(tick_id, source_entity_id, &effect.name);
    let world_entities = build_world_entities(entities, number_entities);
    let world = WorldState { entities: &world_entities };
    let mut write_buffer = WriteBuffer::new();

    // Evaluate prepare mutations for effect (read-only phase)
    for mutation in &effect.prepare {
        crate::module::evaluator::mutation::apply_mutation(mutation, &ctx, &world, &mut write_buffer)?;
    }

    // Evaluate apply mutations for effect (mutation phase)
    for mutation in &effect.apply {
        crate::module::evaluator::mutation::apply_mutation(mutation, &ctx, &world, &mut write_buffer)?;
    }

    let created_entities = write_buffer.created_entities.clone();
    let emitted_effects = write_buffer.emitted_effects.clone();
    let logs = write_buffer.logs.clone();
    let text_mutations = write_buffer.text_mutations.clone();
    let number_mutations = write_buffer.number_mutations.clone();
    write_buffer.commit()?;

    Ok(ExecutionResult {
        created_entities,
        emitted_effects,
        logs,
        text_mutations,
        number_mutations,
    })
}
