package com.example.utils.archiveTestBuilder.assertions;

import com.example.utils.LogAssertions;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

public interface AssertLogLines extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder assertLogLines(long expectedCount, String regexPattern) {
        LogAssertions.assertLogLineContainsRegex(this.getState(), expectedCount, regexPattern);
        return (ArchiveTestBuilder) this;
    }
}
