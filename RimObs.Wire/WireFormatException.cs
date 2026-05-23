using System;

namespace Cryptiklemur.RimObs.Wire;

public sealed class WireFormatException : Exception {
    public WireFormatException(string message) : base(message) {
    }
}
