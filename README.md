# MiniWord

A lightweight word processor built with Avalonia UI and .NET 10.

## Project Status

**Phase 1 - P1.1: Core Foundation - Document Logic & Text Flow** ✅ COMPLETE

## Architecture

The project follows a clean architecture with three main components:

```
MiniWord/
├── src/
│   ├── MiniWord.Core/      # Core business logic and models
│   ├── MiniWord.UI/        # Avalonia UI (MVVM pattern)
│   └── MiniWord.Tests/     # Unit tests
├── logs/                    # Runtime logs
└── MiniWord.slnx           # Solution file
```

## Phase 1 P1.1 Implementation

### Core Components

#### 1. Document Model
- **Document**: Root container for the entire document
  - Manages a collection of paragraphs
  - Tracks metadata (title, created/modified timestamps)
  - Provides statistics (paragraph count, character count, word count)
  - Supports CRUD operations on paragraphs

- **Paragraph**: Block-level text container
  - Contains a collection of text runs
  - Supports formatting (alignment, spacing, indentation)
  - Provides text extraction and empty check methods

- **TextRun**: Smallest unit of formatted text
  - Stores text content
  - Font properties (family, size)
  - Text styling (bold, italic, underline, color)
  - Clone functionality for copying formatting

#### 2. Services
- **DocumentManager**: Manages document operations and text flow
  - Document lifecycle (create, clear)
  - Text insertion with formatting support
  - Paragraph management (append, insert, remove)
  - Advanced operations (merge, split paragraphs)
  - Statistics retrieval

#### 3. Logging Infrastructure
- **Logger**: Thread-safe logging system
  - All exceptions logged to `/logs/miniword-runtime.txt`
  - Supports info, warning, error, and exception logging
  - Automatic log directory creation

### Testing

- **34 unit tests** with 100% pass rate
- Coverage includes:
  - Model classes (Document, Paragraph, TextRun)
  - Service classes (DocumentManager)
  - All CRUD operations
  - Edge cases and error conditions

## Building and Running

### Prerequisites
- .NET 10 SDK
- Linux (Zorin OS) / Windows / macOS

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run UI Application
```bash
dotnet run --project src/MiniWord.UI/MiniWord.UI.csproj
```

## Technology Stack

- **.NET 10**: Core framework
- **Avalonia UI**: Cross-platform UI framework
- **xUnit**: Unit testing framework
- **MVVM Pattern**: UI architecture pattern

## System Requirements

- **OS**: Linux (Zorin OS), Windows, or macOS
- **Framework**: .NET 10.0 or higher
- **IDE**: Visual Studio Code, JetBrains Rider, or Visual Studio

## Development Guidelines

1. All code follows the Core/UI/Tests architecture
2. All exceptions must be logged to `/logs/miniword-runtime.txt`
3. Use MVVM pattern for UI components
4. Write unit tests for all business logic
5. Follow existing code style and conventions
