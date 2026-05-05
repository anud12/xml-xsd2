package com.example.utils.archiveTestBuilder.assertions;

import com.example.utils.StateAssertions;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

public interface AssertPanelNames extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder assertPanelNames(String json) {
        StateAssertions.assertReturnedPanelNamesIsIn(this.getState(), json);
        return (ArchiveTestBuilder) this;
    }
}
