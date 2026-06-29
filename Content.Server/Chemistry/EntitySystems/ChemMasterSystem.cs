// SPDX-FileCopyrightText: 2021 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 Ygg01 <y.laughing.man.y@gmail.com>
// SPDX-FileCopyrightText: 2022 0x6273 <0x40@keemail.me>
// SPDX-FileCopyrightText: 2022 Chief-Engineer <119664036+Chief-Engineer@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Flipp Syder <76629141+vulppine@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Illiux <newoutlook@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DEATHB4DEFEAT <77995199+DEATHB4DEFEAT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 ElectroJr <leonsfriedrich@gmail.com>
// SPDX-FileCopyrightText: 2023 Emisse <99158783+Emisse@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Vordenburg <114301317+Vordenburg@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2024 Cojoke <83733158+Cojoke-dot@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Dora <27211909+catdotjs@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Chemistry.Components;
using Content.Server._Orion.Chemistry.Components;
using Content.Server._Orion.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems
{

    /// <summary>
    /// Contains all the server-side logic for ChemMasters.
    /// <seealso cref="ChemMasterComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly ChemMasterBeakerCapacitySystem _chemMasterBeakerCapacity = default!; // Orion
        [Dependency] private readonly LabelSystem _labelSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        private static readonly EntProtoId PillPrototypeId = "Pill";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionChangedEvent>(SubscribeUpdateUiState); // Orion
            SubscribeLocalEvent<ChemMasterComponent, SolutionTransferredEvent>(SubscribeUpdateUiState); // Orion
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState); // Orion-Edit
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSortingTypeCycleMessage>(OnCycleSortingTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
        }

        private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ChemMasterComponent> ent, bool updateLabel = false)
        {
            var (owner, chemMaster) = ent;
            // Orion-Start
            if (TryComp<ChemMasterBeakerCapacityComponent>(owner, out var beakerCapacity))
                _chemMasterBeakerCapacity.EnsureInitialized((owner, beakerCapacity));
            // Orion-End
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;
            var inputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName);
            var outputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.OutputSlotName);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;
            var bufferMaxVolume = bufferSolution.MaxVolume; // Orion

            var state = new ChemMasterBoundUserInterfaceState(
                chemMaster.Mode, chemMaster.SortingType, BuildInputContainerInfo(inputContainer), BuildOutputContainerInfo(outputContainer),
                bufferReagents, bufferCurrentVolume, chemMaster.PillType, chemMaster.PillDosageLimit, updateLabel, bufferMaxVolume); // Orion-Edit

            _userInterfaceSystem.SetUiState(owner, ChemMasterUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            chemMaster.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnCycleSortingTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSortingTypeCycleMessage message)
        {
            chemMaster.Comp.SortingType++;
            if (chemMaster.Comp.SortingType > ChemMasterSortingType.Latest)
                chemMaster.Comp.SortingType = ChemMasterSortingType.None;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnSetPillTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > SharedChemMaster.PillTypes - 1)
                return;

            chemMaster.Comp.PillType = message.PillType;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnReagentButtonMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
                return;

            switch (chemMaster.Comp.Mode)
            {
                case ChemMasterMode.Transfer:
                    TransferReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer, message.Actor); // goob - logging
                    break;
                case ChemMasterMode.Discard:
                    DiscardReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(chemMaster);
        }

        private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, EntityUid? actor = null) // goob - logging
        {
            var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
            if (container is null ||
                !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out var bufferSoln, out var bufferSolution)) // Orion-Edit
            {
                return;
            }

            // Orion-Edit-Start
            if (fromBuffer) // Buffer -> container
            {
                var removed = FixedPoint2.Min(amount, bufferSolution.GetReagentQuantity(id), containerSolution.AvailableVolume);
                if (removed <= FixedPoint2.Zero)
                    return;

                _solutionContainerSystem.RemoveReagent(bufferSoln.Value, id, removed);
                _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, removed, out _);
                amount = removed;
            }

            else // Container -> buffer
            {
                var available = FixedPoint2.Max(bufferSolution.AvailableVolume, FixedPoint2.Zero);
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id), available);

                if (amount <= FixedPoint2.Zero)
                    return;

                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
                _solutionContainerSystem.TryAddReagent(bufferSoln.Value, id, amount, out _);
            }

            if (actor.HasValue)
            {
                _adminLogger.Add(LogType.Storage,
                    LogImpact.Low,
                    $"{ToPrettyString(actor)} transferred {amount}u of {id} {(!fromBuffer ? "from" : "to")}" +
                    $" {ToPrettyString(containerSoln)} {(fromBuffer ? "from" : "to")} {ToPrettyString(chemMaster)}");
            }
            // Orion-Edit-End

            UpdateUiState(chemMaster, updateLabel: true);
        }

        private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            // Orion-Edit-Start
            if (fromBuffer)
            {
                if (_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out var bufferSoln, out var bufferSolution))
                {
                    var removed = FixedPoint2.Min(amount, bufferSolution.GetReagentQuantity(id));
                    if (removed <= FixedPoint2.Zero)
                        return;

                    _solutionContainerSystem.RemoveReagent(bufferSoln.Value, id, removed);
                }
                else
                {
                    return;
                }
            }
            // Orion-Edit-End

            UpdateUiState(chemMaster, updateLabel: fromBuffer);
        }

        private void OnCreatePillsMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterCreatePillsMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !TryComp(container, out StorageComponent? storage))
            {
                return; // output can't fit pills
            }

            // Ensure the number is valid.
            if (message.Number == 0 || !_storageSystem.HasSpace((container, storage)))
                return;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            var needed = message.Dosage * message.Number;
            if (!WithdrawFromBuffer(chemMaster, needed, user, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);

            for (var i = 0; i < message.Number; i++)
            {
                var item = Spawn(PillPrototypeId, Transform(container).Coordinates);
                _storageSystem.Insert(container, item, out _, user: user, storage);
                _labelSystem.Label(item, message.Label);

                _solutionContainerSystem.EnsureSolutionEntity(item, SharedChemMaster.PillSolutionName,out var itemSolution ,message.Dosage);
                if (!itemSolution.HasValue)
                    return;

                _solutionContainerSystem.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

                var pill = EnsureComp<PillComponent>(item);
                pill.PillType = chemMaster.Comp.PillType;
                Dirty(item, pill);

                // Log pill creation by a user
                _adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
            }

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnOutputToBottleMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterOutputToBottleMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !_solutionContainerSystem.TryGetSolution(container, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
            {
                return; // output can't fit reagents
            }

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > solution.AvailableVolume)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            if (!WithdrawFromBuffer(chemMaster, message.Dosage, user, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);
            _solutionContainerSystem.TryAddSolution(soln.Value, withdrawal);

            // Log bottle creation by a user
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private bool WithdrawFromBuffer(
            Entity<ChemMasterComponent> chemMaster,
            FixedPoint2 neededVolume, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var solution))
            {
                return false;
            }

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // ReSharper disable once InvertIf
            if (neededVolume > solution.Volume)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                return false;
            }

            outputSolution = solution.SplitSolution(neededVolume);
            return true;
        }

        private void ClickSound(Entity<ChemMasterComponent> chemMaster)
        {
            _audioSystem.PlayPvs(chemMaster.Comp.ClickSound, chemMaster, AudioParams.Default.WithVolume(-2f));
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            // Orion-Start
            if (!_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
                return null;
            // Orion-End

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            var name = Name(container.Value);
            {
                if (_solutionContainerSystem.TryGetSolution(
                        container.Value, SharedChemMaster.BottleSolutionName, out _, out var solution))
                {
                    return BuildContainerInfo(name, solution);
                }
            }

            if (!TryComp(container, out StorageComponent? storage))
                return null;

            var pills = storage.Container.ContainedEntities.Select((Func<EntityUid, (string, FixedPoint2 quantity)>) (pill =>
            {
                _solutionContainerSystem.TryGetSolution(pill, SharedChemMaster.PillSolutionName, out _, out var solution);
                var quantity = solution?.Volume ?? FixedPoint2.Zero;
                return (Name(pill), quantity);
            })).ToList();

            return new ContainerInfo(name, _storageSystem.GetCumulativeItemAreas((container.Value, storage)), storage.Grid.GetArea())
            {
                Entities = pills
            };
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }
    }
}
