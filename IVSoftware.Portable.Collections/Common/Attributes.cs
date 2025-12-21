using IVSoftware.Portable.Collections.FollowContexts;
using IVSoftware.Portable.Common.Exceptions;

namespace IVSoftware.Portable.Collections.Common
{
    /// <summary>
    /// Declares a modal context.
    /// </summary>
    /// <remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class FollowAttribute : Attribute
    {
        public FollowAttribute(FollowMode mode, FollowPredicate condition)
        {
            Mode = mode;
        }
        public FollowMode Mode { get; }
        public FollowPredicate Condition { get; }
    }

    public enum FollowPredicate
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
    /// <remarks>
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
        public WhereAttribute(string binding, FollowPredicate wherePredicate) : this(wherePredicate)
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
        public WhereAttribute(Enum stdPropertyName, FollowPredicate wherePredicate) : this(wherePredicate)
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

}
