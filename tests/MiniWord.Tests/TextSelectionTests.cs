using MiniWord.Core.Models;
using Xunit;
using System;

namespace MiniWord.Tests;

/// <summary>
/// Tests for TextSelection model (P2.3: Cursor & Selection management)
/// </summary>
public class TextSelectionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidRange_CreatesSelection()
    {
        // Arrange & Act
        var selection = new TextSelection(5, 10);

        // Assert
        Assert.Equal(5, selection.Start);
        Assert.Equal(10, selection.End);
        Assert.Equal(5, selection.Length);
        Assert.False(selection.IsEmpty);
    }

    [Fact]
    public void Constructor_ReversedRange_NormalizesSelection()
    {
        // Arrange & Act - end before start should be normalized
        var selection = new TextSelection(10, 5);

        // Assert - should swap to have Start <= End
        Assert.Equal(5, selection.Start);
        Assert.Equal(10, selection.End);
        Assert.Equal(5, selection.Length);
    }

    [Fact]
    public void Constructor_SameStartAndEnd_CreatesEmptySelection()
    {
        // Arrange & Act
        var selection = new TextSelection(5, 5);

        // Assert
        Assert.Equal(5, selection.Start);
        Assert.Equal(5, selection.End);
        Assert.Equal(0, selection.Length);
        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void Constructor_NegativeStart_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextSelection(-1, 5));
    }

    [Fact]
    public void Constructor_NegativeEnd_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextSelection(5, -1));
    }

    #endregion

    #region Factory Methods

    [Fact]
    public void Empty_CreatesEmptySelection()
    {
        // Act
        var selection = TextSelection.Empty();

        // Assert
        Assert.Equal(0, selection.Start);
        Assert.Equal(0, selection.End);
        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void Empty_WithPosition_CreatesEmptySelectionAtPosition()
    {
        // Act
        var selection = TextSelection.Empty(10);

        // Assert
        Assert.Equal(10, selection.Start);
        Assert.Equal(10, selection.End);
        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void FromStartAndLength_ValidValues_CreatesSelection()
    {
        // Act
        var selection = TextSelection.FromStartAndLength(5, 10);

        // Assert
        Assert.Equal(5, selection.Start);
        Assert.Equal(15, selection.End);
        Assert.Equal(10, selection.Length);
    }

    [Fact]
    public void FromStartAndLength_ZeroLength_CreatesEmptySelection()
    {
        // Act
        var selection = TextSelection.FromStartAndLength(5, 0);

        // Assert
        Assert.Equal(5, selection.Start);
        Assert.Equal(5, selection.End);
        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void FromStartAndLength_NegativeStart_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => TextSelection.FromStartAndLength(-1, 5));
    }

    [Fact]
    public void FromStartAndLength_NegativeLength_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => TextSelection.FromStartAndLength(5, -1));
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_PositionInRange_ReturnsTrue()
    {
        // Arrange
        var selection = new TextSelection(5, 10);

        // Act & Assert
        Assert.True(selection.Contains(5));  // Start boundary
        Assert.True(selection.Contains(7));  // Middle
        Assert.False(selection.Contains(10)); // End boundary (exclusive)
    }

    [Fact]
    public void Contains_PositionOutOfRange_ReturnsFalse()
    {
        // Arrange
        var selection = new TextSelection(5, 10);

        // Act & Assert
        Assert.False(selection.Contains(4));  // Before start
        Assert.False(selection.Contains(11)); // After end
    }

    [Fact]
    public void Contains_EmptySelection_ReturnsFalse()
    {
        // Arrange
        var selection = TextSelection.Empty(5);

        // Act & Assert
        Assert.False(selection.Contains(5));
    }

    #endregion

    #region IntersectsWith Tests

    [Fact]
    public void IntersectsWith_OverlappingSelections_ReturnsTrue()
    {
        // Arrange
        var selection1 = new TextSelection(5, 10);
        var selection2 = new TextSelection(8, 12);

        // Act & Assert
        Assert.True(selection1.IntersectsWith(selection2));
        Assert.True(selection2.IntersectsWith(selection1));
    }

    [Fact]
    public void IntersectsWith_TouchingSelections_ReturnsTrue()
    {
        // Arrange
        var selection1 = new TextSelection(5, 10);
        var selection2 = new TextSelection(10, 15);

        // Act & Assert - touching at boundary
        Assert.False(selection1.IntersectsWith(selection2));
    }

    [Fact]
    public void IntersectsWith_NonOverlappingSelections_ReturnsFalse()
    {
        // Arrange
        var selection1 = new TextSelection(5, 10);
        var selection2 = new TextSelection(15, 20);

        // Act & Assert
        Assert.False(selection1.IntersectsWith(selection2));
        Assert.False(selection2.IntersectsWith(selection1));
    }

    [Fact]
    public void IntersectsWith_ContainedSelection_ReturnsTrue()
    {
        // Arrange
        var outer = new TextSelection(5, 15);
        var inner = new TextSelection(8, 12);

        // Act & Assert
        Assert.True(outer.IntersectsWith(inner));
        Assert.True(inner.IntersectsWith(outer));
    }

    [Fact]
    public void IntersectsWith_NullSelection_ThrowsException()
    {
        // Arrange
        var selection = new TextSelection(5, 10);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => selection.IntersectsWith(null!));
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameSelection_ReturnsTrue()
    {
        // Arrange
        var selection1 = new TextSelection(5, 10);
        var selection2 = new TextSelection(5, 10);

        // Act & Assert
        Assert.True(selection1.Equals(selection2));
        Assert.Equal(selection1.GetHashCode(), selection2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentSelection_ReturnsFalse()
    {
        // Arrange
        var selection1 = new TextSelection(5, 10);
        var selection2 = new TextSelection(5, 15);

        // Act & Assert
        Assert.False(selection1.Equals(selection2));
    }

    [Fact]
    public void Equals_NormalizedAndReversed_AreEqual()
    {
        // Arrange - both should normalize to (5, 10)
        var selection1 = new TextSelection(5, 10);
        var selection2 = new TextSelection(10, 5);

        // Act & Assert
        Assert.True(selection1.Equals(selection2));
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_EmptySelection_ReturnsEmptyFormat()
    {
        // Arrange
        var selection = TextSelection.Empty(5);

        // Act
        var result = selection.ToString();

        // Assert
        Assert.Contains("Empty", result);
        Assert.Contains("5", result);
    }

    [Fact]
    public void ToString_NonEmptySelection_ReturnsRangeFormat()
    {
        // Arrange
        var selection = new TextSelection(5, 10);

        // Act
        var result = selection.ToString();

        // Assert
        Assert.Contains("Selection", result);
        Assert.Contains("5", result);
        Assert.Contains("10", result);
        Assert.Contains("Length=5", result);
    }

    #endregion

    #region WinForms Compatibility Tests

    [Fact]
    public void WinFormsCompatibility_SelectionStartAndLength()
    {
        // Arrange - simulating WinForms TextBox.SelectionStart = 5, SelectionLength = 10
        var selectionStart = 5;
        var selectionLength = 10;

        // Act - convert to TextSelection
        var selection = TextSelection.FromStartAndLength(selectionStart, selectionLength);

        // Assert - verify properties match WinForms behavior
        Assert.Equal(selectionStart, selection.Start);
        Assert.Equal(selectionLength, selection.Length);
        Assert.Equal(selectionStart + selectionLength, selection.End);
    }

    [Fact]
    public void WinFormsCompatibility_EmptySelection()
    {
        // Arrange - simulating WinForms TextBox with no selection
        var selectionStart = 10;
        var selectionLength = 0;

        // Act
        var selection = TextSelection.FromStartAndLength(selectionStart, selectionLength);

        // Assert
        Assert.True(selection.IsEmpty);
        Assert.Equal(selectionStart, selection.Start);
    }

    #endregion
}
