namespace Project.Scripts.Shared.GroupDefense
{
    public readonly struct AvatarDefenseSnapshot
    {
        public bool IsGroup1Destroyed { get; }
        public bool IsGroup2Destroyed { get; }
        public bool IsExposed => IsGroup1Destroyed || IsGroup2Destroyed;


        public AvatarDefenseSnapshot(bool isGroup1Destroyed, bool isGroup2Destroyed)
        {
            IsGroup1Destroyed = isGroup1Destroyed;
            IsGroup2Destroyed = isGroup2Destroyed;
        }
    }
}