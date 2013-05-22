using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Xml;
using Dataspace.Common.Data;
using Dataspace.Common.Statistics;
using Dataspace.Common.Utility.Dictionary;
using Common.Utility.Dictionary;
[assembly: DebuggerTypeProxy(typeof(CacheNode<,>.CacheNodeProxy),Target = typeof(CacheNode<,>))]
namespace Dataspace.Common.Utility.Dictionary
{
    internal  sealed partial class CacheNode<TKey,TValue>
    {        

        internal class CacheNodeProxy
        {
            private CacheNode<TKey, TValue> _node;

            public CacheNodeProxy(CacheNode<TKey,TValue> node)
            {
                _node = node;
            }

            public string Content
            {
                get { return _node.ToString(); }
            }
        }

        internal const int PathCount = 2;

        private  CacheNode<TKey, TValue> _parent;

        private CacheNode<TKey, TValue> _leftFix;
        private CacheNode<TKey, TValue> _rightFix;

        private readonly int[] _depth = new int[PathCount];

        private readonly WeakReference[] _leftNode = new WeakReference[PathCount];
        private readonly WeakReference[] _rightNode = new WeakReference[PathCount];

        private readonly TKey _key;

        private readonly TValue _content;

        private readonly IComparer<TKey> _comparer;

        private readonly Func<TValue, float> _probabilityCalc;

        private readonly IStatChannel _channel;

        private readonly Action _decCount;

        private readonly int _maxFixedBranchDepth;

        private float GetProbability()
        {
            return _probabilityCalc(_content);
        }

        private CacheNode<TKey, TValue> GetParent()
        {
      
            return _parent;
        }

        private void SetParent(CacheNode<TKey, TValue> parent)
        {            
            _parent = parent;
        }

        private CacheNode<TKey, TValue> GetLeftNode(int path)
        {
            if (HasLeftNode(path))
                return _leftNode[path].Target as CacheNode<TKey, TValue>;
            else return null;
        }

        private bool HasLeftNode(int path)
        {
            return _leftNode[path] != null && _leftNode[path].IsAlive;
        }

        private void SetLeftNode(CacheNode<TKey, TValue> node,int path)
        {

            if (node == null)
                _leftNode[path] = null;
            else
               _leftNode[path] = new WeakReference(node);
        }

        private CacheNode<TKey, TValue> GetRightNode(int path)
        {
            if (HasRightNode(path))
                return _rightNode[path].Target as CacheNode<TKey, TValue>;
            else return null;
        }

        private bool HasRightNode(int path)
        {
            return _rightNode[path] != null && _rightNode[path].IsAlive;
        }

        private void SetRightNode(CacheNode<TKey, TValue> node,int path)
        {
            if (node == null)
                _rightNode[path] = null;
            else
                _rightNode[path] = new WeakReference(node);
        }

        private void CreateNode(bool left, CacheNode<TKey, TValue> node, int path)
        {

            node.SetParent(this);
            node._depth[path] = _depth[path] + 1;
        
            if (left)
                SetLeftNode(node, path);
            else
            {
                SetRightNode(node, path);
            }
            try
            {
                GetParent().FixChild(this, path);
            }
            catch (NullReferenceException) //эта ошибка может случиться только два раза в корневой ноде, быстрее так
            {
            }
            node._leftFix = null;
            node._rightFix = null;
            if (node._depth[path]<=_maxFixedBranchDepth || Settings.NoCacheGarbageChecking)//исключительно для тестирования
                FixChild(node,path);
        }

        private void FixChild(CacheNode<TKey, TValue> node,int path)
        {
            if (node.Equals(GetLeftNode(path)))
                _leftFix = node;
            else if (node.Equals(GetRightNode(path)))
                _rightFix = node;         
        }

        private void UnfixChild(CacheNode<TKey, TValue> node)
        {
            if (node.Equals(_leftFix))
                _leftFix = null;
            else if (node.Equals(_rightFix))
                _rightFix = null;          
        }

        public TValue FindNode(TKey key,int path,out int depth)
        {
           
            var moreOrLess = _comparer.Compare(_key, key);
            if (moreOrLess == 0)
            {
                depth = _depth[path];
                return _content;
            }
            else if (moreOrLess > 0 && HasLeftNode(path))
                return GetLeftNode(path).FindNode(key, path, out depth);
            else if (moreOrLess < 0 && HasRightNode(path))
                return GetRightNode(path).FindNode(key, path, out depth);
            else            
            {
                depth = _depth[path];
                return default(TValue);            
        }
        }

        public IEnumerable<TKey>  GetKeys(int path)
        {
            if (HasLeftNode(path))
                foreach (var key in GetLeftNode(path).GetKeys(path))
                {
                    yield return key;
                }

            yield return _key;

            if (HasRightNode(path))
                foreach (var key in GetRightNode(path).GetKeys(path))
                {
                    yield return key;
                }
        }

      

        private bool CheckTree(int path,int maxDepth, ref int depth)
        {
            depth++;
            if (depth > maxDepth)
                return false;
            var node = GetLeftNode(path);
            bool leftGood = true;
            bool rightGood = true;
            if (node != null)
            {
               leftGood = node.CheckTree(path,maxDepth,ref depth);
            }
            node = GetRightNode(path);
            if (node != null)
            {
               rightGood = node.CheckTree(path,maxDepth, ref depth);
            }
            return leftGood && rightGood;
        }

        internal bool CheckTree(int path,int maxdepth = 500)
        {
            int depth = 0;
            return CheckTree(path, maxdepth, ref depth);
        }

        public void AddNode(CacheNode<TKey,TValue> newNode,int path,ref int depth )
        {
            CacheNode<TKey, TValue> node;
            depth++;
            var moreOrLess = _comparer.Compare(_key, newNode._key);
            if (moreOrLess == 0)
                Debug.Fail("Добавление уже существующего ключа");
            else if (moreOrLess > 0)
            {
                node = GetLeftNode(path);
                if (node != null)
                {
                    node.AddNode(newNode, path,ref depth);
                }
                else
                {
                    CreateNode(true,newNode, path);
                }
            }
            else if (moreOrLess < 0)
            {
                node = GetRightNode(path);
                if (node != null)
                {
                    node.AddNode(newNode, path, ref depth);
                }
                else
                {
                    CreateNode(false,newNode, path);
                }
            }
        }

        public void AddNode(TKey key, TValue value,int maxBranchDepth,int path)
        {
            Contract.Requires(path<PathCount);
            Contract.Assert(path < PathCount);
            var node = new CacheNode<TKey, TValue>(key, value, maxBranchDepth, _comparer, _channel, _decCount, _probabilityCalc);
            int depth = 0;
            AddNode(node,path,ref depth);
        }

        public CacheNode(TKey key, TValue element, int maxBranchDepth,
            IComparer<TKey> comparer,
            IStatChannel channel = null,
            Action decCount = null,
            Func<TValue,float> probabilityCalc = null)
        {           
            _content = element;
            _maxFixedBranchDepth = maxBranchDepth;
            _comparer = comparer;
            _key = key;
            _decCount = decCount;
            _probabilityCalc = probabilityCalc;
            _channel = channel ?? new SecondLevelCache<TKey,TValue>.MockChannel();
        }

        private void Serialize(XmlWriter writer,int path)
        {
            writer.WriteAttributeString("Key", _key.ToString());
            if (_leftFix!=null)
                writer.WriteAttributeString("LeftFix",_leftFix._key.ToString());
            if (_rightFix != null)
                writer.WriteAttributeString("RightFix", _rightFix._key.ToString());
            if (path == -1)
            {
                if (_leftFix != null)
                {
                    writer.WriteStartElement("LeftNode");
                    _leftFix.Serialize(writer, path);
                    writer.WriteEndElement();
                }
                if (_rightFix != null)
                {
                    writer.WriteStartElement("RightNode");
                    _rightFix.Serialize(writer, path);
                    writer.WriteEndElement();
                }
            }
            else 
            {
                if (HasLeftNode(path))
                {
                    writer.WriteStartElement("LeftNode");
                    GetLeftNode(path).Serialize(writer, path);
                    writer.WriteEndElement();
                }
                if (HasRightNode(path))
                {
                    writer.WriteStartElement("RightNode");
                    GetRightNode(path).Serialize(writer, path);
                    writer.WriteEndElement();
                }
            }
          
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb))
            {
                writer.WriteStartElement("Root");              
                writer.WriteStartElement("Path0");
                Serialize(writer, 0);
                writer.WriteEndElement();
                writer.WriteStartElement("Path1");
                Serialize(writer, 1);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            return sb.ToString();
        }

        ~CacheNode()
        {
         
            var parent = GetParent();
          
            if (parent!=null)
            {
                var grandparent = parent.GetParent();
                if (grandparent!=null)
                {
                    for (int i = 0; i < PathCount; i++)
                        if(!parent.HasLeftNode(i)
                        && !parent.HasRightNode(i)
                        && parent._depth[i]>_maxFixedBranchDepth)
                    {
                        grandparent.UnfixChild(parent);
                     
                    }
                
                }
            }
            if (_decCount!=null)
              _decCount();
        }
    }
}
