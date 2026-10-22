// SCHEDULED_CTX_JS Part 1: opening + entity getter (getEntityBy delegates to
// the shared FilterArg evaluator installed by the host API).

const SCHED_CTX_JS_P1: &str = r#"(function() {
    globalThis.__context = {
        getEntityBy: function(filter) {
            return globalThis.__evalEntityFilter(filter);
        },
        emitEvent: function(name, payload) {
            if(name && typeof name==='object'){
                name=(typeof name.name==='string')?name.name
                    :((typeof name.value==='string')?name.value:String(name));
            }
            globalThis.__pendingEffects=globalThis.__pendingEffects||[];
            globalThis.__pendingEffects.push(name);
            var target=null,evs=globalThis.__registeredEvents||[];
            for(var i=0;i<evs.length;i++){
                if(evs[i]&&evs[i].name===name){target=evs[i];break;}
            }
            var _c=false;
            if(target){try{
                var cp=null;
                if(typeof target.prepare==='function')
                    cp=target.prepare(globalThis.__context);
                var hasVal=cp&&typeof cp==='object'&&'value' in cp;
                if(hasVal){
                    _c=!!cp.value;
                    if(typeof target.apply==='function')
                        target.apply(globalThis.__context,cp);
                }
            }catch(e){}}
            var _s=[_c];
            return{
                ifTrue:function(cb){if(_s[0]&&typeof cb==='function'){cb();_s[0]=false;}},
                ifFalse:function(cb){if(!_s[0]&&typeof cb==='function')cb();}
            };
        }
    };
})()"#;

pub fn get_part1() -> &'static str { SCHED_CTX_JS_P1 }
pub fn get_part2() -> &'static str { "" }
pub fn get_part3() -> &'static str { "" }
