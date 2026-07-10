//! Helpers for stripping export prefixes from bundled module sources.
//! Preserves `export default` for later transform, removes named exports.

pub fn strip_export_prefix(line: &str) -> String {
    let trimmed = line.trim_start();
    if !trimmed.starts_with("export ") {
        return line.to_string();
    }
    let indent = &line[..line.len() - trimmed.len()];
    let rest = &trimmed[7..];
    if rest.starts_with("default ") {
        return format!("{}export default {}", indent, &rest[8..]);
    }
    if rest.starts_with("default\n") || rest == "default" {
        return format!("{}export default", indent);
    }
    format!("{}{}", indent, rest)
}
