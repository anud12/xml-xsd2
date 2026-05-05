package com.example.utils.archiveTestBuilder.assertions;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

import java.util.regex.Pattern;

public interface AssertNotInLogs extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder assertNotInLogs(String regexPattern) {
        String output = this.getState().lastOutput != null ? new String(this.getState().lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        Pattern pattern = Pattern.compile(regexPattern.replace("{", "(").replace("}", ")"));
        for (String line : output.split("\\r?\\n")) {
            if (pattern.matcher(line).find()) {
                throw new AssertionError(
                        "Expected no log lines matching regex '" + regexPattern + "' but found at least one.\nFull output:\n" + output);
            }
        }
        return (ArchiveTestBuilder) this;
    }
}
