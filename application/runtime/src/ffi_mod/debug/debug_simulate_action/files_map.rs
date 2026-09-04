use std::collections::HashMap;

pub fn build_files_map() -> HashMap<String, String> {
    let file_rows = crate::state::last_file_rows()
        .lock().unwrap().clone();

    let mut map: HashMap<String, String> = HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            map.insert(r[0].clone(), r[1].clone());
        }
    }
    map
}
