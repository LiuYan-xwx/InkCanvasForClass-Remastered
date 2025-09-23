using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace randomtest
{
    /// <summary>
    /// 一个通用的“抽签袋”，实现了手动重置功能。
    /// </summary>
    /// <typeparam name="T">袋中元素的类型。</typeparam>
    public class ShuffleBag<T>
    {
        private readonly List<T> _sourceItems;
        private readonly List<T> _bag = new List<T>();
        private int _currentIndex = 0;

        /// <summary>
        /// 获取袋中剩余可抽取的元素数量。
        /// </summary>
        public int RemainingCount => _bag.Count - _currentIndex;

        public ShuffleBag(IEnumerable<T> items)
        {
            _sourceItems = items.ToList();
            FillAndShuffle();
        }

        /// <summary>
        /// 从袋子中抽取下一个元素。如果袋子已空，则抛出异常。
        /// </summary>
        /// <returns>随机抽取的元素。</returns>
        public T Next()
        {
            if (_sourceItems.Count == 0)
            {
                throw new ArgumentException("抽签袋中必须至少有一个元素。");
            }
            if (RemainingCount == 0)
            {
                throw new InvalidOperationException("抽签袋已空。请在开始新一轮前调用 Reset() 方法。");
            }

            T result = _bag[_currentIndex];
            _currentIndex++;
            return result;
        }

        /// <summary>
        /// 重置抽签袋，将其重新装满并洗牌，以开始新一轮抽签。
        /// </summary>
        public void Reset()
        {
            FillAndShuffle();
        }

        private void FillAndShuffle()
        {
            _bag.Clear();
            _bag.AddRange(_sourceItems);

            int n = _bag.Count;
            while (n > 1)
            {
                n--;
                int k = RandomNumberGenerator.GetInt32(n + 1);
                (_bag[k], _bag[n]) = (_bag[n], _bag[k]);
            }
            _currentIndex = 0;
        }
    }
}
