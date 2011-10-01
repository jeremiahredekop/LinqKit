using System;
using System.Reflection;

namespace LinqKit
{
    internal static class ReflectionHelper
    {
        public static object GetMemberValue(object myObject, string memberName)
        {
            bool success;
            var output = TryGetMemberValue(myObject, memberName, out success);
            if (success) return output;

            throw new Exception(String.Format("Object {0} does not have a property or field named {1}", myObject.GetType().Name, memberName));
        }

        public static object TryGetMemberValue(object myObject, string memberName, out bool success)
        {
            var output = TryGetFieldValue(memberName, myObject, out success);
            if (success) return output;

            output = TryGetPropertyValue(memberName, myObject, out success);
            return output;
        }

        public static object TryGetFieldValue(string fieldName, object obj, out bool success, Type typeToUse = null)
        {
            success = false;
            var type = typeToUse ?? obj.GetType();
            var lmb = type.GetField(fieldName, DefaultFlags | BindingFlags.GetField);
            if (lmb == null) return null;
            success = true;
            return lmb.GetValue(obj);
        }

        private const BindingFlags DefaultFlags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static object TryGetPropertyValue(string propertyName, object obj, out bool success, Type typeToUse = null)
        {
            var type = typeToUse ?? obj.GetType();

            var lmb = type.GetProperty(propertyName, DefaultFlags | BindingFlags.GetProperty);
            if (lmb != null)
            {
                success = true;
                return lmb.GetValue(obj, null);
            }

            success = false;
            return null;
        }

        public static object TryInvokeMethod(string methodName, object obj, Type typeToUse, out bool success, params object[] args)
        {
            var type = typeToUse ?? obj.GetType();

            var mi = type.GetMethod(methodName, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            success = mi != null;

            return success ? mi.Invoke(obj, args) : null;
        }
    }
}