#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET5_0_OR_GREATER
#endif

#pragma warning disable CA2208
#pragma warning disable CS8632

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     Native sortedSet
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeSortedSet<T> : IDisposable, IEquatable<NativeSortedSet<T>> where T : unmanaged, IComparable<T>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSortedSetHandle
        {
            /// <summary>
            ///     Root
            /// </summary>
            public Node* Root;

            /// <summary>
            ///     Count
            /// </summary>
            public int Count;

            /// <summary>
            ///     Version
            /// </summary>
            public int Version;

            /// <summary>
            ///     Node pool
            /// </summary>
            public NativeMemoryPool NodePool;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeSortedSetHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="size">MemoryPool size</param>
        /// <param name="maxFreeSlabs">MemoryPool maxFreeSlabs</param>
        public NativeSortedSet(int size, int maxFreeSlabs)
        {
            var nodePool = new NativeMemoryPool(size, sizeof(Node), maxFreeSlabs);
            _handle = (NativeSortedSetHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeSortedSetHandle));
            _handle->Root = null;
            _handle->Count = 0;
            _handle->Version = 0;
            _handle->NodePool = nodePool;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => _handle->Count == 0;

        /// <summary>
        ///     Count
        /// </summary>
        public int Count => _handle->Count;

        /// <summary>
        ///     Min
        /// </summary>
        public T? Min
        {
            get
            {
                if (_handle->Root == null)
                    return default;
                var current = _handle->Root;
                while (current->Left != null)
                    current = current->Left;
                return current->Item;
            }
        }

        /// <summary>
        ///     Max
        /// </summary>
        public T? Max
        {
            get
            {
                if (_handle->Root == null)
                    return default;
                var current = _handle->Root;
                while (current->Right != null)
                    current = current->Right;
                return current->Item;
            }
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeSortedSet<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeSortedSet<T> nativeSortedSet && nativeSortedSet == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeSortedSet<{typeof(T).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeSortedSet<T> left, NativeSortedSet<T> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeSortedSet<T> left, NativeSortedSet<T> right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_handle == null)
                return;
            _handle->NodePool.Dispose();
            NativeMemoryAllocator.Free(_handle);
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_handle->Root != null)
            {
                var nodeStack = new NativeStack<nint>(2 * Log2(_handle->Count + 1));
                nodeStack.Push((nint)_handle->Root);
                while (nodeStack.TryPop(out var node))
                {
                    var currentNode = (Node*)node;
                    if (currentNode->Left != null)
                        nodeStack.Push((nint)currentNode->Left);
                    if (currentNode->Right != null)
                        nodeStack.Push((nint)currentNode->Right);
                    _handle->NodePool.Return(currentNode);
                }

                nodeStack.Dispose();
            }

            _handle->Root = null;
            _handle->Count = 0;
            ++_handle->Version;
        }

        /// <summary>
        ///     Add
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Added</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T item)
        {
            if (_handle->Root == null)
            {
                _handle->Root = (Node*)_handle->NodePool.Rent();
                _handle->Root->Item = item;
                _handle->Root->Left = null;
                _handle->Root->Right = null;
                _handle->Root->Color = NodeColor.Black;
                _handle->Count = 1;
                _handle->Version++;
                return true;
            }

            var current = _handle->Root;
            Node* parent = null;
            Node* grandParent = null;
            Node* greatGrandParent = null;
            _handle->Version++;
            var order = 0;
            while (current != null)
            {
                order = item.CompareTo(current->Item);
                if (order == 0)
                {
                    _handle->Root->ColorBlack();
                    return false;
                }

                if (current->Is4Node)
                {
                    current->Split4Node();
                    if (Node.IsNonNullRed(parent))
                        InsertionBalance(current, parent, grandParent, greatGrandParent);
                }

                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current;
                current = order < 0 ? current->Left : current->Right;
            }

            var node = (Node*)_handle->NodePool.Rent();
            node->Item = item;
            node->Left = null;
            node->Right = null;
            node->Color = NodeColor.Red;
            if (order > 0)
                parent->Right = node;
            else
                parent->Left = node;
            if (parent->IsRed)
                InsertionBalance(node, parent, grandParent, greatGrandParent);
            _handle->Root->ColorBlack();
            ++_handle->Count;
            return true;
        }

        /// <summary>
        ///     Add
        /// </summary>
        /// <param name="equalValue">Equal value</param>
        /// <param name="actualValue">Actual value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T equalValue, in T actualValue)
        {
            var node = FindNode(equalValue);
            if (node == null)
            {
                Add(actualValue);
            }
            else
            {
                node->Item = actualValue;
                _handle->Version++;
            }
        }

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Removed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in T item)
        {
            if (_handle->Root == null)
                return false;
            _handle->Version++;
            var current = _handle->Root;
            Node* parent = null;
            Node* grandParent = null;
            Node* match = null;
            Node* parentOfMatch = null;
            var foundMatch = false;
            while (current != null)
            {
                if (current->Is2Node)
                {
                    if (parent == null)
                    {
                        current->ColorRed();
                    }
                    else
                    {
                        var sibling = parent->GetSibling(current);
                        if (sibling->IsRed)
                        {
                            if (parent->Right == sibling)
                                parent->RotateLeft();
                            else
                                parent->RotateRight();
                            parent->ColorRed();
                            sibling->ColorBlack();
                            ReplaceChildOrRoot(grandParent, parent, sibling);
                            grandParent = sibling;
                            if (parent == match)
                                parentOfMatch = sibling;
                            sibling = parent->GetSibling(current);
                        }

                        if (sibling->Is2Node)
                        {
                            parent->Merge2Nodes();
                        }
                        else
                        {
                            var newGrandParent = parent->Rotate(parent->GetRotation(current, sibling));
                            newGrandParent->Color = parent->Color;
                            parent->ColorBlack();
                            current->ColorRed();
                            ReplaceChildOrRoot(grandParent, parent, newGrandParent);
                            if (parent == match)
                                parentOfMatch = newGrandParent;
                        }
                    }
                }

                var order = foundMatch ? -1 : item.CompareTo(current->Item);
                if (order == 0)
                {
                    foundMatch = true;
                    match = current;
                    parentOfMatch = parent;
                }

                grandParent = parent;
                parent = current;
                current = order < 0 ? current->Left : current->Right;
            }

            if (match != null)
            {
                ReplaceNode(match, parentOfMatch, parent, grandParent);
                --_handle->Count;
                _handle->NodePool.Return(match);
            }

            if (_handle->Root != null)
                _handle->Root->ColorBlack();
            return foundMatch;
        }

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="equalValue">Equal value</param>
        /// <param name="actualValue">Actual value</param>
        /// <returns>Removed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in T equalValue, out T actualValue)
        {
            if (_handle->Root == null)
            {
                actualValue = default;
                return false;
            }

            _handle->Version++;
            var current = _handle->Root;
            Node* parent = null;
            Node* grandParent = null;
            Node* match = null;
            Node* parentOfMatch = null;
            var foundMatch = false;
            while (current != null)
            {
                if (current->Is2Node)
                {
                    if (parent == null)
                    {
                        current->ColorRed();
                    }
                    else
                    {
                        var sibling = parent->GetSibling(current);
                        if (sibling->IsRed)
                        {
                            if (parent->Right == sibling)
                                parent->RotateLeft();
                            else
                                parent->RotateRight();
                            parent->ColorRed();
                            sibling->ColorBlack();
                            ReplaceChildOrRoot(grandParent, parent, sibling);
                            grandParent = sibling;
                            if (parent == match)
                                parentOfMatch = sibling;
                            sibling = parent->GetSibling(current);
                        }

                        if (sibling->Is2Node)
                        {
                            parent->Merge2Nodes();
                        }
                        else
                        {
                            var newGrandParent = parent->Rotate(parent->GetRotation(current, sibling));
                            newGrandParent->Color = parent->Color;
                            parent->ColorBlack();
                            current->ColorRed();
                            ReplaceChildOrRoot(grandParent, parent, newGrandParent);
                            if (parent == match)
                                parentOfMatch = newGrandParent;
                        }
                    }
                }

                var order = foundMatch ? -1 : equalValue.CompareTo(current->Item);
                if (order == 0)
                {
                    foundMatch = true;
                    match = current;
                    parentOfMatch = parent;
                }

                grandParent = parent;
                parent = current;
                current = order < 0 ? current->Left : current->Right;
            }

            if (match != null)
            {
                actualValue = match->Item;
                ReplaceNode(match, parentOfMatch, parent, grandParent);
                --_handle->Count;
                _handle->NodePool.Return(match);
            }
            else
            {
                actualValue = default;
            }

            if (_handle->Root != null)
                _handle->Root->ColorBlack();
            return foundMatch;
        }

        /// <summary>
        ///     Contains
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Contains</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in T item) => FindNode(item) != null;

        /// <summary>
        ///     Try to get the actual value
        /// </summary>
        /// <param name="equalValue">Equal value</param>
        /// <param name="actualValue">Actual value</param>
        /// <returns>Got</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(in T equalValue, out T actualValue)
        {
            var node = FindNode(equalValue);
            if (node != null)
            {
                actualValue = node->Item;
                return true;
            }

            actualValue = default;
            return false;
        }

        /// <summary>
        ///     Insertion balance
        /// </summary>
        /// <param name="current">Current</param>
        /// <param name="parent">Parent</param>
        /// <param name="grandParent">Grand parent</param>
        /// <param name="greatGrandParent">GreatGrand parent</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertionBalance(Node* current, Node* parent, Node* grandParent, Node* greatGrandParent)
        {
            var parentIsOnRight = grandParent->Right == parent;
            var currentIsOnRight = parent->Right == current;
            Node* newChildOfGreatGrandParent;
            if (parentIsOnRight == currentIsOnRight)
                newChildOfGreatGrandParent = currentIsOnRight ? grandParent->RotateLeft() : grandParent->RotateRight();
            else
                newChildOfGreatGrandParent = currentIsOnRight ? grandParent->RotateLeftRight() : grandParent->RotateRightLeft();
            grandParent->ColorRed();
            newChildOfGreatGrandParent->ColorBlack();
            ReplaceChildOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
        }

        /// <summary>
        ///     Replace child or root
        /// </summary>
        /// <param name="parent">Parent</param>
        /// <param name="child">Child</param>
        /// <param name="newChild">New child</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReplaceChildOrRoot(Node* parent, Node* child, Node* newChild)
        {
            if (parent != null)
                parent->ReplaceChild(child, newChild);
            else
                _handle->Root = newChild;
        }

        /// <summary>
        ///     Replace node
        /// </summary>
        /// <param name="match">Match</param>
        /// <param name="parentOfMatch">Parent of match</param>
        /// <param name="successor">Successor</param>
        /// <param name="parentOfSuccessor">Parent of successor</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReplaceNode(Node* match, Node* parentOfMatch, Node* successor, Node* parentOfSuccessor)
        {
            if (successor == match)
            {
                successor = match->Left;
            }
            else
            {
                if (successor->Right != null)
                    successor->Right->ColorBlack();
                if (parentOfSuccessor != match)
                {
                    parentOfSuccessor->Left = successor->Right;
                    successor->Right = match->Right;
                }

                successor->Left = match->Left;
            }

            if (successor != null)
                successor->Color = match->Color;
            ReplaceChildOrRoot(parentOfMatch, match, successor);
        }

        /// <summary>
        ///     Find node
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Node</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Node* FindNode(in T item)
        {
            var current = _handle->Root;
            while (current != null)
            {
                var order = item.CompareTo(current->Item);
                if (order == 0)
                    return current;
                current = order < 0 ? current->Left : current->Right;
            }

            return null;
        }

        /// <summary>
        ///     Log2
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Log2</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Log2(int value) => BitOperationsHelpers.Log2(value);

        /// <summary>
        ///     Node
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct Node
        {
            /// <summary>
            ///     Is non null red
            /// </summary>
            /// <param name="node">Node</param>
            /// <returns>Is non null red</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNonNullRed(Node* node) => node != null && node->IsRed;

            /// <summary>
            ///     Is null or black
            /// </summary>
            /// <param name="node">Node</param>
            /// <returns>Is null or black</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsNullOrBlack(Node* node) => node == null || node->IsBlack;

            /// <summary>
            ///     Item
            /// </summary>
            public T Item
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            ///     Left
            /// </summary>
            public Node* Left
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            ///     Right
            /// </summary>
            public Node* Right
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            ///     Color
            /// </summary>
            public NodeColor Color
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set;
            }

            /// <summary>
            ///     Is black
            /// </summary>
            private bool IsBlack
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Color == NodeColor.Black;
            }

            /// <summary>
            ///     Is red
            /// </summary>
            public bool IsRed
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Color == NodeColor.Red;
            }

            /// <summary>
            ///     Is 2 node
            /// </summary>
            public bool Is2Node
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => IsBlack && IsNullOrBlack(Left) && IsNullOrBlack(Right);
            }

            /// <summary>
            ///     Is 4 node
            /// </summary>
            public bool Is4Node
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => IsNonNullRed(Left) && IsNonNullRed(Right);
            }

            /// <summary>
            ///     Set color to black
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ColorBlack() => Color = NodeColor.Black;

            /// <summary>
            ///     Set color to red
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ColorRed() => Color = NodeColor.Red;

            /// <summary>
            ///     Get rotation
            /// </summary>
            /// <param name="current">Current</param>
            /// <param name="sibling">Sibling</param>
            /// <returns>Rotation</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TreeRotation GetRotation(Node* current, Node* sibling)
            {
                var currentIsLeftChild = Left == current;
                return IsNonNullRed(sibling->Left) ? currentIsLeftChild ? TreeRotation.RightLeft : TreeRotation.Right : currentIsLeftChild ? TreeRotation.Left : TreeRotation.LeftRight;
            }

            /// <summary>
            ///     Get sibling
            /// </summary>
            /// <param name="node">Node</param>
            /// <returns>Sibling</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node* GetSibling(Node* node) => node == Left ? Right : Left;

            /// <summary>
            ///     Split 4 node
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Split4Node()
            {
                ColorRed();
                Left->ColorBlack();
                Right->ColorBlack();
            }

            /// <summary>
            ///     Rotate
            /// </summary>
            /// <param name="rotation">Rotation</param>
            /// <returns>Node</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node* Rotate(TreeRotation rotation)
            {
                Node* removeRed;
                switch (rotation)
                {
                    case TreeRotation.Right:
                        removeRed = Left->Left;
                        removeRed->ColorBlack();
                        return RotateRight();
                    case TreeRotation.Left:
                        removeRed = Right->Right;
                        removeRed->ColorBlack();
                        return RotateLeft();
                    case TreeRotation.RightLeft:
                        return RotateRightLeft();
                    case TreeRotation.LeftRight:
                        return RotateLeftRight();
                    default:
                        return null;
                }
            }

            /// <summary>
            ///     Rotate left
            /// </summary>
            /// <returns>Node</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node* RotateLeft()
            {
                var child = Right;
                Right = child->Left;
                child->Left = (Node*)Unsafe.AsPointer(ref this);
                return child;
            }

            /// <summary>
            ///     Rotate left right
            /// </summary>
            /// <returns>Node</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node* RotateLeftRight()
            {
                var child = Left;
                var grandChild = child->Right;
                Left = grandChild->Right;
                grandChild->Right = (Node*)Unsafe.AsPointer(ref this);
                child->Right = grandChild->Left;
                grandChild->Left = child;
                return grandChild;
            }

            /// <summary>
            ///     Rotate right
            /// </summary>
            /// <returns>Node</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node* RotateRight()
            {
                var child = Left;
                Left = child->Right;
                child->Right = (Node*)Unsafe.AsPointer(ref this);
                return child;
            }

            /// <summary>
            ///     Rotate right left
            /// </summary>
            /// <returns>Node</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node* RotateRightLeft()
            {
                var child = Right;
                var grandChild = child->Left;
                Right = grandChild->Left;
                grandChild->Left = (Node*)Unsafe.AsPointer(ref this);
                child->Left = grandChild->Right;
                grandChild->Right = child;
                return grandChild;
            }

            /// <summary>
            ///     Merge 2 nodes
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Merge2Nodes()
            {
                ColorBlack();
                Left->ColorRed();
                Right->ColorRed();
            }

            /// <summary>
            ///     Replace child
            /// </summary>
            /// <param name="child">Child</param>
            /// <param name="newChild">New child</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReplaceChild(Node* child, Node* newChild)
            {
                if (Left == child)
                    Left = newChild;
                else
                    Right = newChild;
            }
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeSortedSet<T> Empty => new();

        /// <summary>
        ///     Get enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        ///     Enumerator
        /// </summary>
        public struct Enumerator : IDisposable
        {
            /// <summary>
            ///     NativeHashSet
            /// </summary>
            private readonly NativeSortedSet<T> _nativeSortedSet;

            /// <summary>
            ///     Version
            /// </summary>
            private readonly int _version;

            /// <summary>
            ///     Node stack
            /// </summary>
            private readonly NativeStack<nint> _nodeStack;

            /// <summary>
            ///     Current
            /// </summary>
            private Node* _currentNode;

            /// <summary>
            ///     Current
            /// </summary>
            private T _current;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeSortedSet">NativeSortedSet</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(NativeSortedSet<T> nativeSortedSet)
            {
                _nativeSortedSet = nativeSortedSet;
                _version = nativeSortedSet._handle->Version;
                _nodeStack = new NativeStack<nint>(2 * Log2(nativeSortedSet.Count + 1));
                _currentNode = null;
                _current = default;
                var node = _nativeSortedSet._handle->Root;
                while (node != null)
                {
                    var next = node->Left;
                    _nodeStack.Push((nint)node);
                    node = next;
                }
            }

            /// <summary>
            ///     Move next
            /// </summary>
            /// <returns>Moved</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_version != _nativeSortedSet._handle->Version)
                    throw new InvalidOperationException("EnumFailedVersion");
                if (!_nodeStack.TryPop(out var result))
                {
                    _currentNode = null;
                    _current = default;
                    return false;
                }

                _currentNode = (Node*)result;
                _current = _currentNode->Item;
                var node = _currentNode->Right;
                while (node != null)
                {
                    var next = node->Left;
                    _nodeStack.Push((nint)node);
                    node = next;
                }

                return true;
            }

            /// <summary>
            ///     Current
            /// </summary>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            /// <summary>
            ///     Dispose
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _nodeStack.Dispose();
        }

        /// <summary>
        ///     Node color
        /// </summary>
        private enum NodeColor : byte
        {
            Black,
            Red
        }

        /// <summary>
        ///     Tree rotation
        /// </summary>
        private enum TreeRotation : byte
        {
            Left,
            LeftRight,
            Right,
            RightLeft
        }
    }
}