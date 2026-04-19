using System;
using System.Reflection;

namespace Buttr.Core {
    public abstract class ObjectResolverBase<TConcrete> : IObjectResolver {
        protected readonly Type[] requirements;
        protected readonly Func<object[], TConcrete> factory;

        protected ObjectResolverBase() {
            ConstructorInfo selectedConstructor = null;

            var constructors = typeof(TConcrete).GetConstructors();
            for (var i = 0; i < constructors.Length; i++) {
                if (constructors[i].IsPublic == false) { continue; }
                
                selectedConstructor = constructors[i];
                break;
            }

            if (selectedConstructor == null)
                throw new InvalidOperationException($"No public constructor found for type {typeof(TConcrete)}");

            var paramInfos = selectedConstructor.GetParameters();
            var paramCount = paramInfos.Length;
            requirements = paramCount == 0 ? Array.Empty<Type>() : new Type[paramCount];

            for (var i = 0; i < paramCount; i++) {
                
                var paramType = paramInfos[i].ParameterType;
                if (paramType.IsByRef) {
                    paramType = paramType.GetElementType(); // unwrap ref type
                    ButtrLog.Log($"Type is passed by reference: reference passing does not function in Buttr {paramType}");
                }

                requirements[i] = paramType;
            }

            factory = ObjectFactory.Create<TConcrete>(selectedConstructor, requirements);
        }

        public abstract bool IsResolved { get; }
        
        public abstract object Resolve();
    }
}