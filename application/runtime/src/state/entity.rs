use serde::{Deserialize, Serialize};
use std::collections::{BTreeMap, HashMap};
use std::sync::{LazyLock, Mutex};

pub type EntityMap = HashMap<String, Entity>;

#[derive(Clone, Debug, Default, Deserialize, Serialize)]
#[serde(default, rename_all = "camelCase")]
pub struct Entity {
    pub id: String,
    pub number_map: HashMap<String, f64>,
    pub string_map: HashMap<String, String>,
    pub polygons: BTreeMap<String, Vec<(f64, f64)>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub entity_list_map: Option<BTreeMap<String, Vec<String>>>,
}

static ENTITIES: LazyLock<Mutex<EntityMap>> = LazyLock::new(Mutex::default);

pub fn entity_map() -> &'static Mutex<EntityMap> {
    &ENTITIES
}

pub fn ensure_entity<'a>(entities: &'a mut EntityMap, id: &str) -> &'a mut Entity {
    entities.entry(id.to_owned()).or_insert_with(|| Entity {
        id: id.to_owned(),
        ..Entity::default()
    })
}

#[cfg(test)]
pub fn seed_group(id: &str, list: &str, members: &[(&str, f64, f64)]) {
    let mut entities = entity_map().lock().unwrap();
    for (member, x, y) in members {
        ensure_entity(&mut entities, member).number_map.extend([
            ("x".into(), *x), ("y".into(), *y),
        ]);
    }
    ensure_entity(&mut entities, id).entity_list_map.get_or_insert_with(BTreeMap::new)
        .insert(list.into(), members.iter().map(|(id, _, _)| id.to_string()).collect());
}

impl Entity {
    pub fn normalize(&mut self) {
        if let Some(lists) = &mut self.entity_list_map {
            for members in lists.values_mut() {
                let mut seen = std::collections::HashSet::new();
                members.retain(|id| !id.is_empty() && seen.insert(id.clone()));
            }
        }
        self.polygons.retain(|_, points| points.len() >= 3);
    }

    pub fn log_dangling(&self, entities: &EntityMap) {
        if let Some(lists) = &self.entity_list_map {
            for (list, members) in lists {
                for id in members {
                    if id != &self.id && !entities.contains_key(id) {
                        runtime_log!("group {:?} list {:?}: dangling member {:?}", self.id, list, id);
                    }
                }
            }
        }
    }

    pub fn position_keys(&self) -> (&str, &str) {
        let key = |name, default| self.string_map.get(name)
            .map(String::as_str).filter(|key| !key.is_empty()).unwrap_or(default);
        (key("xKey", "x"), key("yKey", "y"))
    }

    pub fn position(&self) -> (f64, f64) {
        let (x, y) = self.position_keys();
        (self.number_map.get(x).copied().unwrap_or(0.0),
         self.number_map.get(y).copied().unwrap_or(0.0))
    }

    pub fn number(&self, key: &str) -> Option<f64> {
        self.number_map.get(key).copied()
    }

    pub fn members(&self, list: &str) -> Option<&[String]> {
        self.entity_list_map.as_ref()?.get(list).map(Vec::as_slice)
    }
}
