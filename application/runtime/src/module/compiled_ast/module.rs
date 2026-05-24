#![allow(dead_code)]
use std::collections::HashMap;
use serde::{Serialize, Deserialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompiledModule {
    pub actions: Vec<CompiledAction>,
    pub effects: Vec<CompiledEffect>,
    pub entities: Vec<CompiledEntity>,
    pub panels: Vec<CompiledPanel>,
    pub created_by: HashMap<String, Vec<String>>,
    pub emits_map: HashMap<String, Vec<String>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompiledAction {
    pub name: String,
    pub prepare: Vec<super::mutation::MutationNode>,
    pub apply: Vec<super::mutation::MutationNode>,
    pub guard: Option<super::expr::ConditionExprNode>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompiledEffect {
    pub name: String,
    pub prepare: Vec<super::mutation::MutationNode>,
    pub apply: Vec<super::mutation::MutationNode>,
    pub reoccur_after_ms: Option<super::expr::NumberExprNode>,
    pub is_reoccurrence_applicable: Option<super::expr::ConditionExprNode>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompiledEntity {
    pub id: String,
    pub text_map: HashMap<String, String>,
    pub number_map: HashMap<String, i64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompiledPanel {
    pub id: String,
    pub anchor: Option<CompiledAnchor>,
    pub offset: Option<CompiledOffset>,
    pub content: Option<CompiledPanelContent>,
    /// Raw content JSON from the compiled panel (preserves constant text, entity text value, etc.)
    pub content_json: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompiledAnchor {
    pub x: f64,
    pub y: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompiledOffset {
    pub top: f64,
    pub bottom: f64,
    pub left: f64,
    pub right: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(tag = "contentEntityNumberValue", rename_all = "camelCase")]
pub enum CompiledPanelContent {
    EntityNumberValue {
        entity_id: String,
        align: String,
        expr_id: u64,
        fallback: String,
    },
}
