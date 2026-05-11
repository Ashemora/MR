namespace Project.Scripts.Shared.MoveBar
{
    public class MoveBarEngine : IMoveBarEngine
    {
        private MoveBarSettings _settings;
        private float _accumulatedTime;
        private int _currentMoves;
        private bool _initialized;


        public MoveBarSnapshot Snapshot
        {
            get
            {
                if (false == _initialized)
                    return new MoveBarSnapshot(0, 0f, false);

                var isAtMax = _currentMoves >= _settings.MaxMoves;

                float fillProgress;
                if (isAtMax)
                    fillProgress = 1f;
                else if (_settings is { MaxMoves: > 0, SecondsPerMove: > 0f })
                {
                    var partial = _accumulatedTime / _settings.SecondsPerMove;
                    fillProgress = (_currentMoves + partial) / _settings.MaxMoves;
                }
                else
                    fillProgress = 0f;

                return new MoveBarSnapshot(_currentMoves, fillProgress, isAtMax);
            }
        }


        public void Initialize(MoveBarSettings settings)
        {
            _settings = settings;
            _currentMoves = settings.StartMoves;
            _accumulatedTime = 0f;
            _initialized = true;
        }

        public bool Tick(float deltaTime)
        {
            if (false == _initialized)
                return false;

            if (_currentMoves >= _settings.MaxMoves)
                return false;

            _accumulatedTime += deltaTime;

            var changed = false;

            while (_accumulatedTime >= _settings.SecondsPerMove && _currentMoves < _settings.MaxMoves)
            {
                _accumulatedTime -= _settings.SecondsPerMove;
                _currentMoves++;
                changed = true;
            }

            return changed;
        }

        public bool TryConsume()
        {
            if (_currentMoves <= 0)
                return false;

            _currentMoves--;
            
            return true;
        }
    }
}