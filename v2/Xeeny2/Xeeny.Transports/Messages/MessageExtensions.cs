using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports.Messages;

namespace Xeeny
{
    public static class MessageExtensions
    {
        public static void AddProperty(this Message message, string key, string value)
        {
            var properties = message.Properties;
            if (properties.TryGetValue(key, out var _))
            {
                throw new Exception($"Message already have property with key {key}");
            }
            properties[key] = value;
        }

        public static string TryGetProperty(this Message message, string key)
        {
            message.Properties.TryGetValue(key, out var result);
            return result;
        }

        public static string GetProperty(this Message message, string key)
        {
            return message.Properties[key];
        }

        public static void RemoveProperty(this Message message, string key)
        {
            message.Properties.Remove(key);
        }

        public static void ClearProperties(this Message message)
        {
            message.Properties.Clear();
        }
    }
}
