
- `module`: every rule/ constant declaration are done in this module's context. When importing another, those informations can be read/modified through a "getContext" api

- rename name.md concepts to something similar to `numberExpression` (maybe `stringExpression`)


- `Architecture`: server is a `number cruncher` with `high memory constraints limitations` which reads data only on load, and uses `SIMD` to process all informations as flat as possible, to then relay changes to client or another service which has the role to persist data.