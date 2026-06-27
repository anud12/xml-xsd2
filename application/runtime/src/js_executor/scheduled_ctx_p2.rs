// SCHEDULED_CTX_JS Part 2: get() with flatMap, isCondition

const SCHED_CTX_JS_P2: &str = r#", get: function(index) {
                return { flatMap: function(fn) {
                    if(!fe)return{orElse:function(d){return d;}};
                    var fk=null;
                    var ee={getNumber:function(key){fk=key;
                        return{map:function(cb3){
                            if(!fe.numberMap||fe.numberMap[key]===undefined)
                                return{orElse:function(d){return d;}};
                            var r=cb3({sum:function(s){
                                fe.numberMap[key]=Number(fe.numberMap[key])+Number(s);
                            },divide:function(d){
                                var nv=fe.numberMap[key],rem=Number(nv)%Number(d);
                                return{isEqualTo:function(t){
                                    var eq=(rem===Number(t));
                                    return{ifTrue:function(cb){
                                        if(eq&&typeof cb==='function')cb();
                                    },ifFalse:function(cb){
                                        if(!eq&&typeof cb==='function')cb();
                                    },orElse:function(d){
                                        return eq?{value:true,ifTrue:function(cb){
                                            if(typeof cb==='function')cb();
                                        },ifFalse:function(){}}:d;
                                    }};
                                }};
                            },isLessOrEqualTo:function(t){
                                return Number(fe.numberMap[key])<=Number(t);
                            },modulo:function(d){
                                var rem=Number(fe.numberMap[key])%Number(d);
                                return{isEqualTo:function(t){
                                    var eq=(rem===Number(t));
                                    return{ifTrue:function(cb){
                                        if(eq&&typeof cb==='function')cb();
                                    },ifFalse:function(cb){
                                        if(!eq&&typeof cb==='function')cb();
                                    },orElse:function(d){
                                        return eq?{value:true,ifTrue:function(cb){
                                            if(typeof cb==='function')cb();
                                        },ifFalse:function(){}}:d;
                                    }};
                                }};
                            }});
                            return r||{orElse:function(d){return d;}};
                        }}}
                    };
                    var result=fn(ee);
                    return{map:function(cb){
                        if(result&&typeof result.map==='function')
                            return result.map(cb);
                        return{orElse:function(d){return d;}};
                    },orElse:function(d){return d;},
                    isCondition:function(condFn){
                        var nv=fe.numberMap&&fk!==null?fe.numberMap[fk]:0;
                        var nw={isLessOrEqualTo:function(t){
                            return Number(nv)<Number(t);}};
                        var it=condFn(nw);
                        return{getOnTrueOrFalse:function(tv,fv){
                            return it?tv:fv;}};
                    }};
                }"#;

pub fn get_part2() -> &'static str { SCHED_CTX_JS_P2 }
