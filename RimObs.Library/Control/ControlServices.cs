namespace Cryptiklemur.RimObs.Library.Control;

internal static class ControlServices {
    private static ControlOpQueue? s_Queue;

    public static ControlOpQueue Queue => s_Queue ??= new ControlOpQueue();

    internal static void ResetForTests() {
        s_Queue = null;
    }
}
