#![allow(dead_code)]
use std::collections::HashMap;
use anyhow::Result;

pub mod number;
pub mod string_eval;
pub mod condition;
pub mod mutation;
pub mod query_eval;

#[derive(Debug, Clone)]
pub struct ExecutionContext {
    pub world_seed: u64,
    pub tick_id: u64,
    pub source_entity_id: String,
    pub action_id: String,
    pub call_index: u32,
}

impl ExecutionContext {
    pub fn new(tick_id: u64, source_entity_id: &str, action_id: &str) -> Self {
        Self {
            world_seed: 0,
            tick_id,
            source_entity_id: source_entity_id.to_string(),
            action_id: action_id.to_string(),
            call_index: 0,
        }
    }
}

#[derive(Debug, Clone)]
pub struct EntityData {
    pub text_map: HashMap<String, String>,
    pub number_map: HashMap<String, i64>,
}

pub struct WorldState<'a> {
    pub entities: &'a HashMap<String, EntityData>,
}

pub trait Evaluate {
    type Output;
    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<Self::Output>;
}

pub struct WriteBuffer {
    pub text_mutations: Vec<(String, String, String)>,
    pub number_mutations: Vec<(String, String, i64)>,
    pub created_entities: Vec<String>,
    pub emitted_effects: Vec<String>,
    pub logs: Vec<String>,
}

impl WriteBuffer {
    pub fn new() -> Self {
        Self {
            text_mutations: Vec::new(),
            number_mutations: Vec::new(),
            created_entities: Vec::new(),
            emitted_effects: Vec::new(),
            logs: Vec::new(),
        }
    }

    pub fn set_text(&mut self, entity_id: String, key: String, value: String) {
        self.text_mutations.push((entity_id, key, value));
    }

    pub fn set_number(&mut self, entity_id: String, key: String, value: i64) {
        self.number_mutations.push((entity_id, key, value));
    }

    pub fn create_entity(&mut self, entity_id: String) {
        self.created_entities.push(entity_id);
    }

    pub fn emit_effect(&mut self, effect_name: String) {
        self.emitted_effects.push(effect_name);
    }

    pub fn log(&mut self, message: String) {
        self.logs.push(message);
    }

    pub fn commit(self) -> Result<()> {
        Ok(())
    }
}
