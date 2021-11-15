namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Data structure for player peers in a networked SnowballFight session
    /// </summary>
    public struct PlayerData
    {
        public PlayerData(bool isSelf, bool isHost, PlayerBehaviour behaviour)
        {
            IsSelf = isSelf;
            IsHost = isHost;
            Behaviour = behaviour;
        }

        public bool IsSelf { get; }
        public bool IsHost { get; }
        public PlayerBehaviour Behaviour { get; }
        public string Name { get { return Behaviour != null ? Behaviour.Name : null; } }
        public bool IsReady { get { return Behaviour != null && Behaviour.IsReady; } }

        public override string ToString()
        {
            return string.Format("isSelf={0}, isHost={1}, hasBehaviour={2}, name={3}, isReady={4}",
                IsSelf, IsHost, Behaviour != null, Name, IsReady);
        }
    }
}
