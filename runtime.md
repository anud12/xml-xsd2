## Todo

30fps run,
socket per user,
login,
stream only updates,


- The runtime unpacks ZIP into an isolated in-memory filesystem and evaluates index.js inside a restricted JS sandbox (no direct access to node globals).
- Each module uses host API to declare an AST for the runtime to cache in memory.
- After finishing loading the modules the sandbox is closed, and all runs are done according to the extracted AST.

-Multiple threads can process different Actions simultaneously because their `ExecutionContexts` are independent. No global locks or shared state.


- Are events sync vs queued? reorder/atomicity guarantees?
The runtime employs double buffering, where when an 
   