//! Resolves import specifiers to archive file paths and fetches content.
//! Returns (resolved_path, file_content) or None if not found.

pub fn resolve_and_fetch(
    dir: &str,
    line: &str,
) -> Option<(String, String)> {
    let spec = extract_specifier(line)?;
    if !spec.starts_with('.') { return None; }
    let files = crate::state::archive_files().lock().unwrap();
    let raw = if dir.is_empty() {
        spec.clone()
    } else {
        format!("{}/{}", dir, spec)
    };
    let normalized = normalize_path(&raw);
    if let Some(content) = files.get(&normalized) {
        return Some((normalized, content.clone()));
    }
    let with_ext = format!("{}.js", normalized);
    if let Some(content) = files.get(&with_ext) {
        return Some((with_ext, content.clone()));
    }
    None
}

fn extract_specifier(line: &str) -> Option<String> {
    let mut in_q = false;
    let mut quote = '\0';
    let mut start = None;
    for (i, ch) in line.char_indices().rev() {
        if !in_q && (ch == '\'' || ch == '"') {
            in_q = true;
            quote = ch;
        } else if in_q && ch == quote {
            start = Some(i);
            break;
        }
    }
    start.map(|s| {
        let end = line.len() - s;
        line[line.len() - end..line.len() - 1]
            .trim_matches(|c| c == '\'' || c == '"').to_string()
    })
}

fn normalize_path(path: &str) -> String {
    let mut parts = Vec::new();
    for seg in path.split('/') {
        match seg {
            ".." => { parts.pop(); }
            "." | "" => {}
            _ => { parts.push(seg); }
        }
    }
    parts.join("/")
}
