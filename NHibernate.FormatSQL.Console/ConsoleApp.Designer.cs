﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NHibernate.FormatSQL.Console {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class ConsoleApp : global::System.Configuration.ApplicationSettingsBase {
        
        private static ConsoleApp defaultInstance = ((ConsoleApp)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new ConsoleApp())));
        
        public static ConsoleApp Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string WorkPath {
            get {
                return ((string)(this["WorkPath"]));
            }
            set {
                this["WorkPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public string Mode {
            get {
                return ((string)(this["Mode"]));
            }
            set {
                this["Mode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("On")]
        public string SafeMode {
            get {
                return ((string)(this["SafeMode"]));
            }
            set {
                this["SafeMode"] = value;
            }
        }
    }
}
