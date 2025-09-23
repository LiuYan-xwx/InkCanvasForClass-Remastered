using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace randomtest
{
    /// <summary>
    /// һ��ͨ�õġ���ǩ������ʵ�����ֶ����ù��ܡ�
    /// </summary>
    /// <typeparam name="T">����Ԫ�ص����͡�</typeparam>
    public class ShuffleBag<T>
    {
        private readonly List<T> _sourceItems;
        private readonly List<T> _bag = new List<T>();
        private int _currentIndex = 0;

        /// <summary>
        /// ��ȡ����ʣ��ɳ�ȡ��Ԫ��������
        /// </summary>
        public int RemainingCount => _bag.Count - _currentIndex;

        public ShuffleBag(IEnumerable<T> items)
        {
            _sourceItems = items.ToList();
            FillAndShuffle();
        }

        /// <summary>
        /// �Ӵ����г�ȡ��һ��Ԫ�ء���������ѿգ����׳��쳣��
        /// </summary>
        /// <returns>�����ȡ��Ԫ�ء�</returns>
        public T Next()
        {
            if (_sourceItems.Count == 0)
            {
                throw new ArgumentException("��ǩ���б���������һ��Ԫ�ء�");
            }
            if (RemainingCount == 0)
            {
                throw new InvalidOperationException("��ǩ���ѿա����ڿ�ʼ��һ��ǰ���� Reset() ������");
            }

            T result = _bag[_currentIndex];
            _currentIndex++;
            return result;
        }

        /// <summary>
        /// ���ó�ǩ������������װ����ϴ�ƣ��Կ�ʼ��һ�ֳ�ǩ��
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
