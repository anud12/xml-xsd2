TypeScript check for JavaScript tests

- Run: mvn -DskipTests=true process-test-resources (or mvn clean verify)
- The frontend-maven-plugin installs Node/npm and runs `npx tsc --noEmit --project tsconfig.json` during process-test-resources.
- JS files checked: src/test/resources/**/*.js
- Custom types: types/**/*.d.ts (typeRoots: ./types)

Adjust tsconfig.json or types/ if additional globals are needed.
