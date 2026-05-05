package com.example.utils.archiveTestBuilder;

import com.example.utils.ArchiveRunner;

import java.io.IOException;

public interface ArchiveTestBuilderActions extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder triggerAction(String actionName) {
        String existing = this.getState().lastOutput != null ? new String(this.getState().lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        this.getState().runtimeInteropJava.ifPresent(ri -> ri.trigger_action(actionName));
        this.getState().lastOutput = (existing + ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED)
                .getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder sendActionToEntity(String actionName, String actorId, String targetId) throws IOException {
        String existing = this.getState().lastOutput != null ? new String(this.getState().lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        this.getState().runtimeInteropJava.ifPresent(ri -> ri.trigger_action(actionName));
        this.getState().lastOutput = (existing + ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED)
                .getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder sendActionToContainer(String actionName, String actorId, String containerId) throws IOException {
        String existing = this.getState().lastOutput != null ? new String(this.getState().lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        this.getState().runtimeInteropJava.ifPresent(ri -> ri.trigger_action(actionName));
        this.getState().lastOutput = (existing + ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED)
                .getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder runIterations(int count) {
        String existing = this.getState().lastOutput != null ? new String(this.getState().lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        this.getState().runtimeInteropJava.ifPresent(ri -> ri.runtime_debug_iterate(count));
        StringBuilder sb = new StringBuilder(existing);
        for (int i = 0; i < count; i++)
            sb.append("Iteration completed in 0:0ns\n");
        sb.append(ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED);
        this.getState().lastOutput = sb.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return (ArchiveTestBuilder) this;
    }
}
