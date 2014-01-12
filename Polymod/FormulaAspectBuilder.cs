using Polymod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public class FormulaAspectBuilder : IAspectBuilder
    {
        private readonly Dictionary<Type, List<IFormulaBuilder>> _formulaBuilders = new Dictionary<Type, List<IFormulaBuilder>>();

        public void Build(TypeBuilder typeBuilder, StateBag proxyState)
        {
            List<IFormulaBuilder> formulaBuilders;
            if (_formulaBuilders.TryGetValue(typeBuilder.TargetType, out formulaBuilders))
            {
                foreach (var formulaBuilder in formulaBuilders)
                {
                    formulaBuilder.Build(typeBuilder, proxyState);
                }
            }
        }

        public ForFormulaNode<TSource> For<TSource>()
        {
            return new ForFormulaNode<TSource>(this);
        }

        internal void AddFormulaBuilder(Type type, IFormulaBuilder formulaBuilder)
        {
            List<IFormulaBuilder> tmp;
            if (!_formulaBuilders.TryGetValue(type, out tmp))
            {
                tmp = new List<IFormulaBuilder>();
                _formulaBuilders[type] = tmp;
            }
            tmp.Add(formulaBuilder);
        }

    }

    public class ForFormulaNode<TSource>
    {
        private FormulaAspectBuilder _formulaAspectBuilder;

        internal ForFormulaNode(FormulaAspectBuilder formulaAspectBuilder)
        {
            _formulaAspectBuilder = formulaAspectBuilder;
        }

        public IFormula<TSource, TValue> AddProperty<TValue>(string name, bool isReadonly = false)
        {
            _formulaAspectBuilder.AddFormulaBuilder(typeof(TSource), new PropertyFormulaBuilder(name, typeof(TValue), isReadonly));
            return new Formula<TSource, TValue>(name);
        }

        public IFormula<TSource, TValue> AddFormula<TValue>(string name, Expression<Func<TSource, TValue>> expression)
        {
            _formulaAspectBuilder.AddFormulaBuilder(typeof(TSource), new CalculatedFormulaBuilder(name, expression));
            return new Formula<TSource, TValue>(name);
        }


    }

    internal class PropertyFormulaInterceptor : IPropertyInterceptor
    {
        private Delegate _setter;
        private Delegate _getter;
        private readonly object _lock = new object();
        private volatile bool _isGetterInitialized;
        private volatile bool _isSetterInitialized;
        private string _propertyName;


        public PropertyFormulaInterceptor(string propertyName)
        {
            _propertyName = propertyName;
        }

        private void EnsureGetter(object proxy)
        {
            if (_isGetterInitialized) return;
            lock (_lock)
            {
                if (_isGetterInitialized) return;
                var propertyInfo = GetPropertyInfo(proxy);
                if (!propertyInfo.CanRead) throw new InvalidOperationException(string.Format("Property {0} on type {1} cannot be read from.", propertyInfo.Name, propertyInfo.DeclaringType));
                _getter = ExpressionHelper.CreateGetter(propertyInfo);
                _isGetterInitialized = true;
            }
        }
        private void EnsureSetter(object proxy)
        {
            if (_isSetterInitialized) return;
            lock (_lock)
            {
                if (_isSetterInitialized) return;
                var propertyInfo = GetPropertyInfo(proxy);
                if (!propertyInfo.CanWrite) throw new InvalidOperationException(string.Format("Property {0} on type {1} is readonly.", propertyInfo.Name, propertyInfo.DeclaringType));
                _setter = ExpressionHelper.CreateSetter(propertyInfo);
                _isSetterInitialized = true;
            }
        }

        private PropertyInfo GetPropertyInfo(object proxy)
        {
            var propertyInfo = proxy.GetType().GetProperty(_propertyName);
            if (propertyInfo == null) throw new InvalidOperationException(string.Format("Cannot find property named {0} on type {1}.", _propertyName, proxy.GetType()));
            return propertyInfo;
        }

        public void Set(IProxy proxy, object propertyValue)
        {
            EnsureSetter(proxy);
            _setter.DynamicInvoke(proxy, propertyValue);
        }

        public object Get(IProxy proxy)
        {
            EnsureGetter(proxy);
            return _getter.DynamicInvoke(proxy);
        }
    }
    internal class CalculatedFormulaInterceptor : IPropertyInterceptor
    {
        Delegate _func;

        public CalculatedFormulaInterceptor(Delegate func)
        {
            _func = func;
        }

        public void Set(IProxy proxy, object propertyValue)
        {
            throw new InvalidOperationException("Cannot set value of a Calculated Formula");
        }

        public object Get(IProxy proxy)
        {
            var result = _func.DynamicInvoke(proxy);
            return result;
        }
    }


    /// <summary>
    /// Creates a new Property On the Proxy.
    /// </summary>
    internal class PropertyFormulaBuilder : IFormulaBuilder
    {
        string _name;
        private Type _type;
        private bool _isReadonly;

        public PropertyFormulaBuilder(string name, Type type, bool isReadonly = false)
        {
            _name = name;
            _type = type;
            _isReadonly = isReadonly;
        }

        public void Build(TypeBuilder typeBuilder, StateBag proxyState)
        {
            var fieldName = "_" + _name.Substring(0, 1).ToLowerInvariant() + _name.Substring(1);
            var field = typeBuilder.InnerTypeBuilder.DefineField(fieldName, _type, System.Reflection.FieldAttributes.Private);

            var tb = typeBuilder.InnerTypeBuilder;
            var pb = tb.DefineProperty(_name, PropertyAttributes.None, _type, null);

            var methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the "get" accessor method.
            var mbGet = tb.DefineMethod("get_" + _name, methodAttributes, _type, Type.EmptyTypes);

            var mbGetIl = mbGet.GetILGenerator();

            mbGetIl.Emit(OpCodes.Nop);
            mbGetIl.Emit(OpCodes.Ldarg_0);
            mbGetIl.Emit(OpCodes.Ldfld, field);
            mbGetIl.Emit(OpCodes.Ret);
            pb.SetGetMethod(mbGet);


            if (!_isReadonly)
            {
                // Define the "set" accessor method.
                var mbSet = tb.DefineMethod("set_" + _name, methodAttributes, null, new Type[] { _type });

                var mbSetIl = mbSet.GetILGenerator();

                mbSetIl.Emit(OpCodes.Nop);
                mbSetIl.Emit(OpCodes.Ldarg_0);
                mbSetIl.Emit(OpCodes.Ldarg_1);
                mbSetIl.Emit(OpCodes.Stfld, field);
                mbSetIl.Emit(OpCodes.Ret);

                pb.SetSetMethod(mbSet);
            }

            InterceptorRegistry registry;
            if (proxyState.TryGetValue(States.InterceptorRegistry, out registry))
            {
                registry.Data.Add(_name, new PropertyFormulaInterceptor(_name));
            }
        }
    }
    internal class CalculatedFormulaBuilder : IFormulaBuilder
    {
        string _name;
        private LambdaExpression _expression;

        public CalculatedFormulaBuilder(string name, LambdaExpression expression)
        {
            _name = name;
            _expression = expression;
        }

        public void Build(TypeBuilder typeBuilder, StateBag proxyState)
        {
            var returnType = _expression.ReturnType;
            var tb = typeBuilder.InnerTypeBuilder;

            var pb = tb.DefineProperty(_name, PropertyAttributes.None, returnType, null);

            var methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the "get" accessor method.
            var mbGet = tb.DefineMethod("get_" + _name, methodAttributes, returnType, Type.EmptyTypes);

            var mbGetIl = mbGet.GetILGenerator();

            mbGetIl.Emit(OpCodes.Nop);
            mbGetIl.Emit(OpCodes.Ldarg_0);
            mbGetIl.Emit(OpCodes.Ldstr, _name);
            mbGetIl.Emit(OpCodes.Call, Ex.Method(() => ILHelper.GetPropertyValue(null, "")));
            if (returnType != typeof(object))
            {
                //Convert required to convert from intercepted value back to Formula type.
                var methodAsObject = Ex.Method(() => ILHelper.Convert<object>(null));
                var methodAsReturnType = methodAsObject.GetGenericMethodDefinition().MakeGenericMethod(returnType);

                mbGetIl.Emit(OpCodes.Call, methodAsReturnType);
            }
            
            mbGetIl.Emit(OpCodes.Nop);
            mbGetIl.Emit(OpCodes.Ret);
            pb.SetGetMethod(mbGet);

            var visitedExpression = new FormulaVisitor().Visit(_expression);

            InterceptorRegistry registry;
            if (proxyState.TryGetValue(States.InterceptorRegistry, out registry))
            {
                registry.Data.Add(_name, new CalculatedFormulaInterceptor(((LambdaExpression)visitedExpression).Compile()));
            }
        }
    }

    public interface IFormulaBuilder
    {
        void Build(TypeBuilder typeBuilder, StateBag proxyState);
    }

    public interface IFormula
    {
        string Name { get; }
    }

    public interface IFormula<TSource, TValue> : IFormula
    {
    }

    internal class Formula<TSource, TValue> : IFormula<TSource, TValue>
    {
        public string Name { get; private set; }

        public Formula(string name)
        {
            Name = name;
        }
    }

    internal class FormulaVisitor : ExpressionVisitor
    {
        private bool _signatureChanged;
        private ParameterExpression _originalParameter;
        private ParameterExpression _proxyParameter;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _originalParameter)
            {
                //Substitute the parameter with a call to Proxy.Target.
                var propertyName = Ex<IProxy<object>>.Property(m => m.Target).Name;
                var property = _proxyParameter.Type.GetProperty(propertyName);
                var target = Expression.Property(_proxyParameter, property);
                return target;
            }
            else
            {
                return base.VisitParameter(node);
            }
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (_signatureChanged) return base.VisitLambda<T>(node);
            _signatureChanged = true;
            _originalParameter = node.Parameters.Single();
            _proxyParameter = Expression.Parameter(typeof(IProxy<>).MakeGenericType(_originalParameter.Type), "p0");

            return Expression.Lambda(Visit(node.Body), _proxyParameter);
        }
    }
}
