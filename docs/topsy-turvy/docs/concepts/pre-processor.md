---
sidebar_position: 2
---

# Pre-Processor

The pre-processor transforms source code prior to any parsing of the code is performed.  As part of the transformation, source mappings are built to ensure any modifications to text positions in the transformed code are mapped back to original positions in the original source code.  This ensures any errors from the toolchain, as well as rich experiences from the Analysis system, are properly displayed in code editors such as Visual Studio Code.

## Implementation

In the _Operetta Toolchain_ the pre-processor is implemented in the `BWHazel.TopsyTurvy.Parser` namespace in the `PreProcessorPipeline` class.  It works as a pipeline with a series of pre-processors transforming the source code in sequence.  Each of these pre-processors implement the `ITopsyTurvyPreProcessor` interface, which defines a single method `Process()` which accepts the source code as a `string` and the `SourceMap` to maintain the mappings between the transformed code and original source.  It returns a `PreProcessResult` containing the transformed source code and updated `SourceMap`.

The `SourceMap` stores a list of `SourceMapping` for every line kept during pre-processing which maps:

* The absolute character offset in the source of the start of the line to,
* The original source line and column where that line came from.

Please see the example below for how the mappings work between the original and transformed source.

In the _Operetta_ implementation the following pre-processors are supported:

* **Comment Replacement (`CommentsPreProcessor`):** Removes all comments, either removing them if on the same line as other code or replacing with blank lines otherwise.
* **Victorian Flourish Line Continuation Joiner (`VictorianFlourishPreProcessor`):** Removes the `~` "Victorian Flourish" line continuation character, joining the affected lines together.

The implementation of the pre-processor pipeline is demonstrated using an example.

## Creating a Pre-Processor

To create a new pre-processor, implement `ITopsyTurvyPreProcessor` with a `Process()` method that takes the current source text and the shared `SourceMap`, and returns a `PreProcessResult`.

The following example implements a **blank line compactor**, a pre-processor that collapses two or more consecutive blank lines into a single blank line:

```cs
using System;
using System.Text;
using BWHazel.TopsyTurvy.Parser;

public class BlankLineCompactorPreProcessor : ITopsyTurvyPreProcessor
{
    public PreProcessResult Process(string input, SourceMap currentSourceMap)
    {
        StringBuilder transformedSourceBuilder = new();

        string[] lines = input.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        int originalLine = 1;
        bool previousLineWasBlank = false;
        foreach (string line in lines)
        {
            bool isBlankLine = string.IsNullOrWhiteSpace(line);
            if (isBlankLine && previousLineWasBlank)
            {
                // Skip this line as a blank was already emitted above it.
                originalLine++;
                continue;
            }

            currentSourceMap.AddMapping(transformedSourceBuilder.Length, originalLine, 1);
            transformedSourceBuilder.AppendLine(line);
            previousLineWasBlank = isBlankLine;
            originalLine++;
        }

        return new(transformedSourceBuilder.ToString(), currentSourceMap);
    }
}
```

## Initialising the Pipeline

A pre-processor is added to the pipeline by calling `AddProcessor()` on the `PreProcessorPipeline` class.  When the pipeline runs by calling `Execute()` each pre-processor runs in the order it was added, receiving the output of the one before it.

```cs
PreProcessorPipeline pipeline = new();
pipeline.AddProcessor(new BlankLineCompactorPreProcessor());
PreProcessResult result = pipeline.Execute(source);
```

Given the following Topsy Turvy source:

```
BEHOLD "Ko-Ko"


BEHOLD "Lord High Executioner"
```

the two consecutive blank lines (original lines 2 and 3) have been collapsed into one by the pre-processor, stored in the `TransformedText` property of the returned `PreProcessResult`:

```
BEHOLD "Ko-Ko"

BEHOLD "Lord High Executioner"
```

There are a few things worth noting about this implementation, which demonstrate considerations to bear in mind when implementing pre-processors:

* **The `transformedSourceBuilder.Length` property is called before appending the pre-processed line:** The `StringBuilder` is pointing to the start of the transformed line which is required for a correct mapping to the original, hence is used in the `AddMapping()` call prior to appending the line.
* **Skipped lines get no mapping:** When a duplicate blank line is dropped, `originalLine` is still incremented so the counter stays in step with the original source, but no mapping entry is added and the line is not written to the transformed source.
* **Column is always 1:** Source mappings track the start of lines so the original column is always 1.  The precise column within a line is computed later by `SourceMap.GetOriginalLocation()` by measuring how far into the line an offset falls.

## Source Map

For every line that is **kept** during pre-processing, a source mapping entry is recorded in the Source Map, in the `SourceMap` property of the returned `PreProcessResult`.  Skipped lines are silently dropped therefore no entry is added.

Using the example above, the source map contains three entries:

|Offset in Transformed Text|Original Source Location|
|-|-|
|0|Line 1, Column 1|
|15|Line 2, Column 1|
|16|Line 4, Column 1|

The offset is the character count from the very beginning of the transformed text.  `BEHOLD "Ko-Ko"` is 14 characters and is followed by a newline so the next line starts at offset 15.  The kept blank line is a single newline character, so `BEHOLD "Lord High Executioner"` starts at offset 16.  Line 3, the second blank line which was dropped, has no entry.

If an error is reported at `BEHOLD "Lord High Executioner"`, the toolchain looks up offset 16 in the source map.  It finds the nearest preceding entry, **Offset 16** mapping to **Line 4, Column 1**, and reports that location to the user.  Even though the line moved during transformation the error always points back to the correct position in the original source.
