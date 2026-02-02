# Phase 2.1 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        MainWindow.axaml                          │
│                     (Avalonia UI Window)                         │
├─────────────────────────────────────────────────────────────────┤
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    Toolbar Panel                           │  │
│  │  • Margin Controls (NumericUpDown)                         │  │
│  │  • Apply Margins Button                                    │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    RulerControl                            │  │
│  │  (Visual ruler for measurements)                           │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    A4Canvas                                │  │
│  │  ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  │  │
│  │  ┃ ScrollViewer (gray background)                       ┃  │  │
│  │  ┃  ┌─────────────────────────────────────────────────┐ ┃  │  │
│  │  ┃  │  Canvas (padding container)                     │ ┃  │  │
│  │  ┃  │  ┌────────────────────────────────────────────┐ │ ┃  │  │
│  │  ┃  │  │ Border (A4 paper with shadow)              │ │ ┃  │  │
│  │  ┃  │  │ ┌────────────────────────────────────────┐ │ │ ┃  │  │
│  │  ┃  │  │ │ Canvas (text box container)            │ │ │ ┃  │  │
│  │  ┃  │  │ │ ┌────────────────────────────────────┐ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │ _marginCanvas (layer 1)            │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  ┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┐  │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  │ └┐└┐└┐└┐└┐└┐└┐└┐└┐└┐└┐└┐└┐  │  │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  │  └─ Left Margin Line (dotted)  │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  └────────────────────────────────┘ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  ┌────────────────────────────────┐ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  │ Top Margin Line (dotted)       │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  └────────────────────────────────┘ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  ┌────────────────────────────────┐ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  │ Right Margin Line (dotted)     │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  └────────────────────────────────┘ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  ┌────────────────────────────────┐ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  │ Bottom Margin Line (dotted)    │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  └────────────────────────────────┘ │ │ │ ┃  │  │
│  │  ┃  │  │ │ └────────────────────────────────────┘ │ │ │ ┃  │  │
│  │  ┃  │  │ │ ┌────────────────────────────────────┐ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │ RichTextEditor (layer 2)           │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  • Text editing area                │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  • Cursor tracking                  │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  • Times New Roman 12pt             │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ │  • Transparent background           │ │ │ │ ┃  │  │
│  │  ┃  │  │ │ └────────────────────────────────────┘ │ │ │ ┃  │  │
│  │  ┃  │  │ └────────────────────────────────────────┘ │ │ ┃  │  │
│  │  ┃  │  └────────────────────────────────────────────┘ │ ┃  │  │
│  │  ┃  └─────────────────────────────────────────────────┘ ┃  │  │
│  │  ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Component Hierarchy

```
MainWindow
└── DockPanel
    ├── Border (Toolbar) [DockPanel.Dock="Top"]
    │   └── StackPanel (Horizontal)
    │       ├── TextBlock ("MiniWord")
    │       ├── StackPanel (Left Margin Control)
    │       ├── StackPanel (Right Margin Control)
    │       └── Button (Apply Margins)
    │
    ├── Border (Ruler) [DockPanel.Dock="Top"]
    │   └── RulerControl
    │
    └── Border (Main Content) [LastChildFill]
        └── A4Canvas
            └── ScrollViewer
                └── Canvas (paperCanvas)
                    └── Border (paperBorder)
                        └── Canvas (textBoxContainer)
                            ├── Canvas (marginCanvas) [Layer 1]
                            │   ├── Line (leftMargin)
                            │   ├── Line (rightMargin)
                            │   ├── Line (topMargin)
                            │   └── Line (bottomMargin)
                            └── RichTextEditor [Layer 2]
```

## Data Flow

```
User Input
    │
    ├─→ Types in RichTextEditor
    │      │
    │      ├─→ Text property changed
    │      │      │
    │      │      └─→ IsDirty flag set in A4Document
    │      │
    │      └─→ CaretIndex property changed
    │             │
    │             └─→ CursorPositionChanged event fired
    │                    │
    │                    └─→ OnCursorPositionChanged handler
    │                           │
    │                           ├─→ Calculate cursor position
    │                           ├─→ Check proximity to margins
    │                           └─→ Update margin line colors
    │
    └─→ Changes margins in toolbar
           │
           └─→ Apply Margins button clicked
                  │
                  └─→ MainWindow.ApplyMargins()
                         │
                         └─→ A4Canvas.UpdateMargins()
                                │
                                ├─→ Update text box size/position
                                ├─→ Store new margin values
                                └─→ DrawMarginIndicators()
                                       │
                                       └─→ Redraw all margin lines
```

## Visual Feedback Logic

```
┌─────────────────────────────────────────────────┐
│ Cursor Position Changed Event                   │
└───────────────┬─────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────┐
│ Calculate Text Position                         │
│  • Get caret index                              │
│  • Find last newline before caret               │
│  • Calculate character count in current line    │
│  • Estimate X position (chars × font width)     │
└───────────────┬─────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────┐
│ Check Proximity to Margins                      │
│  • Threshold: 50 pixels                         │
│  • Near left: position < 50px                   │
│  • Near right: position > (width - 50px)        │
└───────────────┬─────────────────────────────────┘
                │
                ▼
        ┌───────┴───────┐
        │               │
        ▼               ▼
  ┌─────────┐     ┌──────────┐
  │  NEAR   │     │ NOT NEAR │
  │ MARGIN  │     │  MARGIN  │
  └────┬────┘     └─────┬────┘
       │                │
       ▼                ▼
  ┌─────────┐     ┌──────────┐
  │ Highlight│     │  Reset   │
  │  Blue    │     │   Gray   │
  │ (100,    │     │ (180,    │
  │  150,    │     │  180,    │
  │  200)    │     │  180)    │
  └──────────┘     └──────────┘
```

## Color Scheme

| Element | Default Color | Highlight Color | Purpose |
|---------|--------------|-----------------|---------|
| Margin Lines | RGB(180,180,180) Light Gray | RGB(100,150,200) Light Blue | Visual boundary markers |
| Paper Border | RGB(200,200,200) Gray | N/A | Page boundary |
| Paper Background | White | N/A | Document canvas |
| Text | Black | N/A | Content |
| Workspace | RGB(240,240,240) | N/A | Outside paper area |

## Key Measurements

| Element | Value | Notes |
|---------|-------|-------|
| A4 Width | 794px | At 96 DPI (210mm) |
| A4 Height | 1123px | At 96 DPI (297mm) |
| Default Margins | 96px | 1 inch all sides |
| Proximity Threshold | 50px | For visual feedback |
| Dash Pattern | [4, 4] | Dotted line style |
| Line Thickness | 1px | Subtle appearance |
| Font Size | 12pt | Times New Roman |
| Shadow Blur | 8px | Paper depth effect |

## Event System

```
┌──────────────────────────────────────────────────────┐
│            RichTextEditor Events                      │
├──────────────────────────────────────────────────────┤
│                                                       │
│  OnPropertyChanged(CaretIndexProperty)                │
│           │                                           │
│           ├─→ Creates CursorPositionChangedEventArgs │
│           │          │                                │
│           │          └─→ Contains: CaretIndex (int)  │
│           │                                           │
│           └─→ Fires: CursorPositionChanged event     │
│                      │                                │
│                      └─→ Subscribed by: A4Canvas     │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│              A4Canvas Event Handlers                  │
├──────────────────────────────────────────────────────┤
│                                                       │
│  OnCursorPositionChanged(sender, args)                │
│           │                                           │
│           ├─→ Extract caret index from args          │
│           ├─→ Calculate text position                │
│           ├─→ Check margin proximity                 │
│           └─→ Update margin line colors              │
└──────────────────────────────────────────────────────┘
```

## Thread Safety

All UI operations occur on the UI thread:
- Avalonia ensures proper threading for property changes
- Event handlers run on UI thread
- No async operations in current implementation
- Future async operations will use Dispatcher

## Performance Characteristics

| Operation | Frequency | Cost | Optimization |
|-----------|-----------|------|--------------|
| Draw Margin Lines | On margin change | O(4) | Reuses Line objects |
| Cursor Tracking | Per cursor move | O(1) | Simple arithmetic |
| Color Update | Per cursor move | O(4) | Direct property set |
| Text Rendering | Avalonia managed | N/A | Framework optimized |

## Memory Footprint

```
A4Canvas Instance:
├─ ScrollViewer: ~1KB
├─ Canvas (paperCanvas): ~500B
├─ Border (paperBorder): ~500B
├─ Canvas (textBoxContainer): ~500B
├─ Canvas (marginCanvas): ~500B
├─ RichTextEditor: ~2KB
├─ 4 × Line objects: ~400B each = 1.6KB
└─ List<Line>: ~100B

Total per instance: ~7KB (excluding text content)
```

This architecture provides:
1. ✅ Clean separation of concerns
2. ✅ Layered rendering (margins behind text)
3. ✅ Event-driven updates
4. ✅ Efficient resource usage
5. ✅ Extensible for future features
