# MiniWord Implementation Roadmap
**From Mockup to Production**

## 📋 Overview

Dự án MiniWord hiện tại đã có:
- ✅ Mockup UI hoàn chỉnh
- ✅ Architecture 3-layer (Core/UI/Tests)
- ✅ Logging infrastructure (Serilog)
- ✅ Basic text flow logic

Roadmap này chia nhỏ quá trình phát triển thành **7 phases độc lập**, mỗi phase có scope rõ ràng, unit tests, và ước lượng prompt cần thiết để hoàn thành production-ready code chạy mượt trên Linux.

---

## 🎯 Phase 1: Core Foundation - Document Logic & Text Flow

**Mục tiêu:** Hoàn thiện core business logic cho A4 document processing, text wrapping algorithm, và pagination system không phụ thuộc UI.

**Prompt Budget:** 3-4 prompts

| Prompt                | Nội dung & Scope                                                                                                                                                                                                                | Deliverables                                                         |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------|
| **P1.1**              | Hoàn thiện `TextFlowEngine.cs` - Improve word wrapping algorithm (xử lý long words, hyphenation, Unicode), thêm `ReflowText()` method khi margins thay đổi. Implement `Func<string, double>` integration với real font metrics. | `TextFlowEngine.cs` + 5-7 unit tests trong `TextFlowEngineTests.cs`  |
| **P1.2**              | Tạo `PaginationEngine.cs` trong `MiniWord.Core/Services/` - Logic chia text thành pages dựa trên `AvailableHeight`, xử lý page breaks, page numbering.                                                                          | `PaginationEngine.cs` + `PaginationEngineTests.cs` (8-10 test cases) |
| **P1.3**              | Enhance `A4Document.cs` - Thêm multi-page support (`List<Page>`), page navigation methods, document state management (`IsDirty` flag). Implement `INotifyPropertyChanged` cho document properties.                              | Updated `A4Document.cs` + `A4DocumentTests.cs` (6-8 tests)           |
| **P1.4** *(Optional)* | Exception handling review - Kiểm tra tất cả exception paths trong Core layer có logging + custom exceptions rõ ràng. Code review cho thread safety nếu cần async operations sau này.                                            | Exception documentation + edge case tests                            |

### Chiến lược
- ✓ Focus 100% vào Core logic, không động vào UI
- ✓ Mỗi prompt test trước bằng xUnit, chạy `dotnet test` trên terminal Linux
- ✓ Logging mọi operation quan trọng với Serilog

---

## 🎨 Phase 2: UI Controls Enhancement - A4Canvas & Rich Text Editing

**Mục tiêu:** Nâng cấp `A4Canvas.cs` từ mockup thành fully functional text editor với margin visualization, cursor management, và text selection.

**Prompt Budget:** 4-5 prompts

| Promp-t               | Nội dung & -Scope                                                                                                                                                                                    | Deliverables                                       |
|-----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------|
| **P2.1**              | Refactor `A4Canvas.cs` - Tách TextBox thành custom RichTextEditor control. Implement margin visualization (dotted lines), page boundaries, và visual feedback khi text approach margins.             | Enhanced `A4Canvas.cs` + visual margin indicators  |
| **P2.2**              | Implement text rendering pipeline - Connect `TextFlowEngine` output (`List<TextLine>`) với Avalonia's `FormattedText` API. Render text lines với proper baseline alignment, line height calculation. | Text rendering integration + font metrics handling |
| **P2.3**              | Cursor & Selection management - Implement caret positioning, text selection (mouse + keyboard), copy/paste operations. Map WinForms `TextBox.SelectionStart/Length` sang Avalonia equivalents.       | Cursor/selection system + clipboard integration    |
| **P2.4**              | Scrolling & viewport optimization - Handle `ScrollViewer` behavior khi document có multiple pages. Implement smooth scrolling với keyboard (Page Up/Down, Ctrl+Home/End).                            | Scrolling behavior + keyboard navigation           |
| **P2.5** *(Optional)* | Performance optimization - Virtual rendering cho large documents (chỉ render visible pages). Debounce text change events để avoid excessive reflow calculations.                                     | Performance profiling results                      |

### Chiến lược
- ✓ Mỗi prompt test bằng cách run app trên Zorin OS terminal (`dotnet run`)
- ✓ Verify keyboard shortcuts work correctly (Avalonia events map từ WinForms patterns)
- ✓ No unit tests cho UI controls (integration tests only nếu cần)

---

## 🔗 Phase 3: ViewModel & Data Binding - MVVM Implementation

**Mục tiêu:** Hoàn thiện `MainWindowViewModel.cs` với proper MVVM pattern, reactive properties, và command binding thay thế event handlers.

**Prompt Budget:** 2-3 prompts

| Prompt                | Nội dung & Scope                                                                                                                                                                                                               | Deliverables                                   |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------|
| **P3.1**              | Refactor `MainWindowViewModel.cs` - Implement ReactiveUI hoặc CommunityToolkit.Mvvm cho ICommand bindings. Convert button click events sang RelayCommand pattern. Add document properties (PageCount, WordCount, CurrentPage). | MVVM-compliant ViewModel + NuGet package setup |
| **P3.2**              | Two-way binding cho margin controls - Bind NumericUpDown values directly với ViewModel properties. Implement `IValueConverter` cho mm↔pixels conversion. Remove code-behind event handlers trong `MainWindow.axaml.cs`.        | XAML bindings + value converters               |
| **P3.3** *(Optional)* | Validation logic - Add input validation cho margin values (min/max constraints), show validation errors trong UI. Implement `IDataErrorInfo` hoặc `INotifyDataErrorInfo`.                                                      | Input validation + error UI feedback           |

### Chiến lược
- ✓ Áp dụng strict separation: Logic trong ViewModel, UI chỉ bind + display
- ✓ Test bindings bằng cách chạy app và verify UI updates khi ViewModel properties change
- ✓ Consider ReactiveUI nếu muốn reactive programming style, hoặc CommunityToolkit.Mvvm nếu prefer đơn giản hơn

---

## 💾 Phase 4: File Operations - Save/Load Document System

**Mục tiêu:** Implement file I/O (`.miniword` format hoặc plain text), recent files tracking, và dirty flag management.

**Prompt Budget:** 3 prompts

| Prompt   | Nội dung & Scope                                                                                                                                                                   | Deliverables                                           |
|----------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------|
| **P4.1** | Tạo `DocumentSerializer.cs` trong `MiniWord.Core/Services/` - Serialize/deserialize document với metadata (margins, formatting). Support JSON format với `System.Text.Json`.       | `DocumentSerializer.cs` + `DocumentSerializerTests.cs` |
| **P4.2** | File menu implementation - Add File→New/Open/Save/SaveAs commands trong `MainWindow.axaml`. Use Avalonia's `OpenFileDialog`/`SaveFileDialog`. Handle unsaved changes confirmation. | File menu + dialog integration                         |
| **P4.3** | Recent files tracking - Implement MRU list (Most Recently Used), persist trong app settings (JSON file trong `~/.config/miniword/`). Add "Open Recent" submenu.                    | Recent files feature + settings persistence            |

### Chiến lược
- ✓ Test file operations trên Linux filesystem (`/home/dien/Documents/`)
- ✓ Handle file permissions errors gracefully với exception handling + logging
- ✓ Unit tests cho serialization logic, manual tests cho dialogs

---

## 🔍 Phase 5: Advanced Text Features - Find/Replace & Formatting

**Mục tiêu:** Search functionality, basic text formatting (bold/italic/underline), và font selection.

**Prompt Budget:** 4 prompts

| Prompt   | Nội dung & Scope                                                                                                                                                              | Deliverables                               |
|----------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------|
| **P5.1** | Tạo `SearchEngine.cs` trong `MiniWord.Core/Services/` - Implement find/replace logic (case-sensitive, whole word, regex support). Return match positions (`List<TextRange>`). | `SearchEngine.cs` + `SearchEngineTests.cs` |
| **P5.2** | Find/Replace dialog UI - Create `FindReplaceWindow.axaml` (modal dialog). Highlight search results trong editor. Implement "Find Next/Previous" navigation.                   | Find/Replace dialog + result highlighting  |
| **P5.3** | Text formatting system - Extend `TextLine.cs` với `FormattingSpan` (bold/italic/underline ranges). Update rendering pipeline để apply formatting. Add toolbar buttons.        | Formatting infrastructure + UI controls    |
| **P5.4** | Font selection - Add font family dropdown và font size spinner trong toolbar. Persist font preferences trong document metadata.                                               | Font selection UI + preference persistence |

### Chiến lược
- ✓ Phase này có thể split làm 2 sub-phases nếu scope quá lớn (Search riêng, Formatting riêng)
- ✓ Focus vào core search logic trước, UI polish sau
- ✓ Test regex search patterns kỹ để avoid ReDoS vulnerabilities

---

## 🌐 Phase 6: MiniBrowser Integration - Google Search Feature

**Mục tiêu:** Embed web browser control để search Google, display results, và integrate selected text vào document.

**Prompt Budget:** 4-5 prompts

| Prompt                | Nội dung & Scope                                                                                                                                                                                       | Deliverables                                       |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------|
| **P6.1**              | Research & setup - Evaluate browser control options cho Avalonia Linux (CefSharp.Avalonia? WebView2? Native WebKit?). Install required packages + dependencies.                                        | Package evaluation document + setup guide          |
| **P6.2**              | Tạo `BrowserControl.cs` - Wrap chosen browser control trong Avalonia UserControl. Implement basic navigation (GoToUrl, Back/Forward, Refresh). Handle loading states.                                  | `BrowserControl.cs` + basic navigation UI          |
| **P6.3**              | Google Search integration - Create `GoogleSearchService.cs` trong Core layer (sử dụng Google Custom Search API hoặc web scraping với HtmlAgilityPack). Return structured results (SearchResult model). | `GoogleSearchService.cs` + API/scraping logic      |
| **P6.4**              | MiniBrowser UI - Add side panel hoặc floating window cho browser view. Implement search box, results list, và "Insert to Document" button.                                                             | Browser UI panel + insert functionality            |
| **P6.5** *(Optional)* | Text extraction & formatting - Extract plain text từ selected webpage content. Clean HTML tags, preserve paragraphs. Apply formatting khi insert vào document.                                         | Text extraction pipeline + formatting preservation |

> [!WARNING]
> **HIGH RISK:** Browser controls trên Linux phức tạp. Có thể cần fallback plan (external browser launch + clipboard import).

### Chiến lược
- ✓ Test thoroughly trên Zorin OS - browser controls có thể cần native dependencies (`libgtk`, `libwebkit2gtk`)
- ✓ Consider security implications (JavaScript execution, URL validation)

---

## ✨ Phase 7: Polish & Production Readiness

**Mục tiêu:** Performance optimization, comprehensive error handling, Linux-specific tweaks, và deployment packaging.

**Prompt Budget:** 3-4 prompts

| Prompt   | Nội dung & Scope                                                                                                                                                                           | Deliverables                               |
|----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------|
| **P7.1** | Performance profiling - Profile app với large documents (>1000 pages). Optimize text rendering, implement caching for `FormattedText` objects. Add loading indicators cho slow operations. | Performance benchmarks + optimizations     |
| **P7.2** | Exception handling review - Audit tất cả try-catch blocks. Add global exception handler (`AppDomain.UnhandledException`). Improve error messages cho end users.                            | Exception handling audit report            |
| **P7.3** | Linux integration - Test keyboard shortcuts, clipboard operations, file dialogs trên Zorin OS. Add app icon, desktop entry file (`.desktop`), và integration với Linux file managers.      | Linux-specific fixes + desktop integration |
| **P7.4** | Packaging & deployment - Create self-contained publish profile (`dotnet publish -c Release -r linux-x64 --self-contained`). Write installation script + README. Test on fresh Linux VM.    | Deployment package + installation docs     |

### Chiến lược
- ✓ Phase này là "clean-up" phase - **không add new features**
- ✓ Test trên clean Zorin OS VM để verify dependencies
- ✓ Document tất cả Linux-specific requirements (`libicu`, `libssl`, etc.)

---

## 🤔 Further Considerations

### Phase 6 Risk Mitigation
Nếu MiniBrowser integration quá phức tạp trên Linux, có thể pivot sang simpler approach:

| Option  | Approach                                                         | Complexity  |
|---------|------------------------------------------------------------------|-------------|
| **A**   | Launch external browser (`xdg-open`) + clipboard monitoring      | Low         |
| **B**   | Built-in simple HTTP client với text-only results (no rendering) | Medium      |
| **C**   | Defer browser feature sang Phase 8 (post-MVP)                    | N/A         |

### Testing Strategy
Current tests chỉ có unit tests cho Core layer. Consider:

- **Option A:** Add integration tests cho key workflows (open→edit→save) using Avalonia's testing framework
- **Option B:** Manual test checklist cho mỗi phase
- **Option C:** No additional tests (keep current unit test coverage)

### MVVM Library Choice
Phase 3 requires choosing MVVM framework:

| Framew-ork                        | Pr-os                                               | C-ons                  |
|-----------------------------------|-----------------------------------------------------|------------------------|
| **ReactiveUI**                    | More powerful, reactive programming style           | Steeper learning curve |
| **CommunityToolkit.Mvvm**         | Simpler, familiar nếu từ WinForms, less boilerplate | Less powerful          |
| **Manual INotifyPropertyChanged** | No dependencies, full control                       | More code              |

### Prompt Context Management
Với **25-30 prompts total**, recommend:

1. ✓ Start mỗi new phase bằng context summary prompt (_"We're starting Phase X, previous phases completed Y features..."_)
2. ✓ Use Git commits sau mỗi phase để có rollback points
3. ✓ Keep logs directory (`/logs/miniword-*.txt`) để reference trong troubleshooting prompts

---

## 📊 Summary

| Phase     | Focus Area          | Prompts   | Risk Level  |
|-----------|---------------------|-----------|-------------|
| 1         | Core Foundation     | 3-4       | 🟢 Low      |
| 2         | UI Controls         | 4-5       | 🟡 Medium   |
| 3         | MVVM Pattern        | 2-3       | 🟢 Low      |
| 4         | File Operations     | 3         | 🟢 Low      |
| 5         | Text Features       | 4         | 🟡 Medium   |
| 6         | Browser Integration | 4-5       | 🔴 High     |
| 7         | Polish & Deploy     | 3-4       | 🟡 Medium   |
| **TOTAL** |                     | **23-28** |             |