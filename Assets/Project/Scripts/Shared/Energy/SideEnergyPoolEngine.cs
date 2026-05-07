namespace Project.Scripts.Shared.Energy
{
    public class SideEnergyPoolEngine
    {
        public SideEnergyPoolSnapshot Snapshot => new(_currentEnergy, _energyCap);
        public float EnergyCap => _energyCap;


        private float _energyCap;
        private float _currentEnergy;


        public SideEnergyPoolEngine(float energyCap)
        {
            _energyCap = energyCap < 0f ? 0f : energyCap;
        }

        public float AddEnergy(float amount)
        {
            if (amount <= 0f)
                return 0f;

            var before = _currentEnergy;
            _currentEnergy += amount;
            if (_energyCap > 0f && _currentEnergy > _energyCap)
                _currentEnergy = _energyCap;

            return _currentEnergy - before;
        }

        public bool CanSpend(int amount)
        {
            return amount > 0 && _currentEnergy >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (false == CanSpend(amount))
                return false;

            _currentEnergy -= amount;

            return true;
        }

        public void SetCap(float energyCap)
        {
            _energyCap = energyCap < 0f ? 0f : energyCap;
            if (_energyCap > 0f && _currentEnergy > _energyCap)
                _currentEnergy = _energyCap;
        }

        public void Reset()
        {
            _currentEnergy = 0f;
        }
    }
}