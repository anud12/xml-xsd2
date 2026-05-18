#![allow(dead_code)]
use serde::{Serialize, Deserialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum NumberExprNode {
    Literal(i64),
    Sum(Box<NumberExprNode>, Box<NumberExprNode>),
    Subtract(Box<NumberExprNode>, Box<NumberExprNode>),
    Multiply(Box<NumberExprNode>, Box<NumberExprNode>),
    Divide(Box<NumberExprNode>, Box<NumberExprNode>),
    Random(Box<NumberExprNode>, Box<NumberExprNode>),
    EntityRef { entity_query: Box<super::query::EntityQueryNode>, key: Box<StringExprNode> },
    RuleRef(String),
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum StringExprNode {
    Literal(String),
    Concat(Box<StringExprNode>, Box<StringExprNode>),
    Join(Vec<StringExprNode>, Option<Box<StringExprNode>>),
    OneOf(Vec<StringExprNode>),
    EntityRef { entity_query: Box<super::query::EntityQueryNode>, key: Box<StringExprNode> },
    RuleRef(String),
}

#[derive(Debug, Clone, Serialize, Deserialize)]
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

#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
pub enum NumberCmpOp {
    Greater,
    Less,
    GreaterEqual,
    LessEqual,
    Equal,
    NotEqual,
}
