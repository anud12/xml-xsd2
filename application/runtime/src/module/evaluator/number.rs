#![allow(dead_code)]
use anyhow::Result;
use crate::module::compiled_ast::expr::NumberExprNode;
use super::{Evaluate, ExecutionContext, WorldState};

impl Evaluate for NumberExprNode {
    type Output = i64;

    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<i64> {
        match self {
            NumberExprNode::Literal(v) => Ok(*v),
            NumberExprNode::Sum(a, b) => {
                let left = a.evaluate(ctx, world)?;
                let right = b.evaluate(ctx, world)?;
                Ok(left + right)
            }
            NumberExprNode::Subtract(a, b) => {
                let left = a.evaluate(ctx, world)?;
                let right = b.evaluate(ctx, world)?;
                Ok(left - right)
            }
            NumberExprNode::Multiply(a, b) => {
                let left = a.evaluate(ctx, world)?;
                let right = b.evaluate(ctx, world)?;
                Ok(left * right)
            }
            NumberExprNode::Divide(a, b) => {
                let left = a.evaluate(ctx, world)?;
                let right = b.evaluate(ctx, world)?;
                if right == 0 {
                    Ok(0)
                } else {
                    Ok(left / right)
                }
            }
            NumberExprNode::Random(_, _) => {
                Ok(0)
            }
            NumberExprNode::EntityRef { entity_query, key } => {
                let entity_ids = entity_query.evaluate(ctx, world)?;
                let key_val = key.evaluate(ctx, world)?;
                for eid in &entity_ids {
                    if let Some(entity_data) = world.entities.get(eid) {
                        if let Some(&val) = entity_data.number_map.get(&key_val) {
                            return Ok(val);
                        }
                    }
                }
                Ok(0)
            }
            NumberExprNode::RuleRef(_) => {
                Ok(0)
            }
        }
    }
}
