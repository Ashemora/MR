using System;

namespace Project.Scripts.Shared.Buffs
{
    public static class BuffRules
    {
        public static float Apply(float currentValue, BuffDefinition buff, int stackCount)
        {
            if (stackCount <= 0 || false == buff.IsConfigured)
                return currentValue;

            var result = currentValue;
            for (var i = 0; i < stackCount; i++)
            {
                if (buff.Operation == ValueModifierOperation.AddFlat)
                    result += buff.Value;
                else if (buff.Operation == ValueModifierOperation.AddPercent)
                    result *= 1f + buff.Value / 100f;
            }

            return result;
        }

        public static int ToDisplayInt(float value)
        {
            return value <= 0f ? 0 : (int)Math.Ceiling(value);
        }

        public static int ResolveAdditiveValue(ValueModifierOperation operation, float value, int baseValue)
        {
            if (baseValue <= 0 || value <= 0f)
                return 0;

            return operation == ValueModifierOperation.AddPercent
                ? ToDisplayInt(baseValue * value / 100f)
                : ToDisplayInt(value);
        }
    }
}