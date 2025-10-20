# Integration with UI Info Suite

<!-- TOC -->
* [Integration with UI Info Suite](#integration-with-ui-info-suite)
  * [Tooltip Additions](#tooltip-additions)
  * [Icon Integration](#icon-integration)
  * [Custom Fields](#custom-fields)
    * [Custom Display Name](#custom-display-name)
<!-- TOC -->

## Tooltip Additions

## Icon Integration

## Custom Fields

### Custom Display Name

**Field Name**: `UIInfoSuite.ExtendedData/DisplayName`

**Field Type**: `String`

**Description**: Display name for things that don't have them (e.g. Wild Trees)

**Example**:

```json
{
  "Action": "EditData",
  "Target": "Data/WildTrees",
  "Entries": {
    //Fir Tree
    "FlashShifter.StardewValleyExpandedCP_Fir_Tree": {
      // Snip
      "CustomFields": {
        "UIInfoSuite.ExtendedData/DisplayName": "{{i18n:displayname.fir-tree}}"
      }
    }
  }
}
```
