using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Bot
{
    public sealed class BotUtilityDecisionEngine
    {
        private readonly Random _rng;


        public BotUtilityDecisionEngine(int seed)
        {
            _rng = new Random(seed);
        }

        public bool TryChoose(BotDecisionRequest request, out BotDecision decision)
        {
            decision = default;
            var candidates = request.Candidates;
            if (candidates.Count == 0)
                return false;

            var scored = ScoreCandidates(candidates, request.Profile, request.Quality);
            if (scored.Count == 0)
                return false;

            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (scored[0].Score < request.Quality.MinScoreToAct)
                return false;

            var picked = ShouldMakeMistake(request.Quality)
                ? scored[_rng.Next(scored.Count)]
                : PickFromTop(scored, request.Quality);

            decision = new BotDecision(picked.Candidate.Source, picked.Candidate.Target, picked.Score);

            return true;
        }

        private List<ScoredCandidate> ScoreCandidates(IReadOnlyList<BotActionCandidate> candidates,
            BotUtilityProfile profile, BotDecisionQualitySettings quality)
        {
            var result = new List<ScoredCandidate>(candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var score = Score(candidate, profile);
                if (quality.RandomNoise > 0f)
                    score += RollNoise(quality.RandomNoise);

                if (score <= 0f)
                    continue;

                result.Add(new ScoredCandidate(candidate, score));
            }

            return result;
        }

        private float Score(BotActionCandidate candidate, BotUtilityProfile profile)
        {
            if (candidate.DirectActionKind == DirectActionKind.Damage)
                return ScoreDamage(candidate, profile);
            if (candidate.DirectActionKind == DirectActionKind.Heal)
                return ScoreHeal(candidate, profile);
            if (candidate.DirectActionKind == DirectActionKind.Resurrect)
                return ScoreResurrect(candidate, profile);
            if (candidate.ActionType == UnitActionType.SupportAlly)
                return profile.SupportPreference;

            return profile.SupportPreference * 0.5f;
        }

        private static float ScoreDamage(BotActionCandidate candidate, BotUtilityProfile profile)
        {
            var score = profile.DamagePreference;
            if (candidate.Target.Kind == UnitKind.Avatar && candidate.Target.Side == BattleSide.Player
                                                  && candidate.TargetIsExposed)
                score += profile.AttackExposedAvatarWeight;

            if (candidate.WouldBreakDefense)
                score += profile.BreakDefenseWeight;

            if (candidate.TargetMaxHP > 0 && candidate.TargetCurrentHP > 0)
            {
                var missingFraction = 1f - (float)candidate.TargetCurrentHP / candidate.TargetMaxHP;
                score += missingFraction * profile.FinishEnemyWeight;

                if (candidate.ActionValue >= candidate.TargetCurrentHP)
                    score += profile.FinishEnemyWeight;

                var overkill = candidate.ActionValue - candidate.TargetCurrentHP;
                if (overkill > 0 && candidate.ActionValue > 0)
                    score -= (float)overkill / candidate.ActionValue * profile.AvoidOverkillWeight;
            }

            return score;
        }

        private static float ScoreHeal(BotActionCandidate candidate, BotUtilityProfile profile)
        {
            var score = profile.HealPreference;
            if (candidate.TargetMaxHP > 0)
            {
                var missingHp = candidate.TargetMaxHP - candidate.TargetCurrentHP;
                var missingFraction = missingHp <= 0 ? 0f : (float)missingHp / candidate.TargetMaxHP;
                score += missingFraction * profile.HealLowHpAllyWeight;

                var overheal = candidate.ActionValue - missingHp;
                if (overheal > 0 && candidate.ActionValue > 0)
                    score -= (float)overheal / candidate.ActionValue * profile.AvoidOverhealWeight;
            }

            if (candidate.Target.Kind == UnitKind.Avatar && candidate.Target.Side == BattleSide.Enemy)
                score += profile.ProtectOwnAvatarWeight;

            return score;
        }

        private static float ScoreResurrect(BotActionCandidate candidate, BotUtilityProfile profile)
        {
            return profile.ResurrectPreference + profile.ResurrectAllyWeight;
        }

        private bool ShouldMakeMistake(BotDecisionQualitySettings quality)
        {
            return quality.MistakeChance > 0f && _rng.NextDouble() < quality.MistakeChance;
        }

        private ScoredCandidate PickFromTop(IReadOnlyList<ScoredCandidate> scored, BotDecisionQualitySettings quality)
        {
            var topCount = Math.Min(quality.TopCandidateCount, scored.Count);
            if (topCount <= 1)
                return scored[0];

            var totalWeight = 0d;
            var weights = new double[topCount];
            var temperature = quality.Temperature <= 0f ? 1f : quality.Temperature;
            for (var i = 0; i < topCount; i++)
            {
                weights[i] = Math.Exp(scored[i].Score / temperature);
                totalWeight += weights[i];
            }

            var roll = _rng.NextDouble() * totalWeight;
            var cumulative = 0d;
            for (var i = 0; i < topCount; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return scored[i];
            }

            return scored[0];
        }

        private float RollNoise(float magnitude)
        {
            return (float)(_rng.NextDouble() * 2d - 1d) * magnitude;
        }

        private readonly struct ScoredCandidate
        {
            public BotActionCandidate Candidate { get; }
            public float Score { get; }


            public ScoredCandidate(BotActionCandidate candidate, float score)
            {
                Candidate = candidate;
                Score = score;
            }
        }
    }
}