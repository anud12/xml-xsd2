# TODO: Frame-by-frame Runtime Control & Inspection

## Goal
Enable frame-by-frame (step-wise) execution and state inspection of the runtime (as an external process) from Java/Cucumber tests.

## Steps

1. **Design Control API**
   - Define API endpoints/commands for: step, pause, resume, reset, and state inspection.
   - Choose protocol (WebSocket, HTTP, gRPC, etc.).

2. **Implement Control API in Runtime**
   - Add server to runtime process to handle control commands.
   - Implement step-wise execution logic (e.g., nextFrame()).
   - Expose state snapshot endpoint/command.

3. **Expose State for Inspection**
   - Serialize relevant internal state for external queries (JSON, protobuf, etc.).
   - Ensure state queries are consistent and safe.

4. **Java/Cucumber Client Implementation**
   - Implement client to send control commands and fetch state from runtime.
   - Integrate with Cucumber step definitions.

5. **Write Cucumber Steps**
   - Add steps for: start runtime, step, pause, resume, reset, inspect state, assert state.

6. **Test & Iterate**
   - Write sample scenarios to verify step-wise control and state inspection.
   - Refine API and implementation as needed.
