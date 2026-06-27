using System;

namespace GameTools.DataBindSystem
{
    [Serializable]
    public struct RangedFloat : IEquatable<RangedFloat>, IComparable<RangedFloat>
    {
        public readonly float min;
        public readonly float max;
        private float _value;
        private bool _isOutRange;
        public bool IsOutRange => _isOutRange;
        public float Value
        {
            get => _value;
            set => _isOutRange = !Clamp(value, min, max, out _value);
        }
        public RangedFloat(float min, float max, float initialValue)
        {
            if (min > max)
                throw new ArgumentException("最小值不能大于最大值。");
            this.min = min;
            this.max = max;
            _isOutRange = !Clamp(initialValue, min, max, out _value);
        }
        public static implicit operator float(RangedFloat rangedFloat)
        {
            return rangedFloat._value;
        }
        /// <returns>是否在有效的范围</returns>
        private static bool Clamp(float value, float min, float max, out float output)
        {
            if (value < min)
            {
                output = min;
                return false;
            }
            else if (value > max)
            {
                output = max;
                return false;
            }
            output = value;
            return true;
        }
        public readonly int CompareTo(RangedFloat other) => _value.CompareTo(other._value);
        public bool Equals(RangedFloat other)=> other._value == _value && other.max == max && other.min == min;
        public override bool Equals(object obj) => obj is RangedFloat other && Equals(other);
        public static bool operator ==(RangedFloat left, RangedFloat right) => left.Equals(right);
        public static bool operator !=(RangedFloat left, RangedFloat right) => !left.Equals(right);
        public override readonly int GetHashCode() =>HashCode.Combine(min, max, _value);
        public static bool operator <(RangedFloat left, RangedFloat right) => left._value < right._value;
        public static bool operator >(RangedFloat left, RangedFloat right) => left._value > right._value;
        public static bool operator <=(RangedFloat left, RangedFloat right) => left._value <= right._value;
        public static bool operator >=(RangedFloat left, RangedFloat right) => left._value >= right._value;

    }
}