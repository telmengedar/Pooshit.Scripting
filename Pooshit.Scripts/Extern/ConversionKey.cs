﻿using System;

namespace NightlyCode.Scripting.Extern {

    /// <summary>
    /// key used for specific conversion
    /// </summary>
    class ConversionKey {
        readonly Type sourcetype;
        readonly Type targettype;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="sourcetype"></param>
        /// <param name="targettype"></param>
        public ConversionKey(Type sourcetype, Type targettype) {
            this.sourcetype = sourcetype;
            this.targettype = targettype;
        }

        /// <summary>
        /// source type from which to convert
        /// </summary>
        public Type SourceType => sourcetype;

        /// <summary>
        /// target type to which to convert
        /// </summary>
        public Type TargetType => targettype;

        protected bool Equals(ConversionKey other) {
            return sourcetype == other.sourcetype && targettype == other.targettype;
        }

        public override bool Equals(object obj) {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != GetType()) return false;
            return Equals((ConversionKey)obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((sourcetype?.GetHashCode() ?? 0) * 397) ^ (targettype?.GetHashCode() ?? 0);
            }
        }
    }
}