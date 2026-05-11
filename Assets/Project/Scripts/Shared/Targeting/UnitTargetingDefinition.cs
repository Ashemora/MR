namespace Project.Scripts.Shared.Targeting
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
}