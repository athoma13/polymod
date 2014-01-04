using Polymod.Fluent;
using Polymod.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public interface IPropertyInterceptor
    {
        void Set(IProxy proxy, object propertyValue);
        object Get(IProxy proxy);
    }

    public class InterceptorRegistry
    {
        public InterceptorRegistry()
        {
            Data = new Dictionary<string, IPropertyInterceptor>();
        }

        public IPropertyInterceptor this[string name]
        {
            get { return Data[name]; }
            set { Data[name] = value; }
        }
        public Dictionary<string, IPropertyInterceptor> Data { get; private set; }

        public void Wrap(Func<string, IPropertyInterceptor, IPropertyInterceptor> func)
        {
            var tmp = new Dictionary<string, IPropertyInterceptor>();
            foreach (var kp in Data)
            {
                tmp[kp.Key] = func(kp.Key, kp.Value);
            }
            foreach (var kp in tmp)
            {
                Data[kp.Key] = kp.Value;
            }


        }
        public void Wrap(Func<IPropertyInterceptor, IPropertyInterceptor> func)
        {
            Wrap((name, interceptor) => func(interceptor));
        }


    }

    public class InterceptorAspectBuilder : IAspectBuilder
    {
        Func<PropertyInfo, IPropertyInterceptor> _propertyInterceptorCreator;

        public InterceptorAspectBuilder()
        {
            //SpiderPropertyInterceptor will create (lazy loaded) proxies hanging off the current branch.
            _propertyInterceptorCreator = p => new SpiderProperyInterceptor(ExpressionHelper.CreateInterceptor(p));
        }

        /// <summary>
        /// Creates InterceptorAspectBuilder
        /// </summary>
        /// <param name="propertyInterceptorCreator">Creates a PropertyInterceptor for all properties of the target class</param>
        public InterceptorAspectBuilder(Func<PropertyInfo, IPropertyInterceptor> propertyInterceptorCreator)
        {
            if (propertyInterceptorCreator == null) throw new ArgumentNullException("propertyInterceptorCreator");
            _propertyInterceptorCreator = propertyInterceptorCreator;
        }


        public void Build(TypeBuilder typeBuilder, IDictionary<string, object> proxyState)
        {
            var interceptors = proxyState.GetOrCreateDefault(States.InterceptorRegistry);
            foreach (var property in typeBuilder.TargetType.GetProperties())
            {
                interceptors[property.Name] = _propertyInterceptorCreator(property);
                CreateProperty(typeBuilder.InnerTypeBuilder, property.Name, property.CanWrite);
            }
        }

        private void CreateProperty(System.Reflection.Emit.TypeBuilder tb, string name, bool isWriteable)
        {
            var returnType = typeof(object);
            var pb = tb.DefineProperty(name, PropertyAttributes.None, returnType, null);

            var methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the "get" accessor method.
            var mbGet = tb.DefineMethod("get_" + name, methodAttributes, returnType, Type.EmptyTypes);

            var mbGetIl = mbGet.GetILGenerator();

            mbGetIl.Emit(OpCodes.Nop);
            mbGetIl.Emit(OpCodes.Ldarg_0);
            mbGetIl.Emit(OpCodes.Ldstr, name);
            mbGetIl.Emit(OpCodes.Call, Ex.Method(() => ILHelper.GetPropertyValue(null, "")));
            mbGetIl.Emit(OpCodes.Nop);
            mbGetIl.Emit(OpCodes.Ret);
            pb.SetGetMethod(mbGet);


            if (isWriteable)
            {
                // Define the "set" accessor method.
                var mbSet = tb.DefineMethod("set_" + name, methodAttributes, null, new Type[] { returnType });

                var mbSetIl = mbSet.GetILGenerator();

                mbSetIl.Emit(OpCodes.Nop);
                mbSetIl.Emit(OpCodes.Ldarg_0);
                mbSetIl.Emit(OpCodes.Ldstr, name);
                mbSetIl.Emit(OpCodes.Ldarg_1);
                mbSetIl.Emit(OpCodes.Call, Ex.Method(() => ILHelper.SetPropertyValue(null, "", null)));
                mbSetIl.Emit(OpCodes.Nop);
                mbSetIl.Emit(OpCodes.Ret);

                pb.SetSetMethod(mbSet);
            }
        }
    }

    public class SpiderProperyInterceptor : IPropertyInterceptor
    {
        private IPropertyInterceptor _inner;
        private readonly object _synclock = new object();
        private volatile bool _hasResult;
        private object _result;

        public SpiderProperyInterceptor(IPropertyInterceptor inner)
        {
            if (inner == null) throw new ArgumentNullException("inner");
            _inner = inner;
        }

        public void Set(IProxy proxy, object propertyValue)
        {
            _hasResult = false;
            _result = null;
            _inner.Set(proxy, propertyValue);
        }

        public object Get(IProxy proxy)
        {
            //TODO: Review locking, when I check for _hasResult, there is a chance that _result is set to null.
            //Use a SimpleLock - with Read checks?!
            if (_hasResult) return _result;

            var proxyBuilder = proxy.State.Get(States.ProxyBuilder);
            lock (_synclock)
            {
                if (_hasResult) return _result;
                var target = _inner.Get(proxy);
                IEnumerable enumerableTarget;

                if (TryMakeEnumerable(target, out enumerableTarget))
                {
                    _result = new SpiderObservableCollection(target, proxy);
                }
                else
                {
                    _result = GetWrappedObject(target, proxyBuilder);
                }

                _hasResult = true;
                return _result;
            }
        }

        private object GetWrappedObject(object target, ProxyBuilder proxyBuilder)
        {
            //TODO: Pass the current state to 'Build'
            if (ReferenceEquals(null, target)) return null;
            if (proxyBuilder.IsProxyCandidate(target.GetType())) return proxyBuilder.Build(target);

            return target;
        }

        /// <summary>
        /// Returns true if target should be made into a collection of proxies rather than a single proxy reference.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool TryMakeEnumerable(object target, out IEnumerable result)
        {
            result = null;
            if (object.ReferenceEquals(null, target)) return false;
            if (target.GetType() == typeof(string)) return false;
            result = target as IEnumerable;
            if (object.ReferenceEquals(null, result)) return false;
            return true;
        }
    }

    public class SpiderObservableCollection : ObservableCollection<object>, IRaisePropertyChanged
    {
        object _target;

        Array _targetAsArray;
        IList _targetAsList;
        ICollection _targetAsCollection;

        private readonly IProxy _parent;
        readonly object _synclock = new object();

        Action<int, object> _insertItem = (i, t) => { throw new NotSupportedException(); };
        Action<int> _removeItem = i => { throw new NotSupportedException(); };
        Action _clearItems = () => { throw new NotSupportedException(); };
        Action<int, object> _setItem = (i, t) => { throw new NotSupportedException(); };

        public SpiderObservableCollection(object target, IProxy parent)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (target == null) throw new ArgumentNullException("value");

            _target = target;
            _parent = parent;

            if (target.GetType().IsArray)
            {
                _targetAsArray = (Array)target;
                _setItem = (i, t) => _targetAsArray.SetValue(t, i);
            }
            else if (typeof(IList).IsAssignableFrom(target.GetType()))
            {
                _targetAsList = (IList)_target;
                _setItem = (i, t) => _targetAsList[i] = t;
                _insertItem = (i, t) => _targetAsList.Insert(i, t);
                _removeItem = i => _targetAsList.RemoveAt(i);
                _clearItems = () => _targetAsList.Clear();
            }

            var enumerable = (IEnumerable)target;
            var proxyBuilder = parent.State.Get(States.ProxyBuilder);
            foreach (var o in enumerable)
            {
                this.Items.Add(proxyBuilder.Build(o));
            }
        }


        public void Add(object item)
        {
            if (ReferenceEquals(null, item))
            {
                base.Add(null);
                return;
            }
            var proxyBuilder = _parent.State.Get(States.ProxyBuilder);
            var proxy = item;
            if (proxyBuilder.IsProxyCandidate(item.GetType())) proxy = proxyBuilder.Build(proxy);
            base.Add(proxy);
        }

        private void CheckBaseReentrancy()
        {
#if !SILVERLIGHT
            base.CheckReentrancy();
#endif
        }

        #region Overrides
        protected override void ClearItems()
        {
            CheckBaseReentrancy();
            _clearItems();
            base.ClearItems();
        }
        protected override void InsertItem(int index, object item)
        {
            CheckBaseReentrancy();

            var itemAsProxy = item as IProxy;
            if (itemAsProxy != null) _insertItem(index, itemAsProxy.Target);
            else _insertItem(index, item);

            base.InsertItem(index, item);
        }
        protected override void RemoveItem(int index)
        {
            CheckBaseReentrancy();
            _removeItem(index);
            base.RemoveItem(index);
        }
        protected override void SetItem(int index, object item)
        {
            CheckBaseReentrancy();

            var itemAsProxy = item as IProxy;
            if (itemAsProxy != null) _setItem(index, itemAsProxy.Target);
            else _setItem(index, item);

            base.SetItem(index, item);
        }

        //protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    base.OnCollectionChanged(e);
        //    ((INotify)this).NotifyChanged("[]");
        //}
        #endregion

        //public bool Contains(T item)
        //{
        //    if (item == null) throw new System.ArgumentNullException("item");
        //    return AsModels.Any(o => item.Equals(o.GetValue()));
        //}

        //public ICollection<IModel<T>> AsModels
        //{
        //    get
        //    {
        //        return this;
        //    }
        //}

        //public void CopyTo(T[] array, int arrayIndex)
        //{
        //    for (int i = arrayIndex; i < this.Count && i < array.Length; i++)
        //    {
        //        array[i] = this[i].GetValue();
        //    }
        //}

        //public bool Remove(T item)
        //{
        //    if (item == null) throw new System.ArgumentNullException("item");
        //    int index = 0;
        //    for (; index < this.Count; index++)
        //    {
        //        if (item.Equals(this[index])) break;
        //    }
        //    if (index >= this.Count) return false;
        //    this.RemoveAt(index);
        //    return true;
        //}



        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets Synclock
        /// </summary>
        public object Synclock
        {
            get { return _synclock; }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CollectionWrapper<T> : IList<T>
    {
        IList<object> _innerCollection;

        public CollectionWrapper(IList<object> innerCollection)
        {
            if (innerCollection == null) throw new ArgumentNullException("innerCollection");
            _innerCollection = innerCollection;
        }

        public void Add(T item)
        {
            _innerCollection.Add(item);
        }

        public void Clear()
        {
            _innerCollection.Clear();
        }

        public bool Contains(T item)
        {
            return _innerCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach(var item in _innerCollection)
            {
                array[arrayIndex] = (T)item;
                arrayIndex++;
            }
        }

        public int Count
        {
            get { return _innerCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return _innerCollection.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return _innerCollection.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _innerCollection)
            {
                yield return (T)item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _innerCollection.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _innerCollection.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _innerCollection.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return (T)_innerCollection[index];
            }
            set
            {
                _innerCollection[index] = value;
            }
        }
    }
}
