#![allow(dead_code)]
use anyhow::Result;
use crate::module::compiled_ast::expr::{ConditionExprNode, NumberCmpOp};
use super::{Evaluate, ExecutionContext, WorldState};

impl Evaluate for ConditionExprNode {
    type Output = bool;

    fn evaluate(&self, ctx: &ExecutionContext, world: &WorldState) -> Result<bool> {
        match self {
            ConditionExprNode::Literal(b) => Ok(*b),
            ConditionExprNode::And(a, b) => {
                let left = a.evaluate(ctx, world)?;
                if !left {
                    return Ok(false);
                }
                b.evaluate(ctx, world)
            }
            ConditionExprNode::Or(a, b) => {
                let left = a.evaluate(ctx, world)?;
                if left {
                    return Ok(true);
                }
                b.evaluate(ctx, world)
            }
            ConditionExprNode::Negate(inner) => {
                let val = inner.evaluate(ctx, world)?;
                Ok(!val)
            }
            ConditionExprNode::StringContains { haystack, needle, exact } => {
                let hay = haystack.evaluate(ctx, world)?;
                let nld = needle.evaluate(ctx, world)?;
                if *exact {
                    Ok(hay == nld)
                } else {
                    Ok(hay.contains(&nld))
                }
            }
            ConditionExprNode::NumberCompare { left, right, op } => {
                let l = left.evaluate(ctx, world)?;
                let r = right.evaluate(ctx, world)?;
                match op {
                    NumberCmpOp::Greater => Ok(l > r),
                    NumberCmpOp::Less => Ok(l < r),
                    NumberCmpOp::GreaterEqual => Ok(l >= r),
                    NumberCmpOp::LessEqual => Ok(l <= r),
                    NumberCmpOp::Equal => Ok(l == r),
                    NumberCmpOp::NotEqual => Ok(l != r),
                }
            }
            ConditionExprNode::IfTrue { condition, then } => {
                let cond_val = condition.evaluate(ctx, world)?;
                if cond_val {
                    then.evaluate(ctx, world)
                } else {
                    Ok(false)
                }
            }
            ConditionExprNode::IfFalse { condition, then } => {
                let cond_val = condition.evaluate(ctx, world)?;
                if !cond_val {
                    then.evaluate(ctx, world)
                } else {
                    Ok(false)
                }
            }
        }
    }
}
