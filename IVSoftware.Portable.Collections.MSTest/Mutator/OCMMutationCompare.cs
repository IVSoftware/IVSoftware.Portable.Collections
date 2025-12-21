using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.MSTest.Mutator
{

    /// <summary>
    /// Compares the predicted result of the stims to the actual behavior or the LUT.
    /// </summary>
    public class OCMMutationCompare : IEquatable<OCMMutationCompare>
    {
        public OCMMutationCompare(MutationPhase phase, object? result, int countChanging, int countChanged, int countThrow)
        {
            Phase = phase;
            Result = result;
            CountChanging = countChanging;
            CountChanged = countChanged;
            CountThrow = countThrow;
        }
        public OCMMutationCompare(MutationPhase phase)
        {
            Phase = phase;
        }

        [JsonIgnore]
        public IList? List { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MutationPhase Phase { get; }
        public string ResultJSON { get; private set; } // Use SerializeResult() to set.
        public object? Result { get; internal set; }
        public int CountChanging { get; internal set; }
        public int CountChanged { get; internal set; }
        public int CountThrow { get; internal set; }

        // ----------------------------------------------------------
        // Structural equality: JSON snapshot + metrics + result.
        // ----------------------------------------------------------
        public bool Equals(OCMMutationCompare? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;

            // Leave Phase out of this!
            return
                ResultJSON == other.ResultJSON &&
                Equals(Result, other.Result) &&
                CountChanging == other.CountChanging &&
                CountChanged == other.CountChanged &&
                CountThrow == other.CountThrow;
        }

        public override bool Equals(object? obj) =>
            Equals(obj as OCMMutationCompare);

        // Leave Phase out of this!
        public override int GetHashCode() =>
            HashCode.Combine(
                ResultJSON,
                Result,
                CountChanging,
                CountChanged,
                CountThrow);

        public void SerializeResult(object? unk, Formatting formatting = Formatting.None)
            => ResultJSON = JsonConvert.SerializeObject(unk, formatting);

        internal void Set(object? result, int countChanging, int countChanged, int countThrow)
        {
            Result = result;
            CountChanging = countChanging;
            CountChanged = countChanged;
            CountThrow = countThrow;
        }
    }
}
