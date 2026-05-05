package com.example.utils.archiveTestBuilder.assertions;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

import java.util.regex.Pattern;

public interface AssertLogLineContains extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder assertLogLineContains(int expectedCount, String regex) {
        String output = this.getState().lastOutput != null ? new String(this.getState().lastOutput) : "";
        Pattern pattern = Pattern.compile(regex.replace("{", "(").replace("}", ")"));
        int matches = 0;
        for (String line : output.split("\\r?\\n")) {
            if (pattern.matcher(line).find())
                matches++;
        }
        if (matches != expectedCount) {
            throw new AssertionError(
                    "Expected exactly %d log line(s) matching regex '%s' but found %d.\nFull output:\n%s"
                            .formatted(expectedCount, regex, matches, output));
        }
        return (ArchiveTestBuilder) this;
    }
}
