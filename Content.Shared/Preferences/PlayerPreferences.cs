// SPDX-FileCopyrightText: 2019 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2020 DamianX <DamianX@users.noreply.github.com>
// SPDX-FileCopyrightText: 2020 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2020 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2021 Leo <lzimann@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Metal Gear Sloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2021 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared._Europa.CustomGhost;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the index of the currently selected character.
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, ICharacterProfile> _characters;

        public PlayerPreferences(IEnumerable<KeyValuePair<int, ICharacterProfile>> characters, int selectedCharacterIndex, Color adminOOCColor, ProtoId<CustomGhostPrototype> ghostPrototype, List<ProtoId<ConstructionPrototype>> constructionFavorites) // Europa-Edit
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            SelectedCharacterIndex = selectedCharacterIndex;
            AdminOOCColor = adminOOCColor;
            CustomGhost = ghostPrototype; // Europa
            ConstructionFavorites = constructionFavorites;
        }

        // Europa-Start
        public PlayerPreferences WithCharacters(IEnumerable<KeyValuePair<int, ICharacterProfile>> characters) =>
            new(characters, SelectedCharacterIndex, AdminOOCColor, CustomGhost, ConstructionFavorites);

        public PlayerPreferences WithSlot(int slot) =>
            new(_characters, slot, AdminOOCColor, CustomGhost, ConstructionFavorites);

        public PlayerPreferences WithAdminOOCColor(Color adminColor) =>
            new(_characters, SelectedCharacterIndex, adminColor, CustomGhost, ConstructionFavorites);

        public PlayerPreferences WithCustomGhost(ProtoId<CustomGhostPrototype> customGhost) =>
            new(_characters, SelectedCharacterIndex, AdminOOCColor, customGhost, ConstructionFavorites);

        public PlayerPreferences WithConstructionFavorites(List<ProtoId<ConstructionPrototype>> favorites) =>
            new(_characters, SelectedCharacterIndex, AdminOOCColor, CustomGhost, favorites);
        // Europa-End

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];

        public Color AdminOOCColor { get; set; }
        public ProtoId<CustomGhostPrototype> CustomGhost { get; set; } // Europa

        /// <summary>
        ///    List of favorite items in the construction menu.
        /// </summary>
        public List<ProtoId<ConstructionPrototype>> ConstructionFavorites { get; set; } = [];

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }
    }
}
