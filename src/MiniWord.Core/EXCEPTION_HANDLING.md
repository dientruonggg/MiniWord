# Exception Handling Documentation - MiniWord Core Layer

## Version: Phase 1 - P1.4
## Date: 2026-02-02

## Overview

This document describes the exception handling strategy for the MiniWord Core layer. All exceptions are properly logged and use custom exception types for better error categorization and debugging.

---

## Exception Hierarchy

```
Exception (System)
└── DocumentException (MiniWord.Core.Exceptions)
    ├── MarginException
    └── PageException
```

### DocumentException (Base)

**Purpose**: Base exception for all document-related errors.

**Properties**:
- `Message` (string): Human-readable error description
- `ErrorCode` (string): Machine-readable error category
- `InnerException` (Exception?): Original exception if wrapping another error

**Error Codes**:
- `DOCUMENT_ERROR` (default): General document operation error
- `INVALID_WIDTH`: Invalid width dimension
- `INVALID_LINE_HEIGHT`: Invalid line height
- `LINE_BREAK_ERROR`: Text line break calculation failed
- `REFLOW_ERROR`: Text reflow operation failed
- `NULL_MEASUREMENT_FUNCTION`: Required measurement function is null

**Usage**:
```csharp
throw new DocumentException("Operation failed");
throw new DocumentException("Invalid width", "INVALID_WIDTH");
throw new DocumentException("Failed to process", innerException);
throw new DocumentException("Failed to process", "SPECIFIC_ERROR", innerException);
```

---

### MarginException

**Purpose**: Specialized exception for margin validation and calculation errors.

**Inherits**: DocumentException

**Common Scenarios**:
- Negative margin values
- Margins too large for paper size
- Invalid paper dimensions
- Margin calculations that result in zero or negative available space

**Usage Examples**:
```csharp
// In DocumentMargins property setters
if (value < 0)
{
    var ex = new MarginException($"Left margin cannot be negative. Attempted value: {value}");
    _logger.Error(ex, "Invalid left margin value: {Value}", value);
    throw ex;
}

// In MarginCalculator
if (availableWidth <= 0)
{
    var ex = new MarginException(
        $"Margins are too large for the given paper width. Paper width: {paperWidth}px, Total margins: {margins.TotalHorizontal}px");
    _logger.Error(ex, "Margins too large: Available width is {Width}", availableWidth);
    throw ex;
}
```

---

### PageException

**Purpose**: Specialized exception for page-related operations (reserved for future use).

**Inherits**: DocumentException

**Potential Future Scenarios**:
- Invalid page index
- Page content exceeds capacity
- Page navigation errors
- Multi-page document errors

**Note**: Currently defined but not yet used. Will be utilized when more complex page operations are implemented in later phases.

---

## Exception Handling by Component

### 1. DocumentMargins (Models/DocumentMargins.cs)

**Exceptions Thrown**:
- `MarginException` - For negative margin values

**Validation Points**:
- Property setters for Left, Right, Top, Bottom margins

**Logging**:
- All validation failures are logged at ERROR level before throwing

**Example**:
```csharp
public double Left
{
    get => _left;
    set
    {
        if (value < 0)
        {
            var ex = new MarginException($"Left margin cannot be negative. Attempted value: {value}");
            _logger.Error(ex, "Invalid left margin value: {Value}", value);
            throw ex;
        }
        _left = value;
    }
}
```

---

### 2. A4Document (Models/A4Document.cs)

**Exceptions Thrown**:
- `MarginException` - When updating margins that exceed page dimensions

**Exception Paths**:
1. `UpdateMargins()` - Validates total margins don't exceed paper size

**Logging**:
- Margin validation errors logged at ERROR level
- Successful operations logged at INFORMATION level

**Example**:
```csharp
public void UpdateMargins(DocumentMargins newMargins)
{
    if (newMargins.TotalHorizontal >= A4_WIDTH_PX)
    {
        var ex = new MarginException(
            $"Total horizontal margin ({newMargins.TotalHorizontal:F1}px) exceeds page width ({A4_WIDTH_PX}px)");
        _logger.Error(ex, "Invalid margins: Total horizontal margin {Total} exceeds page width {Width}",
            newMargins.TotalHorizontal, A4_WIDTH_PX);
        throw ex;
    }
    // ... validation continues
}
```

---

### 3. TextFlowEngine (Services/TextFlowEngine.cs)

**Exceptions Thrown**:
- `DocumentException` - For text processing errors

**Exception Paths**:
1. `CalculateLineBreaks()` - Invalid width, null measurement function, processing errors
2. `ReflowText()` - Text reflow failures
3. `EstimateLinesInHeight()` - Invalid line height

**Error Codes Used**:
- `INVALID_WIDTH` - Non-positive available width
- `NULL_MEASUREMENT_FUNCTION` - Measurement function is null
- `LINE_BREAK_ERROR` - Unexpected error during line break calculation
- `REFLOW_ERROR` - Text reflow operation failed
- `INVALID_LINE_HEIGHT` - Non-positive line height

**Logging**:
- Input validation logged at ERROR level
- Successful operations logged at INFORMATION/DEBUG level
- Unexpected errors logged at ERROR level with full exception details

**Example**:
```csharp
if (availableWidth <= 0)
{
    var ex = new DocumentException(
        $"Available width must be positive. Provided: {availableWidth}px",
        "INVALID_WIDTH");
    _logger.Error(ex, "Invalid available width: {Width}", availableWidth);
    throw ex;
}

try
{
    // ... processing logic
}
catch (Exception ex) when (ex is not DocumentException)
{
    _logger.Error(ex, "Failed to calculate line breaks");
    throw new DocumentException("Failed to calculate line breaks", "LINE_BREAK_ERROR", ex);
}
```

---

### 4. MarginCalculator (Services/MarginCalculator.cs)

**Exceptions Thrown**:
- `MarginException` - For invalid dimensions or margin calculations

**Exception Paths**:
1. `CalculateAvailableWidth()` - Invalid paper width, margins too large
2. `CalculateAvailableHeight()` - Invalid paper height, margins too large
3. `ValidateMargins()` - Returns false instead of throwing (catches MarginException)

**Logging**:
- All validation errors logged at ERROR level
- Successful calculations logged at DEBUG level
- Validation failures logged at WARNING level

**Example**:
```csharp
public double CalculateAvailableWidth(double paperWidth, DocumentMargins margins)
{
    if (paperWidth <= 0)
    {
        var ex = new MarginException($"Paper width must be positive. Provided: {paperWidth}px");
        _logger.Error(ex, "Invalid paper width: {Width}", paperWidth);
        throw ex;
    }
    
    var availableWidth = paperWidth - margins.TotalHorizontal;
    
    if (availableWidth <= 0)
    {
        var ex = new MarginException(
            $"Margins are too large for the given paper width. Paper width: {paperWidth}px, Total margins: {margins.TotalHorizontal}px");
        _logger.Error(ex, "Margins too large: Available width is {Width}", availableWidth);
        throw ex;
    }
    
    return availableWidth;
}
```

---

## Logging Strategy

### Log Levels

**ERROR**: 
- All exceptions before throwing
- Validation failures
- Invalid input parameters

**WARNING**:
- Recoverable error conditions
- Validation failures that don't throw (e.g., ValidateMargins returning false)

**INFORMATION**:
- Successful operations with significant state changes
- Document creation, margin updates, page operations

**DEBUG**:
- Detailed operation tracking
- Calculation results
- Navigation events

### Log Format

All exception logs include:
1. Exception object (for stack trace)
2. Contextual message with placeholders
3. Relevant parameter values

**Example**:
```csharp
_logger.Error(ex, "Invalid margins: Total horizontal margin {Total} exceeds page width {Width}",
    newMargins.TotalHorizontal, A4_WIDTH_PX);
```

### Log Destination

- Console (during development and testing)
- File: `/logs/miniword-runtime.txt` (production)
- Configured in `appsettings.json` via Serilog

---

## Best Practices

### 1. Always Log Before Throwing

✅ **DO**:
```csharp
var ex = new DocumentException("Error message");
_logger.Error(ex, "Context with {Parameter}", paramValue);
throw ex;
```

❌ **DON'T**:
```csharp
throw new DocumentException("Error message"); // No logging!
```

---

### 2. Use Specific Exception Types

✅ **DO**:
```csharp
throw new MarginException("Margin too large"); // Specific type
```

❌ **DON'T**:
```csharp
throw new Exception("Margin too large"); // Generic type
```

---

### 3. Include Contextual Information

✅ **DO**:
```csharp
throw new MarginException(
    $"Paper width must be positive. Provided: {paperWidth}px");
```

❌ **DON'T**:
```csharp
throw new MarginException("Invalid input"); // No context
```

---

### 4. Use Error Codes for Programmatic Handling

✅ **DO**:
```csharp
throw new DocumentException("Invalid width", "INVALID_WIDTH");
// Caller can check: if (ex.ErrorCode == "INVALID_WIDTH") { ... }
```

❌ **DON'T**:
```csharp
throw new DocumentException("Invalid width");
// Caller must parse message string (fragile)
```

---

### 5. Preserve Inner Exceptions

✅ **DO**:
```csharp
catch (Exception ex) when (ex is not DocumentException)
{
    throw new DocumentException("Processing failed", "PROCESSING_ERROR", ex);
}
```

❌ **DON'T**:
```csharp
catch (Exception ex)
{
    throw new DocumentException("Processing failed"); // Lost inner exception!
}
```

---

## Testing Exception Handling

All exception paths are covered by unit tests in `ExceptionHandlingTests.cs`:

### Test Categories

1. **DocumentMargins Exception Tests** (6 tests)
   - Negative margin values for each side
   - Zero margins validation

2. **A4Document Exception Tests** (3 tests)
   - Horizontal margins exceeding width
   - Vertical margins exceeding height
   - Edge case: margins exactly equal to page size

3. **Custom Exception Tests** (6 tests)
   - Error code behavior
   - Exception hierarchy
   - Inner exception preservation

4. **TextFlowEngine Exception Tests** (3 tests)
   - Null measurement function
   - Invalid width values
   - Invalid line height

5. **MarginCalculator Exception Tests** (3 tests)
   - Zero paper dimensions
   - Margins equal to paper size

### Running Exception Tests

```bash
cd /home/runner/work/MiniWord/MiniWord
dotnet test --filter "FullyQualifiedName~ExceptionHandlingTests"
```

**Current Status**: ✅ All 20 exception tests passing

---

## Future Enhancements (Post-Phase 1)

### 1. Async Operation Error Handling
When async operations are added:
- Add timeout exceptions
- Handle cancellation properly
- Log task exceptions

### 2. Page Operation Exceptions
When pagination engine is implemented (Phase 2):
- Use PageException for page-related errors
- Add error codes for pagination failures

### 3. Validation Exception
Consider adding:
```csharp
public class ValidationException : DocumentException
{
    public List<ValidationError> Errors { get; }
    // For multiple validation failures
}
```

### 4. Resource Exceptions
For file operations (Phase 4):
- FileAccessException
- SerializationException

---

## Summary

| Exception Type       | Use Case                          | Logging Level | Error Codes Used              |
|---------------------|-----------------------------------|---------------|-------------------------------|
| DocumentException   | General document operations       | ERROR         | DOCUMENT_ERROR (default)      |
|                     | Text processing                   | ERROR         | INVALID_WIDTH, LINE_BREAK_ERROR, REFLOW_ERROR, NULL_MEASUREMENT_FUNCTION, INVALID_LINE_HEIGHT |
| MarginException     | Margin validation                 | ERROR         | Inherits from DocumentException |
| PageException       | Page operations (future)          | ERROR         | Inherits from DocumentException |

**Key Principles**:
1. ✅ All exceptions are logged before throwing
2. ✅ Custom exception types for better categorization
3. ✅ Error codes for programmatic error handling
4. ✅ Detailed error messages with context
5. ✅ Inner exceptions preserved
6. ✅ Comprehensive test coverage

---

## References

- Exception Classes: `/src/MiniWord.Core/Exceptions/`
- Exception Tests: `/tests/MiniWord.Tests/ExceptionHandlingTests.cs`
- Thread Safety: `/src/MiniWord.Core/THREAD_SAFETY.md`
- Logging Configuration: Handled by Serilog (P1.2)

---

## Document Version History

- v1.0 (2026-02-02): Initial exception handling documentation for P1.4
