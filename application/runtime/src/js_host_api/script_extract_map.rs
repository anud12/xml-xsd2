pub(super) fn extract_map_items() -> &'static str {
    r#"
        const re = globalThis.__registeredEvents || [];
        out.events = re.map(ev => {
            if (typeof ev === 'string') return ev;
            if (ev && typeof ev === 'object') {
                if (typeof ev.name === 'string')
                    return ev.name;
                if (ev.apply && typeof ev.apply
                    === 'function' && ev.apply.name)
                    return ev.apply.name;
                try { return JSON.stringify(ev); }
                catch(e) { return String(ev); }
            }
            return String(ev);
        });
        const ra = globalThis.__registeredActions || [];
        out.actions = ra.map(ev => {
            if (typeof ev === 'string') return ev;
            if (ev && typeof ev === 'object') {
                if (typeof ev.name === 'string')
                    return ev.name;
                if (ev.apply && typeof ev.apply
                    === 'function' && ev.apply.name)
                    return ev.apply.name;
                try { return JSON.stringify(ev); }
                catch(e) { return String(ev); }
            }
            return String(ev);
        });
        const ce = globalThis.__createdEntities || [];
        out.entities = ce.map(en => {
            if (typeof en === 'string') return en;
            if (en && typeof en === 'object') {
                if (typeof en.firstName === 'string')
                    return en.firstName;
                try { return JSON.stringify(en); }
                catch(e) { return String(en); }
            }
            return String(en);
        });"#
}
