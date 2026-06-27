pub fn collect_panels_fallback(panels: &mut Vec<String>) {
    if !panels.is_empty() { return; }
    panels_from_csv(panels);
    if !panels.is_empty() { return; }
    panels_from_modules(panels);
    if !panels.is_empty() { return; }
    panels_from_js(panels);
}

fn panels_from_csv(panels: &mut Vec<String>) {
    for row in crate::state::last_file_rows().lock().unwrap().iter() {
        if row.len() < 2 { continue; }
        let fname = row[0].to_lowercase();
        if !(fname.contains("panel") && fname.contains(".csv")) { continue; }
        for line in row[1].lines() {
            let t = line.trim();
            if t.is_empty() { continue; }
            let f = t.split(',').next().unwrap().trim_matches('"');
            if f.eq_ignore_ascii_case("id") || f.is_empty() { continue; }
            panels.push(f.to_string());
        }
    }
}

fn panels_from_modules(panels: &mut Vec<String>) {
    if let Some(row) = crate::state::last_module_rows().lock().unwrap().get(0) {
        let id = row.get(0).cloned().unwrap_or_default();
        let name = row.get(1).cloned().unwrap_or_default();
        let chosen = if !id.is_empty() { id } else { name };
        if !chosen.is_empty() { panels.push(chosen); }
    }
}

fn panels_from_js(panels: &mut Vec<String>) {
    for row in crate::state::last_file_rows().lock().unwrap().iter() {
        if row.len() < 2 || !row[0].to_lowercase().ends_with(".js") { continue; }
        let src = &row[1];
        for (start, _) in src.match_indices("registerPanel(") {
            let s = start + 16;
            if let Some(rest) = src.get(s..) {
                if let Some(e) = rest.find(')') {
                    let arg = &rest[..e];
                    for q in ['"', '\''] {
                        if let Some(fs) = arg.find(q) {
                            if let Some(fe) = arg[fs+1..].find(q) {
                                let v = &arg[fs+1..fs+1+fe];
                                if !v.is_empty() { panels.push(v.to_string()); }
                            }
                        }
                    }
                    if let Some(p) = arg.find("id") {
                        if let Some(c) = arg[p..].find(':') {
                            let v = arg[p+c+1..].trim()
                                .trim_matches(|ch| matches!(ch, ' ' | '"' | '\'' | '}'));
                            if !v.is_empty() { panels.push(v.to_string()); }
                        }
                    }
                }
            }
        }
        for q in ['"', '\''] {
            for (i, _) in src.match_indices(q) {
                if let Some(rest) = src.get(i+1..) {
                    if let Some(e) = rest.find(q) {
                        let v = &rest[..e];
                        if v.to_lowercase().contains("panel") {
                            panels.push(v.to_string());
                        }
                    }
                }
            }
        }
    }
}
