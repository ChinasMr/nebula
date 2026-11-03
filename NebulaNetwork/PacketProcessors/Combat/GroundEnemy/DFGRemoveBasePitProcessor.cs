#region

using NebulaAPI.Packets;
using NebulaModel.Logger;
using NebulaModel.Networking;
using NebulaModel.Packets;
using NebulaModel.Packets.Combat.GroundEnemy;
using NebulaWorld;
using NebulaWorld.Combat;

#endregion

namespace NebulaNetwork.PacketProcessors.Combat.GroundEnemy;

[RegisterPacketProcessor]
public class DFGRemoveBasePitProcessor : PacketProcessor<DFGRemoveBasePitPacket>
{
    protected override void ProcessPacket(DFGRemoveBasePitPacket packet, NebulaConnection conn)
    {
        var factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
        if (factory == null) return;

        if (IsHost) // Forward and approve the active base event
        {
            // RemoveBase only deletes the core. Validate on the authoritative factory
            // before forwarding so clients cannot approve removal with stale state.
            if (!EnemyManager.CanRemoveBasePit(factory.enemySystem, packet.BaseId))
            {
                Log.Warn($"Reject DFGRemoveBasePitPacket: planet={packet.PlanetId}, base={packet.BaseId} still has buildings or units");
                return;
            }
            Multiplayer.Session.Network.SendPacketToStar(packet, factory.planet.star.id);
        }

        using (Multiplayer.Session.Combat.IsIncomingRequest.On())
        {
            //Use RemoveBase instead for it uses baseId directly
            factory.enemySystem.RemoveBase(packet.BaseId, true);
        }
        GameMain.gameScenario.NotifyOnRemovePit();
    }
}
