# General
## A Standard Library for Topsy Turvy
### Specification: Version 0.0.1-preview1

---

## Overview

The Standard Library, **General**, after the Major-General in the Pirates of Penznce who has "information vegetable, animal, and mineral", provides common functionality available to all Topsy Turvy programmes.  An implementation of the Standard Library is expected to be provided as part of a language implementation or be able to easily integrate with one.

## Global Namespace

### `PreviewBehold` Function

> **Preview**<br />
> This is a preview function and may change without warning.

Prints the specified `Text` to standard output with a `WithCeremony` value indicating whether a newline should be added at the end.

```topsy
IT IS MY DUTY TO PERFORM PreviewBehold ~
    UNDER THE TERMS OF ~
        Text AS A YARN AND ~
        WithCeremony AS A DECREE
```

**Parameters**

|Parameter|Type|Description|
|-|-|-|
|`Text`|`YARN`|The text to print to Standard Output.|
|`WithCeremony`|`DECREE`|A value indicating whether a new-line should be applied.|

**Returns**

_No return value._

### `PreviewPrayTell` Function

> **Preview**<br />
> This is a preview function and may change without warning.

Returns the value from standard input or an empty string if no value provided.

```topsy
IT IS MY DUTY TO PERFORM PreviewPrayTell ~
    TO FIND YARN ~
    UNDER NO OBLIGATION
```

**Parameters**

_No parameters._

**Returns**

|Type|Description|
|-|-|
|`YARN`|The value returned from Standard Input.|