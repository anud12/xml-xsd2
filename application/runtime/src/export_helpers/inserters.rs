use rusqlite::Connection;

pub fn insert_actions(mem_conn: &mut Connection, actions: &Vec<Vec<String>>) {
    if actions.is_empty() { return; }
    let tx = mem_conn.transaction().expect("tx_actions");
    for row in actions.iter() {
        tx.execute("INSERT INTO action (name) VALUES (?1)", &[&row[0]])
            .ok();
    }
    tx.commit().ok();
}

pub fn insert_events(mem_conn: &mut Connection, events: &Vec<Vec<String>>) {
    if events.is_empty() { return; }
    let tx = mem_conn.transaction().expect("tx_events");
    for row in events.iter() {
        let val = row.get(0).map(|s| s.as_str()).unwrap_or("");
        let norm = val.replace("effect", "event");
        tx.execute("INSERT INTO events (name) VALUES (?1)", &[&norm])
            .ok();
    }
    tx.commit().ok();
}

pub fn insert_entities(mem_conn: &mut Connection, entities: &Vec<Vec<String>>) {
    if entities.is_empty() { return; }
    let tx = mem_conn.transaction().expect("tx2");
    for row in entities.iter() {
        tx.execute(
            "INSERT INTO entity (textMap_name) VALUES (?1)",
            &[&row[0]],
        )
        .ok();
    }
    tx.commit().ok();
}

pub fn insert_panels(mem_conn: &mut Connection, panels: &Vec<String>) {
    if panels.is_empty() { return; }
    let txp = mem_conn.transaction().expect("tx_panels");
    for p in panels.iter() {
        let panel_id = extract_panel_id(p);
        txp.execute("INSERT INTO panel (id) VALUES (?1)", &[&panel_id])
            .ok();
    }
    txp.commit().ok();
}

fn extract_panel_id(p: &str) -> String {
    if p.trim_start().starts_with('{') {
        if let Ok(v) = serde_json::from_str::<serde_json::Value>(p) {
            v.get("id")
                .and_then(|x| x.as_str())
                .unwrap_or(p)
                .to_string()
        } else {
            p.to_string()
        }
    } else {
        p.to_string()
    }
}
