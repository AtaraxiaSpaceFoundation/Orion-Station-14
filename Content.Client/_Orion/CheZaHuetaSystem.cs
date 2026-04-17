namespace Content.Client.White;

//Система со смешным названием, чье предназначение заключается лишь в одном - отправке нетворк ивентов.
public sealed class CheZaHuetaSystem : EntitySystem
{
    public void SendNetMessage(EntityEventArgs message)
    {
        RaiseNetworkEvent(message);
    }
}
