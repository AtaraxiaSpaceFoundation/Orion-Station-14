using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server.Radio;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared._Europa.TTS;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Europa.TTS;

public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedLanguageSystem _lang = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Учёные, тут странная аномалия в баре! Она уже съела мима!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled;

    private const float VoiceRangeSquared = SharedChatSystem.VoiceRange * SharedChatSystem.VoiceRange;
    private const float WhisperClearRangeSquared = SharedChatSystem.WhisperClearRange * SharedChatSystem.WhisperClearRange;

    private readonly Queue<QueuedAnnouncement> _announcementQueue = new();
    private bool _isPlayingAnnouncement;
    private TimeSpan _currentAnnouncementEndTime;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<CollectiveMindSpokeEvent>(OnCollectiveMindSpokeEvent);

        RegisterRateLimits();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        ProcessAnnouncementQueue();
    }

    private void ProcessAnnouncementQueue()
    {
        // Check if current announcement has finished
        if (_isPlayingAnnouncement && _timing.CurTime >= _currentAnnouncementEndTime)
        {
            _isPlayingAnnouncement = false;
        }

        if (!_isPlayingAnnouncement && _announcementQueue.Count > 0)
        {
            var announcement = _announcementQueue.Dequeue();
            PlayAnnouncementNow(announcement);
        }
    }

    public async void DispatchAnnouncementToAllStations(string message, EntityUid? sender = null)
    {
        if (!_isEnabled)
            return;

        var speaker = "adjutant";

        if (sender != null
            && TryComp<TTSComponent>(sender.Value, out var ttsComp)
            && ttsComp.VoicePrototypeId != null)
        {
            if (_prototypeManager.TryIndex<TTSVoicePrototype>(ttsComp.VoicePrototypeId, out var voiceProto))
                speaker = voiceProto.Speaker;
        }

        var soundData = await GenerateTTS(message, speaker, effect: "announce");
        if (soundData is null)
            return;

        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out var stationUid, out _))
        {
            var queuedAnnouncement = new QueuedAnnouncement(stationUid, soundData)
            {
                QueuedAt = _timing.CurTime
            };

            if (!_isPlayingAnnouncement)
            {
                PlayAnnouncementNow(queuedAnnouncement);
            }
            else
            {
                _announcementQueue.Enqueue(queuedAnnouncement);
            }
        }
    }

    public async void DispatchAnnouncementToOneStation(string message, EntityUid station, EntityUid? sender = null)
    {
        if (!_isEnabled)
            return;

        var speaker = "adjutant";

        if (sender != null
            && TryComp<TTSComponent>(sender.Value, out var ttsComp)
            && ttsComp.VoicePrototypeId != null)
        {
            if (_prototypeManager.TryIndex<TTSVoicePrototype>(ttsComp.VoicePrototypeId, out var voiceProto))
                speaker = voiceProto.Speaker;
        }

        var soundData = await GenerateTTS(message, speaker, effect: "announce");
        if (soundData is null)
            return;

        var queuedAnnouncement = new QueuedAnnouncement(station, soundData)
        {
            QueuedAt = _timing.CurTime
        };

        if (!_isPlayingAnnouncement)
        {
            PlayAnnouncementNow(queuedAnnouncement);
        }
        else
        {
            _announcementQueue.Enqueue(queuedAnnouncement);
        }
    }

    private void PlayAnnouncementNow(QueuedAnnouncement announcement)
    {
        var duration = GetAudioDurationFromBytes(announcement.TtsData);

        _isPlayingAnnouncement = true;
        _currentAnnouncementEndTime = _timing.CurTime + duration;

        var ev = new PlayTTSEvent(announcement.TtsData);

        if (!TryComp<StationDataComponent>(announcement.Station, out var stationDataComp))
            return;

        var receptions = _station.GetInStation(stationDataComp).Recipients;

        var commonSessions = receptions as ICommonSession[] ?? receptions.ToArray();
        if (commonSessions.Length == 0)
            return;

        foreach (var session in commonSessions)
        {
            if (session.AttachedEntity is not { } ent)
                continue;

            RaiseNetworkEvent(ev, session);
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private void OnRadioReceiveEvent(RadioSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        if (!TryComp(args.Source, out TTSComponent? senderComponent))
            return;

        var voiceId = senderComponent.VoicePrototypeId;
        if (voiceId == null)
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        HandleRadio(args.Receivers, args.Message, protoVoice);
    }

    private async void HandleRadio(EntityUid[] uids, string message, TTSVoicePrototype voicePrototype)
    {
        var soundData = await GenerateTTS(message, voicePrototype.Speaker, effect: "radio");
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, null, true), Filter.Entities(uids));
    }

    private async void OnCollectiveMindSpokeEvent(CollectiveMindSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        var speaker = "nyx_assassin_dota_2";

        var soundData = await GenerateTTS(args.Message, speaker, effect: "echo");
        if (soundData is null)
            return;

        var recipients = Filter.Entities(args.Receivers.ToArray());
        RaiseNetworkEvent(new PlayTTSEvent(soundData), recipients);
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker, effect: ev.Species == "IPC" ? "robotic" : "");
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        var effect = "";
        if (TryComp<HumanoidAppearanceComponent>(uid, out var comp))
        {
            if (comp.Species == "IPC")
                effect = "robotic";
        }

        if (args.IsWhisper)
        {
            HandleWhisper(uid, args.Message, protoVoice.Speaker, args.Language, effect);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker, args.Language, effect);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker, LanguagePrototype? language = null, string effect = "")
    {
        var soundData = await GenerateTTS(message, speaker, effect: effect);
        if (soundData is null)
            return;
        var ev = new PlayTTSEvent(soundData, GetNetEntity(uid));
        SendToLanguageSpeakersOnly(uid, ev, false, language);
    }

    private async void HandleWhisper(EntityUid uid, string message, string speaker, LanguagePrototype? language = null, string effect = "")
    {
        var fullSoundData = await GenerateTTS(message, speaker, true, effect: effect);
        if (fullSoundData is null)
            return;
        var fullTtsEvent = new PlayTTSEvent(fullSoundData, GetNetEntity(uid), true);
        SendToLanguageSpeakersOnly(uid, fullTtsEvent, true, language);
    }

    private void SendToLanguageSpeakersOnly(EntityUid uid, PlayTTSEvent ev, bool isWhisper = false, LanguagePrototype? language = null, bool notInverted = true)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;

        var commonSessions = receptions as ICommonSession[] ?? receptions.ToArray();
        if (commonSessions.Length == 0)
            return;

        var hasLanguage = language != null;

        foreach (var session in commonSessions)
        {
            if (session.AttachedEntity is not { } ent)
                continue;

            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distanceSquared = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();

            if (distanceSquared > VoiceRangeSquared == notInverted)
                continue;

            if (hasLanguage && !_lang.CanUnderstand(ent, language!))
                continue;

            if (isWhisper && distanceSquared > WhisperClearRangeSquared == notInverted)
                continue;

            RaiseNetworkEvent(ev, session);
        }
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false, string effect = "")
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
            return null;
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        return await _ttsManager.ConvertTextToSpeech(speaker, textSanitized, effect);
    }

    public sealed class QueuedAnnouncement(EntityUid station, byte[] ttsData)
    {
        public EntityUid Station { get; set; } = station;
        public byte[] TtsData { get; set; } = ttsData;
        public TimeSpan QueuedAt { get; set; }
    }

    private static TimeSpan GetAudioDurationFromBytes(byte[] oggData)
    {
        if (oggData.Length < 4 || oggData[0] != 'O' || oggData[1] != 'g' || oggData[2] != 'g' || oggData[3] != 'S')
            return TimeSpan.Zero;

        const double averageBitrate = 70500.0; // 705 кбит/с
        var durationSeconds = oggData.Length * 8.0 / averageBitrate;
        return TimeSpan.FromSeconds(durationSeconds);
    }
}
