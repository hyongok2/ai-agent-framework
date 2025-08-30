using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AIAgentFramework.Configuration.Utils
{
    /// <summary>
    /// 스레드 안전한 Set 컬렉션
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentSet<T> : ISet<T>, IReadOnlySet<T>, IDisposable where T : notnull
    {
        private readonly ConcurrentDictionary<T, byte> _dictionary;
        private readonly IEqualityComparer<T> _comparer;
        private volatile bool _disposed;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public ConcurrentSet() : this(EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// 비교자를 지정하는 생성자
        /// </summary>
        /// <param name="comparer">비교자</param>
        public ConcurrentSet(IEqualityComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _dictionary = new ConcurrentDictionary<T, byte>(comparer);
        }

        /// <summary>
        /// 컬렉션으로부터 초기화하는 생성자
        /// </summary>
        /// <param name="collection">초기 컬렉션</param>
        public ConcurrentSet(IEnumerable<T> collection) : this(collection, EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// 컬렉션과 비교자를 지정하는 생성자
        /// </summary>
        /// <param name="collection">초기 컬렉션</param>
        /// <param name="comparer">비교자</param>
        public ConcurrentSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _dictionary = new ConcurrentDictionary<T, byte>(collection.Select(item => new KeyValuePair<T, byte>(item, 0)), comparer);
        }

        /// <inheritdoc />
        public int Count => _dictionary.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public bool Add(T item)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(item);
            
            return _dictionary.TryAdd(item, 0);
        }

        /// <inheritdoc />
        void ICollection<T>.Add(T item) => Add(item);

        /// <inheritdoc />
        public bool Remove(T item)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(item);
            
            return _dictionary.TryRemove(item, out _);
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(item);
            
            return _dictionary.ContainsKey(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            ThrowIfDisposed();
            _dictionary.Clear();
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(array);
            
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("배열 크기가 충분하지 않습니다.");

            var keys = _dictionary.Keys.ToArray();
            keys.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public void ExceptWith(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            foreach (var item in other)
            {
                Remove(item);
            }
        }

        /// <inheritdoc />
        public void IntersectWith(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            var otherSet = other as ISet<T> ?? new HashSet<T>(other, _comparer);
            var toRemove = _dictionary.Keys.Where(item => !otherSet.Contains(item)).ToList();
            
            foreach (var item in toRemove)
            {
                Remove(item);
            }
        }

        /// <inheritdoc />
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            var otherSet = other as ISet<T> ?? new HashSet<T>(other, _comparer);
            return Count < otherSet.Count && IsSubsetOf(otherSet);
        }

        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            var otherSet = other as ISet<T> ?? new HashSet<T>(other, _comparer);
            return Count > otherSet.Count && IsSupersetOf(otherSet);
        }

        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            var otherSet = other as ISet<T> ?? new HashSet<T>(other, _comparer);
            return _dictionary.Keys.All(otherSet.Contains);
        }

        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            return other.All(Contains);
        }

        /// <inheritdoc />
        public bool Overlaps(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            return other.Any(Contains);
        }

        /// <inheritdoc />
        public bool SetEquals(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            var otherSet = other as ISet<T> ?? new HashSet<T>(other, _comparer);
            return Count == otherSet.Count && IsSupersetOf(otherSet);
        }

        /// <inheritdoc />
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            foreach (var item in other)
            {
                if (!Remove(item))
                {
                    Add(item);
                }
            }
        }

        /// <inheritdoc />
        public void UnionWith(IEnumerable<T> other)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(other);

            foreach (var item in other)
            {
                Add(item);
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            ThrowIfDisposed();
            return _dictionary.Keys.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 조건에 맞는 요소들을 제거합니다
        /// </summary>
        /// <param name="predicate">제거 조건</param>
        /// <returns>제거된 요소 수</returns>
        public int RemoveWhere(Func<T, bool> predicate)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(predicate);

            var toRemove = _dictionary.Keys.Where(predicate).ToList();
            var removedCount = 0;

            foreach (var item in toRemove)
            {
                if (Remove(item))
                    removedCount++;
            }

            return removedCount;
        }

        /// <summary>
        /// 요소를 안전하게 추가합니다 (이미 존재해도 예외 발생하지 않음)
        /// </summary>
        /// <param name="item">추가할 요소</param>
        /// <returns>실제로 추가되었는지 여부</returns>
        public bool TryAdd(T item)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(item);
            
            return _dictionary.TryAdd(item, 0);
        }

        /// <summary>
        /// 요소를 안전하게 제거합니다 (존재하지 않아도 예외 발생하지 않음)
        /// </summary>
        /// <param name="item">제거할 요소</param>
        /// <returns>실제로 제거되었는지 여부</returns>
        public bool TryRemove(T item)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(item);
            
            return _dictionary.TryRemove(item, out _);
        }

        /// <summary>
        /// 현재 요소들의 스냅샷을 배열로 가져옵니다
        /// </summary>
        /// <returns>요소 배열</returns>
        public T[] ToArray()
        {
            ThrowIfDisposed();
            return _dictionary.Keys.ToArray();
        }

        /// <summary>
        /// 현재 요소들의 스냅샷을 리스트로 가져옵니다
        /// </summary>
        /// <returns>요소 리스트</returns>
        public List<T> ToList()
        {
            ThrowIfDisposed();
            return _dictionary.Keys.ToList();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 리소스를 해제합니다
        /// </summary>
        /// <param name="disposing">관리 리소스 해제 여부</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _dictionary.Clear();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConcurrentSet<T>));
        }
    }
}