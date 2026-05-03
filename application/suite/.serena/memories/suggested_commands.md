## Suggested Commands for suite (Java test project)

### Building & Testing
- `mvn clean compile test` - Build and run all tests
- `cargo test --manifest-path ../runtime/Cargo.toml` - Run Rust runtime tests (which triggers Maven tests via java_tests.rs)
- `cd ..\runtime && cargo test` - Run from runtime directory

### Git Operations
- `git status`, `git diff`, `git log` - Standard git commands
- `git branch`, `git checkout -b <branch>`, `git commit -m "msg"` - Branch and commit management

### Code Style
- Java 21 with standard conventions
- Package: `com.example.*` for test code
- Naming: camelCase for methods/variables, PascalCase for classes
