using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct UnitTargetingDefinition
    {
        public UnitTargetScope Scope { get; }
        public UnitTargetRelation Relation { get; }
        public UnitTargetKind UnitKind { get; }
        public bool IncludeOwner { get; }
        public UnitTargetSelectionMode SelectionMode { get; }
        public UnitTargetFilter[] Filters => _filters ?? System.Array.Empty<UnitTargetFilter>();

        
        private readonly UnitTargetFilter[] _filters;


        public UnitTargetingDefinition(UnitTargetScope scope, UnitTargetRelation relation, UnitTargetKind unitKind,
            bool includeOwner, UnitTargetSelectionMode selectionMode, UnitTargetFilter[] filters)
        {
            Scope = scope;
            Relation = relation;
            UnitKind = unitKind;
            IncludeOwner = includeOwner;
            SelectionMode = selectionMode;
            _filters = filters ?? System.Array.Empty<UnitTargetFilter>();
        }
    }

    public readonly struct UnitTargetCandidate
    {
        public UnitDescriptor Descriptor { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public bool IsAvailable { get; }


        public UnitTargetCandidate(UnitDescriptor descriptor, int currentHP, int maxHP, bool isAvailable)
        {
            Descriptor = descriptor;
            CurrentHP = currentHP < 0 ? 0 : currentHP;
            MaxHP = maxHP < 0 ? 0 : maxHP;
            IsAvailable = isAvailable;
        }
    }
    
    
    public enum UnitTargetScope
    {
        Self,
        ByRelation,
        SelectedTarget
    }

    public enum UnitTargetRelation
    {
        Allies,
        Enemies,
        Everyone
    }

    public enum UnitTargetKind
    {
        Units,
        Heroes,
        Avatar
    }

    public enum UnitTargetSelectionMode
    {
        All
    }

    public enum UnitTargetFilter
    {
        CanDealDamage
    }
}