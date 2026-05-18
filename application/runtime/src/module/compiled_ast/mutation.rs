#![allow(dead_code)]
use serde::{Serialize, Deserialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum MutationNode {
    SetTextMapValue { entity: super::query::EntityQueryNode, key: super::expr::StringExprNode, value: super::expr::StringExprNode },
    SetNumberMapValue { entity: super::query::EntityQueryNode, key: super::expr::StringExprNode, value: super::expr::NumberExprNode },
    ConcatText { entity: super::query::EntityQueryNode, key: super::expr::StringExprNode, suffix: super::expr::StringExprNode },
    SumNumber { entity: super::query::EntityQueryNode, key: super::expr::StringExprNode, addend: super::expr::NumberExprNode },
    CreateEntity { text_map: Vec<(super::expr::StringExprNode, super::expr::StringExprNode)>, number_map: Vec<(super::expr::StringExprNode, super::expr::NumberExprNode)> },
    EmitEvent { event_name: super::expr::StringExprNode, payload: serde_json::Value },
    Log { message: super::expr::StringExprNode },
}
