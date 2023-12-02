namespace Fantasy;

public static class ObjectHelper
{
    public static void Swap<T>(ref T t1, ref T t2)
    {
        (t1, t2) = (t2, t1);
    }

    public static T[] Reverse<T>(this T[] arr)
    {
        Array.Reverse(arr);
        return arr;
    }

    public static void DisposeClear<T>(this List<T> arr) where T : IDisposable
    {
        if (arr == null)
        {
            return;
        }

        foreach (var dis in arr)
        {
            dis.Dispose();
        }

        arr.Clear();
    }

    public static T[] Clone<T>(this T[] arr)
    {
        return Clone(new List<T>(arr)).ToArray();
    }

    public static List<T> Clone<T>(this List<T> list)
    {
        List<T> copy = new List<T>(list);
        return copy;
    }
}