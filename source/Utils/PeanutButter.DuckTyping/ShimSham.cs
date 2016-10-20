﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PeanutButter.DuckTyping.Exceptions;

namespace PeanutButter.DuckTyping
{
    public class PropertyInfoContainer
    {
        public Dictionary<string, PropertyInfo> PropertyInfos { get; }
        public Dictionary<string, PropertyInfo> FuzzyPropertyInfos { get; }
        public PropertyInfoContainer(
            PropertyInfo[] propertyInfos
        )
        {
            PropertyInfos = propertyInfos.ToDictionary(
                pi => pi.Name,
                pi => pi,
                StringComparer.InvariantCulture
            );
            FuzzyPropertyInfos = propertyInfos.ToDictionary(
                pi => pi.Name,
                pi => pi,
                StringComparer.OrdinalIgnoreCase
            );
        }
    }
    public class MethodInfoContainer
    {
        public Dictionary<string, MethodInfo> MethodInfos { get; }
        public Dictionary<string, MethodInfo> FuzzyMethodInfos { get; }
        public MethodInfoContainer(
            MethodInfo[] methodInfos
        )
        {
            MethodInfos = methodInfos.ToDictionary(
                pi => pi.Name,
                pi => pi,
                StringComparer.InvariantCulture
            );
            FuzzyMethodInfos = methodInfos.ToDictionary(
                pi => pi.Name,
                pi => pi,
                StringComparer.OrdinalIgnoreCase
            );
        }
    }
    public class ShimSham
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsFuzzy { get; }
        private readonly object _wrapped;
        private readonly Type _wrappedType;
        private readonly bool _wrappingADuck;

        private static readonly Dictionary<Type,  PropertyInfoContainer> _propertyInfos = new Dictionary<Type, PropertyInfoContainer>();
        private static readonly Dictionary<Type,  MethodInfoContainer> _methodInfos = new Dictionary<Type, MethodInfoContainer>();
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _fieldInfos = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private Dictionary<string, PropertyInfo> _localPropertyInfos;
        private Dictionary<string, FieldInfo> _localFieldInfos;
        private Dictionary<string, MethodInfo> _localMethodInfos;

        // ReSharper disable once IntroduceOptionalParameters.Global
        // ReSharper disable once UnusedMember.Global
        public ShimSham(object toWrap): this(toWrap, false)
        {
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public ShimSham(object toWrap, bool isFuzzy)
        {
            IsFuzzy = isFuzzy;
            _wrapped = toWrap;
            _wrappedType = toWrap.GetType();
            _wrappingADuck = IsObjectADuck();
            StaticallyCachePropertyInfosFor(_wrappedType, _wrappingADuck);
            StaticallyCacheMethodInfosFor(_wrappedType);
            LocallyCachePropertyInfos();
            LocallyCacheMethodInfos();
        }

        private void StaticallyCacheMethodInfosFor(Type wrappedType)
        {
            lock(_methodInfos)
            {
                if (_methodInfos.ContainsKey(wrappedType))
                    return;
                _methodInfos[wrappedType] = new MethodInfoContainer(
                    wrappedType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                );
            }
        }

        // ReSharper disable once UnusedMember.Global

        public object GetPropertyValue(string propertyName)
        {
            if (_wrappingADuck)
            {
                return FieldValueFor(propertyName);
            }

            var propInfo = FindPropertyInfoFor(propertyName);
            if (!propInfo.CanRead)
            {
                throw new WriteOnlyPropertyException(_wrappedType, propertyName);
            }
            return propInfo.GetValue(_wrapped);
        }

        // ReSharper disable once UnusedMember.Global
        public void SetPropertyValue(string propertyName, object newValue)
        {

            if (_wrappingADuck)
            {
                SetFieldValue(propertyName, newValue);
                return;
            }

            var propInfo = FindPropertyInfoFor(propertyName);
            if (!propInfo.CanWrite)
            {
                throw new ReadOnlyPropertyException(_wrappedType, propertyName);
            }
            propInfo.SetValue(_wrapped, newValue);
        }

        // ReSharper disable once UnusedMember.Global
        public void CallThroughVoid(string methodName, params object[] parameters)
        {
            CallThrough(methodName, parameters);
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public object CallThrough(string methodName, object[] parameters)
        {
            if (_wrappingADuck)
            {
                throw new NotImplementedException("Cannot call-through when there is no wrapped object");
            }
            MethodInfo methodInfo;
            if (!_localMethodInfos.TryGetValue(methodName, out methodInfo))
                throw new MethodNotFoundException(_wrappedType, methodName);
            if (IsFuzzy)
                parameters = AttemptToOrderCorrectly(parameters, methodInfo);
            var result =  methodInfo.Invoke(_wrapped, parameters);
            return result;
        }

        private object[] AttemptToOrderCorrectly(
            object[] parameters, 
            MethodInfo methodInfo)
        {
            var methodParameters = methodInfo.GetParameters();
            if (parameters.Length != methodParameters.Length)
                throw new ParameterCountMismatchException(parameters.Length, methodInfo);
            var srcTypes = parameters.Select(o => o?.GetType()).ToArray();
            var dstTypes = methodParameters.Select(p => p.ParameterType).ToArray();
            if (AlreadyInCorrectOrderByType(srcTypes, dstTypes))
                return parameters;  // no need to change anything here and we don't have to care about parameters with the same type
            if (dstTypes.Distinct().Count() != dstTypes.Length)
                throw new UnresolveableParameterOrderMismatchException(dstTypes, methodInfo);
            return Reorder(parameters, dstTypes);
        }

        private object[] Reorder(object[] parameters, Type[] dstTypes)
        {
            return dstTypes
                    .Select(type => FindBestMatchFor(type, parameters))
                    .ToArray();
        }

        private object FindBestMatchFor(Type type, object[] parameters)
        {
            return parameters.FirstOrDefault(p => p.GetType() == type);
        }

        private bool AlreadyInCorrectOrderByType(
            Type[] srcTypes,
            Type[] dstTypes
        )
        {
            for (var i = 0; i < srcTypes.Length; i++)
            {
                var src = srcTypes[i];
                var dst = dstTypes[i];
                if (src == null && dst.IsPrimitive)
                    return false;
                if (src != dst)
                    return false;
            }
            return true;
        }

        private void LocallyCachePropertyInfos()
        {
            _localPropertyInfos = IsFuzzy 
                                    ? _propertyInfos[_wrappedType].FuzzyPropertyInfos
                                    : _propertyInfos[_wrappedType].PropertyInfos;
            if (_wrappingADuck)
                _localFieldInfos = _fieldInfos[_wrappedType];
        }

        private void LocallyCacheMethodInfos()
        {
            _localMethodInfos = IsFuzzy
                                ? _methodInfos[_wrappedType].FuzzyMethodInfos
                                : _methodInfos[_wrappedType].MethodInfos;
        }

        private static void StaticallyCachePropertyInfosFor(Type toCacheFor, bool cacheFieldInfosToo)
        {
            lock (_propertyInfos)
            {
                if (_propertyInfos.ContainsKey(toCacheFor))
                    return;
                _propertyInfos[toCacheFor] = new PropertyInfoContainer(toCacheFor
                                    .GetProperties(BindingFlags.Instance | BindingFlags.Public));
                if (!cacheFieldInfosToo)
                    return;
                _fieldInfos[toCacheFor] = toCacheFor
                                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                .ToDictionary(
                                    fi => fi.Name,
                                    fi => fi
                                );
            }
        }

        private bool IsObjectADuck()
        {
            return _wrappedType.GetCustomAttributes(true).OfType<IsADuckAttribute>().Any();
        }

        private PropertyInfo FindPropertyInfoFor(string propertyName)
        {
            PropertyInfo propInfo;
            if (!_localPropertyInfos.TryGetValue(propertyName, out propInfo))
                throw new PropertyNotFoundException(_wrappedType, propertyName);
            return propInfo;
        }

        private void SetFieldValue(string propertyName, object newValue)
        {
            var fieldInfo = FindPrivateBackingFieldFor(propertyName);
            fieldInfo.SetValue(_wrapped, newValue);
        }

        private object FieldValueFor(string propertyName)
        {
            var fieldInfo = FindPrivateBackingFieldFor(propertyName);
            return fieldInfo.GetValue(_wrapped);
        }

        private FieldInfo FindPrivateBackingFieldFor(string propertyName)
        {
            var seek = "_" + propertyName;
            FieldInfo fieldInfo;
            if (!_localFieldInfos.TryGetValue(seek, out fieldInfo))
                throw new BackingFieldForPropertyNotFoundException(_wrappedType, propertyName);
            return fieldInfo;
        }
    }

}