using System;

namespace MagisIT.ReactiveActions.Reactivity.Persistence.Models
{
    public sealed class ActionCallReference : IEquatable<ActionCallReference>
    {
        public string ActionCallId { get; set; }

        public bool Direct { get; set; }

        public bool Equals(ActionCallReference other) => other != null && ActionCallId == other.ActionCallId && Direct == other.Direct;

        public override bool Equals(object obj) => Equals(obj as ActionCallReference);

        public override int GetHashCode() => (ActionCallId, Direct).GetHashCode();

        public static bool operator ==(ActionCallReference left, ActionCallReference right) => Equals(left, right);

        public static bool operator !=(ActionCallReference left, ActionCallReference right) => !Equals(left, right);
    }
}
