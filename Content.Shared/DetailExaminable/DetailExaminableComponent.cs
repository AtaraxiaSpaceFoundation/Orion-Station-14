// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Milon <milonpl.git@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Preferences;
using Robust.Shared.GameStates;

namespace Content.Shared.DetailExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DetailExaminableComponent : Component
{
    [DataField, AutoNetworkedField] // Orion-Edit | Removed: "required: true"
    public string Content = string.Empty;

    // Orion-Start
    [DataField, AutoNetworkedField]
    public string CharacterContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string OOCContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string TagsContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string LinksContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string GreenContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string YellowContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string RedContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NsfwContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NsfwOOCContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NsfwLinksContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NsfwTagsContent { get; set; } = string.Empty;

    public void SetProfile(HumanoidCharacterProfile profile)
    {
        Content = profile.FlavorText;
        CharacterContent = profile.CharacterFlavorText;
        OOCContent = profile.OocFlavorText;
        TagsContent = profile.TagsFlavorText;
        LinksContent = profile.LinksFlavorText;
        GreenContent = profile.GreenFlavorText;
        YellowContent = profile.YellowFlavorText;
        RedContent = profile.RedFlavorText;
        NsfwContent = profile.NsfwFlavorText;
        NsfwOOCContent = profile.NsfwOOCFlavorText;
        NsfwLinksContent = profile.NsfwLinksFlavorText;
        NsfwTagsContent = profile.NsfwTagsFlavorText;
    }
    // Orion-End
}
