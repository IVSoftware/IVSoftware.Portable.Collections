using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.Common.Exceptions;

namespace IVSoftware.Portable.Collections.Common
{
    /// <summary>
    /// Declares a modal context.
    /// </summary>
    /// <remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class TrackAttribute : Attribute
    {
        public TrackAttribute(TrackMode mode, WherePredicate condition)
        {
            Mode = mode;
        }
        public TrackMode Mode { get; }
        public WherePredicate Condition { get; }
    }

    public enum WherePredicate
    {
        [WherePredicate("<> 0")]
        IsNotZero,

        [WherePredicate("= 0")]
        IsZero,

        [WherePredicate("< 0")]
        IsLessThanZero,

        [WherePredicate("> 0")]
        IsGreaterThanZero,

        [WherePredicate("<= 0")]
        IsLessThanOrEqualToZero,

        [WherePredicate(">= 0")]
        IsGreaterThanOrEqualToZero,

        [WherePredicate("<> 0")]
        IsTrue,

        [WherePredicate("= 0")]
        IsFalse,
    }

    /// <summary>
    /// Declares a string-based predicate associated with an enum member.
    /// </summary>
    /// <remarks>   
    /// <see cref="WherePredicate"/>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class WherePredicateAttribute : CollectionAttribute
    {
        public WherePredicateAttribute(string predicate)
        {
            Predicate = predicate;
        }
        public string Predicate { get; }
    }

    /// <summary>
    /// Declares a string-based predicate associated with an enum member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class WhereAttribute : CollectionAttribute
    {
        private WhereAttribute(Enum wherePredicate)
        {
            if (wherePredicate.GetCustomAttribute<WherePredicateAttribute>() is { } attr)
            {
                Predicate = attr.Predicate;
            }
            else
            {
                this.ThrowHard<ArgumentException>(
                    "Missing [WherePredicate] on enum member.",
                    nameof(wherePredicate));
            }
        }
        private WhereAttribute(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
            {
                this.ThrowHard<ArgumentException>(
                    "Expr must be a non-empty string.",
                    nameof(expr));
            }
            else
            {
                Predicate = expr;
            }
        }
        public WhereAttribute(string binding, WherePredicate wherePredicate) : this(wherePredicate)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                this.ThrowHard<ArgumentException>(
                    "PropertyName must be a non-empty string.",
                    nameof(binding));
            }
            else
            {
                Binding = binding;
            }
        }
        public WhereAttribute(Enum stdPropertyName, WherePredicate wherePredicate) : this(wherePredicate)
        {
            Binding = stdPropertyName.ToString();
        }
        public WhereAttribute(string propertyName, string expr) : this(expr)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                this.ThrowHard<ArgumentException>(
                    "PropertyName must be a non-empty string.",
                    nameof(propertyName));
            }
            else
            {
                Binding = propertyName;
            }
        }
        public WhereAttribute(Enum stdPropertyName, string expr) : this(expr)
        {
            Binding = stdPropertyName.ToString();
        }
        public string Binding { get; } = null!;
        public string Predicate { get; } = null!;
        public string Expr => $"{Binding} {Predicate}";
    }

    [Flags]
    public enum VisibilityPredicateFlag
    {
        Always      = 0x0,
        Empty       = 0x1,
        Single      = Empty << 1,
        Multiple    = Single << 1,
    }

    /// <summary>
    /// Declares a member visibility based on a track context.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class VisibilityPredicateAttribute : Attribute
    {
        public VisibilityPredicateAttribute(VisibilityPredicateFlag visibility) => Visibility = visibility;

        public VisibilityPredicateFlag Visibility { get; }
    }
}
