using Content.Shared.White.Jukebox;

namespace Content.Client.White.Jukebox;

public sealed class ClientJukeboxSongsSyncManager : JukeboxSongsSyncManager
{
    public override void OnSongUploaded(JukeboxSongUploadNetMessage message)
    {
        ContentRoot.AddOrUpdateFile(message.RelativePath!, message.Data);
    }
}
