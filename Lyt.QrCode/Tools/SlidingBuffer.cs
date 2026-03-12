namespace Lyt.QrCode.Tools;

public sealed class SlidingBuffer<T>(int capacity) : IEnumerable<T> where T : class
{
    private readonly Queue<T> queue = new(capacity);

    public int Capacity  => this.queue.Capacity;

    public void Add(T item)
    {
        if (this.queue.Count == capacity)
        {
            this.queue.Dequeue();
        }

        this.queue.Enqueue(item);
    }

    public bool TryGetLast([NotNullWhen(true)] out T? result ) 
        => this.queue.TryPeek(out result) && result is not null ;

    public List<T> AllEntries => [.. this.queue];

    public IEnumerator<T> GetEnumerator() => this.queue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}