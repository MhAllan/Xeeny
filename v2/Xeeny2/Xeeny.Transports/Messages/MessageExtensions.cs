using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Serialization.Abstractions;
using Xeeny.Transports.Messages;

namespace Xeeny
{
    public static class MessageExtensions
    {
        public static void AddProperty(this Message message, MessageProperty key, string value)
        {
            AddProperty(message, (ushort)key, value);
        }

        public static string TryGetProperty(this Message message, MessageProperty key)
        {
            return TryGetProperty(message, (ushort)key);
        }

        public static string GetProperty(this Message message, MessageProperty key)
        {
            return GetProperty(message, (ushort)key);
        }

        public static void RemoveProperty(this Message message, MessageProperty key)
        {
            RemoveProperty(message, (ushort)key);
        }

        public static void AddProperty(this Message message, ushort key, string value)
        {
            var properties = message.Properties;
            if (properties.TryGetValue(key, out var _))
            {
                throw new Exception($"Message already have property with key {key}");
            }
            properties[key] = value;
        }

        public static string TryGetProperty(this Message message, ushort key)
        {
            message.Properties.TryGetValue(key, out var result);
            return result;
        }

        public static string GetProperty(this Message message, ushort key)
        {
            return message.Properties[key];
        }

        public static void RemoveProperty(this Message message, ushort key)
        {
            message.Properties.Remove(key);
        }

        public static void ClearProperties(this Message message)
        {
            message.Properties.Clear();
        }
    }
}
