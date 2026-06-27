using System;
using System.Collections.Generic;


namespace GameTools.DataBindSystem
{
    public class BindableEnum<TEnum> : BindableSource<Enum> where TEnum : struct, Enum
    {
        public TEnum EnumValue
        {
            get => (TEnum)Value;
            set => Value = value;
        }

        public BindableEnum(TEnum value, Action<TEnum> onValueChange = null) :
            base(value) { }

        public BindableEnum(Func<TEnum> get, Action<TEnum> set, Action<TEnum> onValueChange = null) :
            base(() => get(), (x) => set((TEnum)x)) { }

        public static implicit operator TEnum(BindableEnum<TEnum> bindableEnum)
        {
            return bindableEnum.EnumValue;
        }
    }
}