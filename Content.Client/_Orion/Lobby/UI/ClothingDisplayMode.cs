namespace Content.Client._Orion.Lobby.UI;

/// <summary>
///     How clothing should be displayed in the lobby
/// </summary>
public enum ClothingDisplayMode : byte
{
    /// <summary>
    ///     Display all clothing
    /// </summary>
    ShowAll = 0,

    /// <summary>
    ///     Display only underwear
    /// </summary>
    ShowUnderwearOnly = 1,

    /// <summary>
    ///     Hide all clothing
    /// </summary>
    HideAll = 2,
}
