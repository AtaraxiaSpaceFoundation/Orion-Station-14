using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.White.Jukebox;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Server.White.Jukebox;

public sealed class TapeCreatorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ServerJukeboxSongsSyncManager _songsSyncManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;


    private readonly int _recordTime = 25;

    private static string TapeCreatorContainerName = "tape_creator_container";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<JukeboxSongUploadRequest>(OnSongUploaded);
        SubscribeLocalEvent<TapeCreatorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TapeCreatorComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<TapeCreatorComponent, GetVerbsEvent<Verb>>(OnTapeCreatorGetVerb);
        SubscribeLocalEvent<TapeCreatorComponent, ComponentGetState>(OnTapeCreatorStateChanged);
        SubscribeLocalEvent<TapeComponent, ComponentGetState>(OnTapeStateChanged);
    }

    private void OnTapeCreatorGetVerb(EntityUid uid, TapeCreatorComponent component, GetVerbsEvent<Verb> ev)
    {
        if (component.Recording) return;
        if (ev.Hands == null) return;
        if (component.TapeContainer.ContainedEntities.Count == 0) return;

        var removeTapeVerb = new Verb()
        {
            Text = "Вытащить касету",
            Priority = 10000,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/remove_tape.png")),
            Act = () =>
            {
                var tapes = component.TapeContainer.ContainedEntities.ToList();
                _containerSystem.EmptyContainer(component.TapeContainer, true);

                foreach (var tape in tapes)
                {
                    _handsSystem.PickupOrDrop(ev.User, tape);
                }

                component.InsertedTape = null;
                Dirty(component);
            }
        };

        ev.Verbs.Add(removeTapeVerb);
    }

    private void OnTapeStateChanged(EntityUid uid, TapeComponent component, ref ComponentGetState args)
    {
        args.State = new TapeComponentState()
        {
            Songs = component.Songs
        };
    }

    private void OnTapeCreatorStateChanged(EntityUid uid, TapeCreatorComponent component, ref ComponentGetState args)
    {
        args.State = new TapeCreatorComponentState
        {
            Recording = component.Recording,
            CoinBalance = component.CoinBalance,
            InsertedTape = component.InsertedTape
        };
    }

    private void OnComponentInit(EntityUid uid, TapeCreatorComponent component, ComponentInit args)
    {
        component.TapeContainer = _containerSystem.EnsureContainer<Container>(uid, TapeCreatorContainerName);
    }

    private void OnInteract(EntityUid uid, TapeCreatorComponent component, InteractUsingEvent args)
    {
        if(component.Recording) return;

        if (TryComp<TapeComponent>(args.Used, out var tape))
        {
            var containedEntities = component.TapeContainer.ContainedEntities;

            if (containedEntities.Count > 1)
            {
                var removedTapes = _containerSystem.EmptyContainer(component.TapeContainer, true).ToList();
                component.TapeContainer.Insert(args.Used);

                foreach (var tapes in removedTapes)
                {
                    _handsSystem.PickupOrDrop(args.User, tapes);
                }
            }
            else
            {
                component.TapeContainer.Insert(args.Used);
            }

            component.InsertedTape = tape.Owner;
            Dirty(component);
            return;
        }

        if (_tagSystem.HasTag(args.Used, "TapeRecorderCoin"))
        {
            Del(args.Used);
            component.CoinBalance += 1;
            Dirty(component);

            return;
        }
    }

    private void OnSongUploaded(JukeboxSongUploadRequest ev)
    {
        if(!TryComp<TapeCreatorComponent>(ev.TapeCreatorUid, out var tapeCreatorComponent)) return;

        if (!tapeCreatorComponent.InsertedTape.HasValue || tapeCreatorComponent.CoinBalance <= 0)
        {
            _popupSystem.PopupEntity("Т# %ак@ э*^о сdf{ал б2я~b? Запись была прервана.", tapeCreatorComponent.Owner);
            return;
        }

        tapeCreatorComponent.CoinBalance -= 1;
        tapeCreatorComponent.Recording = true;

        var tapeComponent = Comp<TapeComponent>(tapeCreatorComponent.InsertedTape.Value);
        var songData = _songsSyncManager.SyncSongData(ev.SongName, ev.SongBytes);

        var song = new JukeboxSong()
        {
            SongName = songData.songName,
            SongPath = songData.path
        };

        tapeComponent.Songs.Add(song);
        _popupSystem.PopupEntity($"Запись началась, примерное время ожидания: {_recordTime} секунд", tapeCreatorComponent.Owner);
        Dirty(ev.TapeCreatorUid);
        Dirty(tapeComponent);
        StartRecordDelayAsync(tapeCreatorComponent, _popupSystem, _containerSystem);
    }

    private async void StartRecordDelayAsync(TapeCreatorComponent component, SharedPopupSystem popupSystem, SharedContainerSystem containerSystem)
    {
        var recordTimeDelay = _recordTime * 1000 / 10;

        await Task.Delay(1000);

        for (int i = 0; i < 10; i++)
        {
            popupSystem.PopupEntity($"Запись мозговой активности выполнена на {i * 10}%", component.Owner);
            await Task.Delay(recordTimeDelay);
        }


        containerSystem.EmptyContainer(component.TapeContainer, force: true).ToList();

        component.Recording = false;
        component.InsertedTape = null;

        popupSystem.PopupEntity($"Запись мозговой активности завершена", component.Owner);
        Dirty(component);
    }
}
