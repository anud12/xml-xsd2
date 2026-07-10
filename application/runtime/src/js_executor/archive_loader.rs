use rquickjs::{Ctx, Error, loader::Resolver, Module};

/// Resolves module specifiers against the archive's files map stored in global state.
pub struct ArchiveResolver;

impl Resolver for ArchiveResolver {
    fn resolve<'js>(
        &mut self,
        _ctx: &Ctx<'js>,
        base: &str,
        name: &str,
    ) -> Result<String, Error> {
        if !name.starts_with('.') {
            return Ok(name.to_string());
        }

        let files = crate::state::archive_files().lock().unwrap();

        let base_dir = if let Some(last_slash) = base.rfind('/') {
            &base[..last_slash]
        } else {
            ""
        };

        let mut path = if base_dir.is_empty() {
            name.to_string()
        } else {
            format!("{}/{}", base_dir, name)
        };

        path = normalize_path(&path);

        if files.contains_key(&path) {
            return Ok(path);
        }

        let with_ext = format!("{}.js", path);
        if files.contains_key(&with_ext) {
            return Ok(with_ext);
        }

        Err(Error::new_resolving(base, name))
    }
}

fn normalize_path(path: &str) -> String {
    let mut parts = Vec::new();
    for segment in path.split('/') {
        match segment {
            ".." => { parts.pop(); }
            "." | "" => {}
            _ => { parts.push(segment); }
        }
    }
    parts.join("/")
}

/// Loads module source from the archive's files map stored in global state.
pub struct ArchiveLoader;

impl rquickjs::loader::Loader for ArchiveLoader {
    fn load<'js>(
        &mut self,
        ctx: &Ctx<'js>,
        name: &str,
    ) -> Result<Module<'js>, Error> {
        let files = crate::state::archive_files().lock().unwrap();

        let source = files.get(name)
            .ok_or_else(|| Error::new_loading(name))?;

        Module::declare(ctx.clone(), name, source.clone())
    }
}
