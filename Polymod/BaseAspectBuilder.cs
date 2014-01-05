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
    /// <summary>
    /// This aspect makes Proxies implement IProxy, which is the basic requirement for having Proxies.
    /// </summary>
    internal class BaseAspectBuilder : IAspectBuilder
    {
        public void Build(TypeBuilder typeBuilder, StateBag aspectState)
        {
            var tb = typeBuilder.InnerTypeBuilder;

            var proxyType = typeof(IProxy<>).MakeGenericType(typeBuilder.TargetType);
            var targetField = tb.DefineField("_target", typeBuilder.TargetType, FieldAttributes.Private);
            var stateField = tb.DefineField("_state", typeof(Dictionary<string, object>), FieldAttributes.Private);
            
            tb.AddInterfaceImplementation(proxyType);
            ImplementProperty(tb, targetField, Ex<IProxy>.Property(p => p.Target));
            ImplementProperty(tb, targetField, proxyType.GetProperty(Ex<IProxy>.Property(p => p.Target).Name));
            ImplementProperty(tb, stateField, Ex<IProxy>.Property(p => p.State));
            BuildConstructor(tb, targetField, stateField);
        }

        private void BuildConstructor(System.Reflection.Emit.TypeBuilder tb, FieldBuilder target, FieldBuilder state)
        {
            var constructorSignature = new Type[] { target.FieldType, typeof(StateBag) };
            var constructor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, constructorSignature);
            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, tb.BaseType.GetConstructor(Type.EmptyTypes)); //Call base constructor
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, target);
            il.Emit(OpCodes.Nop);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, state);

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);

        }

        private void ImplementProperty(System.Reflection.Emit.TypeBuilder tb, FieldInfo valueField, PropertyInfo propertyInfo)
        {
            var methodAttributes = MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var propertyAttributes = PropertyAttributes.None;
            
            var pb = tb.DefineProperty(string.Format("{0}.{1}", propertyInfo.DeclaringType.Name, propertyInfo.Name), propertyAttributes, propertyInfo.PropertyType, Type.EmptyTypes);

            var mbGet = tb.DefineMethod("get_" + propertyInfo.Name, methodAttributes, propertyInfo.PropertyType, Type.EmptyTypes);
            tb.DefineMethodOverride(mbGet, propertyInfo.GetMethod);

            var il = mbGet.GetILGenerator();
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, valueField);
            il.Emit(OpCodes.Ret);
        }

    }
}
