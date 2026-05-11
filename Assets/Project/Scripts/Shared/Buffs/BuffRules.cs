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
                if (buff.Operation == BuffModifierOperation.AddFlat)
                    result += buff.Value;
                else if (buff.Operation == BuffModifierOperation.AddPercent)
                    result *= 1f + buff.Value / 100f;
            }

            return result;
        }

        public static int ToDisplayInt(float value)
        {
            return value <= 0f ? 0 : (int)Math.Ceiling(value);
        }
    }
}