using Polymod.Fluent;
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
    public class TypeBuilder
    {
        private readonly List<Type> _interfaces = new List<Type>();
        private static System.Reflection.Emit.ModuleBuilder _mb;
        private static System.Reflection.Emit.AssemblyBuilder _ab;

        System.Reflection.Emit.TypeBuilder _innerTypeBuilder;
        public System.Reflection.Emit.TypeBuilder InnerTypeBuilder
        {
            get
            {
                if (_innerTypeBuilder != null) return _innerTypeBuilder;
                if (_mb == null)
                {
                    //TODO: Thread Locking and proper naming!
                    _ab = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new System.Reflection.AssemblyName("Proxy"), System.Reflection.Emit.AssemblyBuilderAccess.Run);
                    _mb = _ab.DefineDynamicModule("Proxy.dll");
                }
                var typeName = TargetType.Name + "_Proxy";
                var index = 0;
                while (_mb.GetType(typeName) != null)
                {
                    typeName = TargetType.Name + "_Proxy_" + (index++);
                }

                _innerTypeBuilder = _mb.DefineType(typeName, System.Reflection.TypeAttributes.Public);
                return _innerTypeBuilder;
            }
        }
        public Type TargetType { get; private set; }

        public TypeBuilder(Type targetType)
        {
            if (targetType == null) throw new ArgumentNullException("type");
            TargetType = targetType;
        }

        private Type _dynamicType;

        public bool Implements<T>()
        {
            return typeof(T).IsAssignableFrom(InnerTypeBuilder);
        }
        public void Implement<T>()
        {
            if (InnerTypeBuilder.GetInterfaces().Contains(typeof(T))) return;
            InnerTypeBuilder.AddInterfaceImplementation(typeof(T));
        }

        public MethodBuilder ExplicitlyImplement(MethodInfo methodInfo)
        {
            var explicitImplementation = MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var mb = InnerTypeBuilder.DefineMethod(methodInfo.DeclaringType.Name + "." + methodInfo.Name, explicitImplementation, CallingConventions.Standard, methodInfo.ReturnType, methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
            InnerTypeBuilder.DefineMethodOverride(mb, methodInfo);
            return mb;
        }

        public FieldBuilder ExplicitlyImplement(EventInfo eventInfo)
        {
            var explicitEventImplementation = EventAttributes.SpecialName;
            var explicitMethodImplementation = MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var fb = InnerTypeBuilder.DefineField(ToFieldName(eventInfo.Name), eventInfo.EventHandlerType, FieldAttributes.Private);
            var eb = InnerTypeBuilder.DefineEvent(string.Format("{0}.{1}", eventInfo.DeclaringType.Name, eventInfo.Name), explicitEventImplementation, eventInfo.EventHandlerType);

            var addMb = InnerTypeBuilder.DefineMethod(eventInfo.DeclaringType.Name + ".add_" + eventInfo.Name, explicitMethodImplementation, CallingConventions.Standard, typeof(void), new[] { eventInfo.EventHandlerType });
            var removeMb = InnerTypeBuilder.DefineMethod(eventInfo.DeclaringType.Name + ".remove_" + eventInfo.Name, explicitMethodImplementation, CallingConventions.Standard, typeof(void), new[] { eventInfo.EventHandlerType });
            eb.SetAddOnMethod(addMb);
            eb.SetRemoveOnMethod(removeMb);
            InnerTypeBuilder.DefineMethodOverride(addMb, eventInfo.GetAddMethod());
            InnerTypeBuilder.DefineMethodOverride(removeMb, eventInfo.GetRemoveMethod());

            //Call Delegate.Combine for Adds and Delegate.Remove for remove.
            BuildEventMethod(addMb.GetILGenerator(), fb, eventInfo, Ex.Method(() => Delegate.Combine(null, null)));
            BuildEventMethod(removeMb.GetILGenerator(), fb, eventInfo, Ex.Method(() => Delegate.Remove(null, null)));


            return fb;
        }

        private static string ToFieldName(string name)
        {
            if (name.StartsWith("_", StringComparison.Ordinal)) return name;
            return "_" + name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }


        private void BuildEventMethod(ILGenerator il, FieldBuilder fb, EventInfo eventInfo, MethodInfo callMethod)
        {
            //NOTE: There is no locking around the event, so concurrent multi-threaded callers could mess the add/remove of events.
            //Since this is unlikely, I have left this non-thread safe.

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldfld, fb);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, callMethod);
            il.Emit(OpCodes.Castclass, eventInfo.EventHandlerType);
            il.Emit(OpCodes.Stfld, fb);
            il.Emit(OpCodes.Ret);

        }




        public Type CreateProxyType()
        {
            var result = InnerTypeBuilder.CreateType();
            return result;
        }

        public object CreateProxy(object value)
        {
            if (_dynamicType == null) _dynamicType = InnerTypeBuilder.CreateType();
            var result = Activator.CreateInstance(_dynamicType);
            return result;
        }

    }
}
