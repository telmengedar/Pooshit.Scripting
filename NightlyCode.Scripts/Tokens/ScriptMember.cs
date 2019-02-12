using System;
using System.Linq;
using System.Reflection;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// reads a value from a host member
    /// </summary>
    class ScriptMember : AssignableToken {
        readonly IScriptToken hosttoken;
        readonly string membername;

        /// <summary>
        /// creates a new <see cref="ScriptMember"/>
        /// </summary>
        /// <param name="host">host of member</param>
        /// <param name="membername">name of member to read</param>
        public ScriptMember(IScriptToken host, string membername) {
            hosttoken = host;
            this.membername = membername.ToLower();
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments) {
            object host = hosttoken.Execute(arguments);
            PropertyInfo property = host.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower() == membername);
            if(property != null) {
                try {
                    return property.GetValue(host);
                }
                catch(Exception e) {
                    throw new ScriptRuntimeException("Unable to read property", null, e);
                }
            }


            FieldInfo fieldinfo = host.GetType().GetFields().FirstOrDefault(f => f.Name.ToLower() == membername);
            if (fieldinfo == null)
                throw new ScriptRuntimeException($"A member with the name of {membername} was not found in type {host.GetType().Name}");

            try {
                return fieldinfo.GetValue(host);
            }
            catch(Exception e) {
                throw new ScriptRuntimeException("Unable to read field", null, e);
            }
        }

        object SetProperty(object host, PropertyInfo property, IScriptToken valuetoken, IVariableProvider arguments)
        {
            object targetvalue = Converter.Convert(valuetoken.Execute(arguments), property.PropertyType);
            try
            {
                property.SetValue(host, targetvalue, null);
            }
            catch (Exception e)
            {
                throw new ScriptRuntimeException("Unable to set property", null, e);
            }

            return targetvalue;
        }

        object SetField(object host, FieldInfo fieldinfo, IScriptToken valuetoken, IVariableProvider arguments)
        {
            object targetvalue = Converter.Convert(valuetoken.Execute(arguments), fieldinfo.FieldType);
            try
            {
                fieldinfo.SetValue(host, targetvalue);
            }
            catch (Exception e)
            {
                throw new ScriptRuntimeException("Unable to set field", null, e);
            }

            return targetvalue;
        }

        protected override object AssignToken(IScriptToken token, IVariableProvider arguments) {
            object host = hosttoken.Execute(arguments);
            PropertyInfo property = host.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower() == membername);
            if (property != null)
                return SetProperty(host, property, token, arguments);

            FieldInfo fieldinfo = host.GetType().GetFields().FirstOrDefault(f => f.Name.ToLower() == membername);
            if (fieldinfo == null)
                throw new ScriptRuntimeException($"A member with the name of {membername} was not found in type {host.GetType().Name}");

            return SetField(host, fieldinfo, token, arguments);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{hosttoken}.{membername}";
        }
    }
}