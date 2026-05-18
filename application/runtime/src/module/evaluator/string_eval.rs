#![allow(dead_code)]
use anyhow::Result;
use crate::module::compiled_ast::expr::StringExprNode;
use super::{Evaluate, ExecutionContext, WorldState};

impl Evaluate for StringExprNode {
    type Output = String;

    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<String> {
        match self {
            StringExprNode::Literal(s) => Ok(s.clone()),
            StringExprNode::Concat(a, b) => {
                let left = a.evaluate(ctx, world)?;
                let right = b.evaluate(ctx, world)?;
                Ok(format!("{}{}", left, right))
            }
            StringExprNode::Join(parts, separator) => {
                let evaluated: Vec<String> = parts
                    .iter()
                    .map(|p| p.evaluate(ctx, world))
                    .collect::<Result<Vec<_>>>()?;
                let sep = match separator {
                    Some(s) => s.evaluate(ctx, world)?,
                    None => String::new(),
                };
                Ok(evaluated.join(&sep))
            }
            StringExprNode::OneOf(options) => {
                if let Some(first) = options.first() {
                    first.evaluate(ctx, world)
                } else {
                    Ok(String::new())
                }
            }
            StringExprNode::EntityRef { entity_query, key } => {
                let entity_ids = entity_query.evaluate(ctx, world)?;
                let key_val = key.evaluate(ctx, world)?;
                for eid in &entity_ids {
                    if let Some(entity_data) = world.entities.get(eid) {
                        if let Some(val) = entity_data.text_map.get(&key_val) {
                            return Ok(val.clone());
                        }
                    }
                }
                Ok(String::new())
            }
            StringExprNode::RuleRef(_) => {
                Ok(String::new())
            }
        }
    }
}
