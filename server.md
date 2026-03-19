## Todo

30fps run,
socket per user,
login,
stream only updates,


- The server unpacks ZIP into an isolated in-memory filesystem and evaluates index.js inside a restricted JS sandbox (no direct access to node globals).
- Each module uses host API to declare an AST for the server to cache in memory.
- After finishing loading the modules the sandbox is closed, and all runs are done according to the extracted AST.