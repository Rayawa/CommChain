namespace CommChain.App.Queueing;

/// <summary>
/// 基于手写单向链表实现的泛型队列（FIFO）。
/// 考察点（第一阶段）：链表数据结构、指针操作、动态内存管理。
/// 不使用 BCL 的 Queue&lt;T&gt; 或 LinkedList&lt;T&gt;，以体现链表实现细节。
/// </summary>
public sealed class LinkedListQueue<T>
{
    /// <summary>链表节点。</summary>
    private sealed class Node
    {
        public T Value = default!;   // 由对象初始化器赋值，构造时不要求非空
        public Node? Next;
    }

    private Node? _head;   // 队头（出队端）
    private Node? _tail;   // 队尾（入队端）

    /// <summary>当前队列中的元素个数。</summary>
    public int Count { get; private set; }

    /// <summary>队列是否为空。</summary>
    public bool IsEmpty => _head is null;

    /// <summary>入队：在队尾追加新节点。</summary>
    public void Enqueue(T item)
    {
        var node = new Node { Value = item };
        if (_tail is null)
        {
            // 空队列：头尾都指向新节点
            _head = node;
            _tail = node;
        }
        else
        {
            _tail.Next = node;
            _tail = node;
        }
        Count++;
    }

    /// <summary>出队：移除并返回队头元素。</summary>
    /// <exception cref="InvalidOperationException">队列为空时抛出。</exception>
    public T Dequeue()
    {
        if (_head is null)
            throw new InvalidOperationException("队列为空，无法出队。");

        T value = _head.Value;
        _head = _head.Next;
        if (_head is null)
            _tail = null;   // 队空后尾指针一并清空，便于复用
        Count--;
        return value;
    }

    /// <summary>查看队头元素（不出队）。</summary>
    public T Peek()
    {
        if (_head is null)
            throw new InvalidOperationException("队列为空，无法查看队头。");
        return _head.Value;
    }

    /// <summary>把当前队列内容浅拷贝为 List，便于遍历显示（不改变队列）。</summary>
    public List<T> ToList()
    {
        var result = new List<T>();
        for (var node = _head; node is not null; node = node.Next)
            result.Add(node.Value);
        return result;
    }
}
