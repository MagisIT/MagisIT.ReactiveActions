using System;

namespace MagisIT.ReactiveActions.Reactivity.Persistence.Models
{
    public sealed class ActionCallReference : IEquatable<ActionCallReference>
    {
        public string ActionCallId { get; set; }

        public bool IsResultSource { get; set; }

        public bool Equals(ActionCallReference other) => other != null && ActionCallId == other.ActionCallId && IsResultSource == other.IsResultSource;

        public override bool Equals(object obj) => Equals(obj as ActionCallReference);

        public override int GetHashCode() => (ActionCallId, IsResultSource).GetHashCode();

        public static bool operator ==(ActionCallReference left, ActionCallReference right) => Equals(left, right);

        public static bool operator !=(ActionCallReference left, ActionCallReference right) => !Equals(left, right);
    }
}
