using System;
using System.Collections.Generic;

namespace Project.Scripts.Shared.Bot
{
    public readonly struct BotDecisionRequest
    {
        public IReadOnlyList<BotActionCandidate> Candidates => _candidates ?? Array.Empty<BotActionCandidate>();
        public BotUtilityProfile Profile { get; }
        public BotDecisionQualitySettings Quality { get; }


        private readonly BotActionCandidate[] _candidates;


        public BotDecisionRequest(IReadOnlyList<BotActionCandidate> candidates, BotUtilityProfile profile,
            BotDecisionQualitySettings quality)
        {
            Profile = profile;
            Quality = quality;
            _candidates = CopyCandidates(candidates);
        }

        private static BotActionCandidate[] CopyCandidates(IReadOnlyList<BotActionCandidate> candidates)
        {
            if (null == candidates || candidates.Count == 0)
                return Array.Empty<BotActionCandidate>();

            var result = new BotActionCandidate[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
                result[i] = candidates[i];

            return result;
        }
    }
}