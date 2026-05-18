#![allow(dead_code)]
use anyhow::Result;
use crate::module::compiled_ast::query::{EntityQueryNode, EntityFilterNode};
use crate::module::compiled_ast::expr::ConditionExprNode;
use super::{Evaluate, ExecutionContext, WorldState};

impl Evaluate for EntityQueryNode {
    type Output = Vec<String>;

    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<Vec<String>> {
        match self {
            EntityQueryNode::Filter(filter) => filter.evaluate(ctx, world),
        }
    }
}

impl Evaluate for EntityFilterNode {
    type Output = Vec<String>;

    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<Vec<String>> {
        match self {
            EntityFilterNode::All => {
                Ok(world.entities.keys().cloned().collect())
            }
            EntityFilterNode::ById { predicate } => {
                let mut matched = Vec::new();
                // Extract target ID(s) from predicate and match entities
                let target_ids: Vec<String> = match predicate.as_ref() {
                    ConditionExprNode::StringContains { needle, .. } => {
                        needle.evaluate(ctx, world).map(|s| vec![s]).unwrap_or_default()
                    }
                    ConditionExprNode::Literal(true) => {
                        world.entities.keys().cloned().collect()
                    }
                    _ => Vec::new(),
                };
                for (id, _) in world.entities {
                    if target_ids.iter().any(|t| id == t || id.contains(t)) {
                        matched.push(id.clone());
                    }
                }
                Ok(matched)
            }
            EntityFilterNode::HasTextValue { key, predicate } => {
                let key_val = key.evaluate(ctx, world)?;
                let mut matched = Vec::new();
                for (id, data) in world.entities {
                    if data.text_map.contains_key(&key_val) {
                        if let ConditionExprNode::Literal(true) = &**predicate {
                            matched.push(id.clone());
                        }
                    }
                }
                Ok(matched)
            }
            EntityFilterNode::HasNumberValue { key, predicate } => {
                let key_val = key.evaluate(ctx, world)?;
                let mut matched = Vec::new();
                for (id, data) in world.entities {
                    if data.number_map.contains_key(&key_val) {
                        if let ConditionExprNode::Literal(true) = &**predicate {
                            matched.push(id.clone());
                        }
                    }
                }
                Ok(matched)
            }
            EntityFilterNode::HasContainer(inner) => {
                inner.evaluate(ctx, world)
            }
            EntityFilterNode::Not(inner) => {
                let inner_ids = inner.evaluate(ctx, world)?;
                Ok(world.entities.keys()
                    .filter(|k| !inner_ids.contains(*k))
                    .cloned()
                    .collect())
            }
            EntityFilterNode::And(filters) => {
                if filters.is_empty() {
                    return Ok(world.entities.keys().cloned().collect());
                }
                let mut result: Vec<String> = filters[0].evaluate(ctx, world)?;
                for filter in filters.iter().skip(1) {
                    let subset = filter.evaluate(ctx, world)?;
                    result.retain(|id| subset.contains(id));
                }
                Ok(result)
            }
            EntityFilterNode::Or(filters) => {
                let mut result = Vec::new();
                for filter in filters {
                    let subset = filter.evaluate(ctx, world)?;
                    for id in subset {
                        if !result.contains(&id) {
                            result.push(id);
                        }
                    }
                }
                Ok(result)
            }
        }
    }
}
