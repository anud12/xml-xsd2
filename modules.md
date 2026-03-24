# Module System Model
This document describes the core `Module` concept.

## Summary
Modules are ZIP archives containing sandboxed JavaScript (ESM) and related assets. Modules can be uploaded or overwritten at runtime to change rulesets and behavior without rebuilding the runtime. Modules are stored in-memory (ephemeral) by the runtime at present; persistence and promotion to disk are considered separately.

Module entrypoint is [root]/index.js. A manifest.json at the root describes metadata, permissions and script entrypoint.

## Proposed Architecture

- Modules are uploaded as ZIP archives and validated before loading. Validation includes manifest presence, entrypoint availability and size limits,
- The runtime unpacks ZIP into an isolated in-memory filesystem and evaluates index.js inside a restricted JS sandbox (no direct access to node globals).
- Each loaded module has a lifecycle and a small sandboxed runtime that exposes a restricted host API.
- Each module uses host API to declare an AST for the runtime to cache in memory.
- After finishing loading the modules the sandbox is closed, and all runs are done according to the extracted AST.

### Module ZIP layout (required)

Root (required):
- manifest.json       - Metadata, permissions, exported APIs, version.

Files outside these paths are allowed but should be referenced explicitly from imports.

### manifest.json (schema, example)

Required fields:
- id (string): unique module id (reverse-DNS recommended)
- version (string): semver compliant
- name (string): human friendly name
- entry (string): relative path to entrypoint (default: "index.js")
- permissions (array): list of requested runtime permissions (see below)

Optional:
- description, author, repository, license
- assets: listing of public asset paths

Example:
```json
{
  "id": "com.example.rules.weather",
  "version": "1.2.0",
  "name": "Weather Name Rules",
  "entry": "index.js",
}
```

### Entry script API and exports

Entry script must be an ESM module. The sandbox passes a `HostApi` object passed to the module's exported default function.

```javascript
/**
 * @param {HostApi} arg - The input string to process.
 * @returns {void}
 */
export default (arg) => {
  /* module logic */
}
```

### Security & Sandboxing (high level)

- Execute module code in a dedicated JS sandbox with no direct access to process, filesystem, or network unless explicitly proxied via the host API.
- Sanitize any content coming from modules before using it in core systems (e.g., ensure rules are well-formed and cannot inject arbitrary runtime behavior).

### Size and performance constraints

- Max uncompressed module size (configurable)
- Module initialization should be synchronous.
- Prefer pre-loading of large assets;
- The runtime would offer the functionality to download the archived module.


### Host Api 

  The runtime exposes the following TypeScript declaration file (.d.ts) which enhances the HostApi.
  Modules should import or reference this file to type their entry function and host interactions.

  ```typescript
   export type HostApi = {
    /*... rest of declarations */
    loadModule: <SubmoduleType>(name:string) => SubmoduleType
   }
  ```

