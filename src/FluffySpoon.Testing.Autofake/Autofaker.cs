﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FluffySpoon.Testing.Autofake
{
    public class Autofaker : IAutofaker
    {
        private IInversionOfControlRegistration _registration;
        private IFakeGenerator _fakeGenerator;

        /// <summary>
        /// Creates an Autofaker instance. Configure with extension methods afterwards.
        /// </summary>
        public Autofaker()
        {
        }

        public Autofaker(
            IInversionOfControlRegistration registration,
            IFakeGenerator fakeGenerator)
        {
            Configure(
                registration,
                fakeGenerator);
        }

        public void Configure(
            IInversionOfControlRegistration registration)
        {
            _registration = registration;
        }

        public void Configure(
            IFakeGenerator fakeGenerator)
        {
            _fakeGenerator = fakeGenerator;
        }

        public void Configure(
            IInversionOfControlRegistration registration,
            IFakeGenerator fakeGenerator)
        {
            Configure(registration);
            Configure(fakeGenerator);
        }

        public void RegisterFakesForConstructorParameterTypesOf<TClassOrInterface>()
        {
            if (_registration == null)
                throw new InvalidOperationException("An Inversion of Control registration must be specified.");

            if (_fakeGenerator == null)
                throw new InvalidOperationException("A faking framework must be specified.");

            var classType = typeof(TClassOrInterface);

            var classTypeInfo = classType.GetTypeInfo();
            if (classTypeInfo.IsInterface)
            {
                classType = FindImplementingClassType(classType);
            }

            var constructors = classType.GetTypeInfo()
              .DeclaredConstructors
              .ToArray();
            if (constructors.Length > 1)
                throw new InvalidOperationException("More than one constructor was found for " + classType.FullName + " which is not supported by Autofaker.");

            var constructor = constructors.Single();

            var arguments = constructor.GetParameters();
            foreach (var argument in arguments)
            {
                var parameterType = argument.ParameterType;
                var parameterTypeInfo = parameterType.GetTypeInfo();
                if (parameterType != typeof(string) && !parameterTypeInfo.IsInterface)
                    continue;

                var registrationType = _registration.GetType();
                var registerTypeAsInstanceMethod = registrationType.GetRuntimeMethod(
                    nameof(_registration.RegisterInterfaceTypeAsInstanceFromAccessor),
                    new[] { typeof(Func<object>) });
                foreach (var fakeInstanceFactory in GetFakeInstanceFactories(parameterType))
                {
                    var registerTypeAsInstanceGenericMethod = registerTypeAsInstanceMethod.MakeGenericMethod(fakeInstanceFactory.Type);
                    registerTypeAsInstanceGenericMethod.Invoke(
                        _registration,
                        new object[] { fakeInstanceFactory.Accessor });
                }
            }
        }

        private IReadOnlyList<IFakeInstanceFactory> GetFakeInstanceFactories(Type parameterType)
        {
            if (parameterType == typeof(string))
            {
                return new[]
                {
                    new FakeInstanceFactory(parameterType, () => string.Empty)
                };
            }

            return _fakeGenerator.GenerateFakeInstanceFactories(parameterType);
        }

        private Type FindImplementingClassType(Type interfaceType)
        {
            var assemblyTypes = interfaceType
                .GetTypeInfo()
                .Assembly
                .DefinedTypes;

            foreach (var classType in assemblyTypes)
            {
                if (!classType.IsClass)
                    continue;

                var implementedInterfaces = classType.ImplementedInterfaces;
                if (implementedInterfaces.All(x => x != interfaceType))
                    continue;

                return classType.AsType();
            }

            throw new InvalidOperationException("Could not find an implementing class of " + interfaceType.FullName + " in the same assembly.");
        }
    }
}
