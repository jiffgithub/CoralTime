using System;
using System.Collections.Generic;
using static CoralTime.Common.Constants.Constants;

namespace CoralTime.Common.Helpers
{
    public class UpdateService<T>
    {
        public static T UpdateObject(IDictionary<string,object> delta, T currentObject)
        {
            var propertyInfoes = typeof(T).GetProperties();

            foreach (var propertyInfo in propertyInfoes)
            {
                if (StringHandler.ToLowerCamelCase(propertyInfo.Name)!="id" &&
                    HasField(delta, StringHandler.ToLowerCamelCase(propertyInfo.Name)))
                {
                    if (propertyInfo.PropertyType.Name.Contains("Nullable"))
                    {
                        Int32? value = Convert.ToInt32(delta[StringHandler.ToLowerCamelCase(propertyInfo.Name)]);
                        propertyInfo.SetValue(currentObject, value);
                    }
                    else if (propertyInfo.PropertyType.Name.Contains("DateTime"))
                    {
                        DateTime value = (DateTime)delta[StringHandler.ToLowerCamelCase(propertyInfo.Name)];
                        propertyInfo.SetValue(currentObject, value);
                    }
                    else if (propertyInfo.PropertyType.Name.Contains("Boolean"))
                    {
                        Boolean value = Boolean.Parse(delta[StringHandler.ToLowerCamelCase(propertyInfo.Name)].ToString());
                        propertyInfo.SetValue(currentObject, value);
                    }
                    else if (propertyInfo.PropertyType.Name.Contains("Double"))
                    {
                        double value = (double)delta[StringHandler.ToLowerCamelCase(propertyInfo.Name)];
                        propertyInfo.SetValue(currentObject, value);
                    }
                    else if (propertyInfo.PropertyType.Name.Contains("LockTimePeriod"))
                    {
                        LockTimePeriod value = (LockTimePeriod)(Int32.Parse(delta[StringHandler.ToLowerCamelCase(propertyInfo.Name)].ToString()));
                        propertyInfo.SetValue(currentObject, value);
                    }
                    else
                    {
                        string value = delta[StringHandler.ToLowerCamelCase(propertyInfo.Name)].ToString();
                        propertyInfo.SetValue(currentObject, Convert.ChangeType(value, propertyInfo.PropertyType));
                    }
                }
            }

            return currentObject;
        }

        public static bool HasField(IDictionary<string, object> dynamicObject, string fieldName)
        {
            if (!dynamicObject.ContainsKey(fieldName))
                return false;

            var value = dynamicObject[fieldName];
            return value != null;
        }
    }
}