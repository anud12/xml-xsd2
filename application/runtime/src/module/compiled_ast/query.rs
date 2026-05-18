#![allow(dead_code)]
use serde::{Serialize, Deserialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum EntityQueryNode {
    Filter(EntityFilterNode),
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum EntityFilterNode {
    All,
    ById { predicate: Box<super::expr::ConditionExprNode> },
    HasTextValue { key: Box<super::expr::StringExprNode>, predicate: Box<super::expr::ConditionExprNode> },
    HasNumberValue { key: Box<super::expr::StringExprNode>, predicate: Box<super::expr::ConditionExprNode> },
    HasContainer(Box<EntityQueryNode>),
    Not(Box<EntityFilterNode>),
    And(Vec<EntityFilterNode>),
    Or(Vec<EntityFilterNode>),
}
