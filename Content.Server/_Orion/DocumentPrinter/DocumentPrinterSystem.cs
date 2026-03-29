using Content.Server.GameTicking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Shared.DocumentPrinter;
public sealed class DocumentPrinterSystem : EntitySystem
{
    const int SPACE_STATION_YEAR_OFFSET = 540; // real year + offset = station year

    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DocumentPrinterComponent, PrintingDocumentEvent>(OnPrinting);
        SubscribeLocalEvent<DocumentPrinterComponent, GetVerbsEvent<AlternativeVerb>>(AddVerbOnOff);
    }

    public void AddVerbOnOff(EntityUid uid, DocumentPrinterComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        AlternativeVerb verb = new();
        if (component.IsOnAutocomplite)
        {
            verb.Text = Loc.GetString("printer-autofill-off");
            verb.Act = () =>
        {
            component.IsOnAutocomplite = false;
            _audioSystem.PlayPvs(component.SwitchSound, uid);
        };
        }
        else
        {
            verb.Text = Loc.GetString("printer-autofill-on");
            verb.Act = () =>
        {
            component.IsOnAutocomplite = true;
            _audioSystem.PlayPvs(component.SwitchSound, uid);
        };
        }
        args.Verbs.Add(verb);
    }

    public void OnPrinting(EntityUid uid, DocumentPrinterComponent component, PrintingDocumentEvent args)
    {
        // check info from id, time and job
        if (!TryComp<PaperComponent>(args.Paper, out var paperComponent)) return;
        InventoryComponent? inventoryComponent = null;  
        TryComp(args.Actor, out inventoryComponent);

        string text = paperComponent.Content;

        if (component.IsOnAutocomplite)
        {
            MetaDataComponent? idCard = null;
            PdaComponent? pda = null;
            if (inventoryComponent is not null)
            foreach (var slot in inventoryComponent.Containers)
            {
                if (slot.ID == "id") // for checking only PDA
                {
                    TryComp(slot.ContainedEntity, out pda);
                    if (pda?.ContainedId is EntityUid idUid)
                        TryComp(idUid, out idCard);
                    break;
                }
            }
            DateTime time = DateTime.UtcNow.AddYears(SPACE_STATION_YEAR_OFFSET).AddHours(3);
            text = text.Replace("$time$", $"{_gameTiming.CurTime.Subtract(_ticker.RoundStartTimeSpan).ToString("hh\\:mm\\:ss")} / {(time.Day < 10 ? $"0{time.Day}" : time.Day)}.{(time.Month < 10 ? $"0{time.Month}" : time.Month)}.{time.Year}");
            if (pda?.StationName is not null)
            {
                text = text.Replace("Station XX-000", pda.StationName);
            }
            if (idCard is null)
            {
                text = text.Replace("$name$", "");
                text = text.Replace("$job$", "");
            }
            else
            {
                int startIndex = idCard.EntityName.IndexOf("("); int endIndex = idCard.EntityName.IndexOf(")");
                if (startIndex.Equals(-1) || endIndex.Equals(-1))
                {
                    text = text.Replace("$name$", "");
                    text = text.Replace("$job$", "");
                }
                else
                {
                    text = text.Replace("$name$", idCard.FullName ?? "");
                    text = text.Replace("$job$", idCard.LocalizedJobTitle ?? "");
                }
            }
            paperComponent.Content = text;
        }
        else
        {
            text = text.Replace("$time$", "");
            text = text.Replace("$name$", "");
            text = text.Replace("$job$", "");
            paperComponent.Content = text;
        }
    }
}
