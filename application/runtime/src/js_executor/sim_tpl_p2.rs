// sim_template part 2: buildEventContext

const SIM_TPL_P2: &str = r#"
  function buildEventContext() {
    return { createEntity: recordCreated,
      getEntityBy: function(filter) {
        return { randomElement: function() {
          return { ifPresent: function(cb) {
            let found = null;
            try { let src = filter.toString();
              let m = src.match(/isContainingExactly\(hostApi\.string\.of\("([^"]+)"\)\)/);
              if (m) { const v = m[1];
                for (let i=0;i<globalThis.__entityStore.length;i++) {
                  const e = globalThis.__entityStore[i];
                  for (let key in e) { if (String(e[key]).includes(v)) { found = e; break; } }
                  if (found) break; }
              }
            } catch(e) {}
            if (!found && globalThis.__entityStore.length>0) found = globalThis.__entityStore[0];
            if (!found) return cb(null);
            const foundId = found && typeof found === 'object'
              ? (found.id || (globalThis.__entityStore.indexOf(found) >= 0 ? String(found) : null))
              : null;
            const wrapper = { getText: function(key) {
              return { ifPresent: function(cb2) {
                const nameObj = { concat: function(s) { try {
                  if (found && typeof found === 'object') {
                    if (key in found) found[key] = String(found[key]) + String(s);
                    else { const pk = Object.keys(found);
                      if (pk.length>0) found[pk[0]] = String(found[pk[0]]) + String(s); }
                  }
                } catch(e) {} }}; cb2(nameObj);
              }}; }, ifPresent: function(cb3) { cb3(wrapper); },
              getEntitiesInsideArea: function(areaName) {
                var areaMap = (globalThis.__insideArea && globalThis.__insideArea[foundId]) || {};
                var ids = areaMap[areaName] || [];
                return {
                  forEach: function(cb) {
                    for (var i = 0; i < ids.length; i++) {
                      cb({ getId: function() { return ids[i]; } });
                    }
                  },
                  size: function() { return ids.length; }
                };
              }};
            cb(wrapper);
          }};
        }};
      }
    };
  }"#;

pub fn get_part2() -> &'static str { SIM_TPL_P2 }
