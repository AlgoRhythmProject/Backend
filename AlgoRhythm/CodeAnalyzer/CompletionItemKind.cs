namespace CodeAnalyzer
{
    /// <summary>
    /// Represents the kind of a completion item in IntelliSense, following the values used by Monaco Editor.
    /// Each enum value corresponds to a specific type of symbol or code element and determines
    /// the icon and behavior shown in code completion lists.
    ///
    /// These values are used for mapping symbols from Roslyn (<see cref="Microsoft.CodeAnalysis.SymbolKind"/>)
    /// to completion items in the editor, enabling proper icons, descriptions, and sorting in IntelliSense.
    /// </summary>
    public enum CompletionItemKind
    {
        Method = 1,
        Function = 1,
        Constructor = 2,
        Field = 3,
        Variable = 4,
        Class = 5,
        Struct = 6,
        Interface = 7,
        Module = 8,
        Property = 9,
        Event = 10,
        Operator = 11,
        Unit = 12,
        Value = 13,
        Constant = 14,
        Enum = 15,
        EnumMember = 16,
        Keyword = 17,
        Text = 18,
        Color = 19,
        File = 20,
        Reference = 21,
        Folder = 23,
        TypeParameter = 24,
        User = 25,
        Issue = 26,
    }
}