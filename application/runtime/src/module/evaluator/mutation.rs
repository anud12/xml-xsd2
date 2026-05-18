#![allow(dead_code)]
use anyhow::Result;
use crate::module::compiled_ast::mutation::MutationNode;
use super::{Evaluate, ExecutionContext, WorldState, WriteBuffer};

pub fn apply_mutation(
    mutation: &MutationNode,
    ctx: &ExecutionContext,
    world: &WorldState,
    write_buffer: &mut WriteBuffer,
) -> Result<()> {
    match mutation {
        MutationNode::SumNumber { entity, key, addend } => {
            let target_ids = entity.evaluate(ctx, world)?;
            let key_val = key.evaluate(ctx, world)?;
            let delta = addend.evaluate(ctx, world)?;
            for entity_id in target_ids {
                let current = world.entities.get(&entity_id)
                    .and_then(|e| e.number_map.get(&key_val)).copied().unwrap_or(0);
                write_buffer.set_number(entity_id.clone(), key_val.clone(), current + delta);
            }
        }
        MutationNode::SetTextMapValue { entity, key, value } => {
            let target_ids = entity.evaluate(ctx, world)?;
            let key_val = key.evaluate(ctx, world)?;
            let val = value.evaluate(ctx, world)?;
            for entity_id in target_ids {
                write_buffer.set_text(entity_id.clone(), key_val.clone(), val.clone());
            }
        }
        MutationNode::ConcatText { entity, key, suffix } => {
            let target_ids = entity.evaluate(ctx, world)?;
            let key_val = key.evaluate(ctx, world)?;
            let suffix_val = suffix.evaluate(ctx, world)?;
            for entity_id in target_ids {
                let current = world.entities.get(&entity_id)
                    .and_then(|e| e.text_map.get(&key_val)).cloned().unwrap_or_default();
                write_buffer.set_text(entity_id.clone(), key_val.clone(), format!("{}{}", current, suffix_val));
            }
        }
        MutationNode::CreateEntity { .. } => {
            // placeholder
        }
        MutationNode::EmitEvent { event_name, .. } => {
            let name = event_name.evaluate(ctx, world)?;
            write_buffer.emit_effect(name);
        }
        MutationNode::Log { message } => {
            let msg = message.evaluate(ctx, world)?;
            write_buffer.log(msg);
        }
        MutationNode::SetNumberMapValue { entity, key, value } => {
            let target_ids = entity.evaluate(ctx, world)?;
            let key_val = key.evaluate(ctx, world)?;
            let val = value.evaluate(ctx, world)?;
            for entity_id in target_ids {
                write_buffer.set_number(entity_id.clone(), key_val.clone(), val);
            }
        }
    }
    Ok(())
}
