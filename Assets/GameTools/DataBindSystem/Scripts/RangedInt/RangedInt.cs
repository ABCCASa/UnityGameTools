using System;
using UnityEngine;

namespace GameTools.DataBindSystem
{
    [Serializable]
    public struct RangedInt : IEquatable<RangedInt>, IComparable<RangedInt>
    {
        public readonly int min;
        public readonly int max;
        private int _value;
        private bool _isOutRange;
        public bool IsOutRange => _isOutRange;
        public int Value
        {
            get => _value;
            set => _isOutRange = !Clamp(value, min, max, out _value);
        }
        public RangedInt(int min, int max, int initialValue)
        {
            if (min > max)
                throw new ArgumentException("最小值不能大于最大值。");
            this.min = min;
            this.max = max;
            _isOutRange = !Clamp(initialValue, min, max, out _value);
        }
       
        /// <returns>是否在有效的范围</returns>
        private static bool Clamp(int value, int min, int max, out int output)
        {
            if (value < min)
            {
                output = min;
                return false;
            }
            if (value > max)
            {
                output = max;
                return false;
            }
            output = value;
            return true;
        }
        public static implicit operator RangedFloat(RangedInt rangedInt)
        {
            return new RangedFloat(rangedInt.min, rangedInt.max, rangedInt._value);
        }

        public static implicit operator int(RangedInt rangedInt)
        {
            return rangedInt._value;
        }
        public override readonly int GetHashCode() => HashCode.Combine(min, max, _value);
        public bool Equals(RangedInt other)=> other._value == _value && other.max == max && other.min == min;
        public override bool Equals(object obj) => obj is RangedInt other && Equals(other);
        public readonly int CompareTo(RangedInt other) => _value.CompareTo(other._value);
        public static bool operator ==(RangedInt left, RangedInt right) => left.Equals(right);
        public static bool operator !=(RangedInt left, RangedInt right) => !left.Equals(right);
        public static bool operator <(RangedInt left, RangedInt right) => left._value < right._value;
        public static bool operator >(RangedInt left, RangedInt right) => left._value > right._value;
        public static bool operator <=(RangedInt left, RangedInt right) => left._value <= right._value;
        public static bool operator >=(RangedInt left, RangedInt right) => left._value >= right._value;



    }
}