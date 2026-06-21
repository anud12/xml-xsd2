pub(crate) fn extract_json_str(
    s: &str, key: &str
) -> Option<String> {
    let pos = s.find(key)?;
    let colon = s[pos..].find(':')?;
    let after = &s[pos + colon + 1..];
    let mut v = after.trim_start();
    if v.starts_with('"') {
        v = &v[1..];
        if let Some(end) = v.find('"') {
            return Some(v[..end].to_string());
        }
    } else {
        let mut end = v.len();
        if let Some(c) = v.find(',') { end = c.min(end); }
        if let Some(c) = v.find('}') { end = c.min(end); }
        return Some(v[..end].trim().to_string());
    }
    None
}

pub(crate) fn lookup_panel(
    panels: &[String], id_str: &str,
) -> Option<(String, Option<String>)> {
    for p in panels.iter() {
        if p.trim_start().starts_with('{') {
            if let Some(pid) = extract_json_str(p, "\"id\"") {
                if pid == id_str {
                    let bg = extract_json_str(p, "\"background\"");
                    let bg_clean = bg.filter(|s| !s.is_empty());
                    return Some((p.clone(), bg_clean));
                }
            }
        } else if p == id_str {
            let json = format!(
                "{{\"id\":\"{}\",\"background\":null}}",
                id_str
            );
            return Some((json, None));
        }
    }
    None
}
