namespace RimObsTest.Fixtures;

public class ResolverTargets {
    public int Add(int a, int b) => a + b;

    public int Add(int a, int b, int c) => a + b + c;

    public T Identity<T>(T value) => value;

    public abstract class Inner {
        public abstract int Abstract();
    }
}
