﻿using Artemis.Core;
using Artemis.Core.Modules;
using Artemis.Core.Services;
using Artemis.Plugins.Module.DynamicExternalDataModule.DataModels;
using Artemis.UI.Services;
using System;
using System.Collections.Generic;

namespace Artemis.Plugins.Module.DynamicExternalDataModule
{
    public class DesktopVariablesDataModelExpansion : Module<DynamicExternalDataModuleDataModel>
    {
        private readonly IWebServerService _webServerService;
        private readonly PluginSettings _pluginSettings;
        private Dictionary<string, KeyValuePair<object, Type>> _savedDynamicData;
        private PluginSetting<Dictionary<string, KeyValuePair<object, Type>>> _savedDynamicDataSetting;
        public DesktopVariablesDataModelExpansion(IWebServerService webServerService, IDebugService _debugService, PluginSettings pluginSettings)
        {
            _webServerService = webServerService;
            _pluginSettings = pluginSettings;
        }

        public override List<IModuleActivationRequirement> ActivationRequirements => null;

        public override void Enable()
        {
            _savedDynamicDataSetting = _pluginSettings.GetSetting("SavedDynamicDataSetting", new Dictionary<string, KeyValuePair<object, Type>>());
            _webServerService.AddStringEndPoint(this, "SetBoolValue", payload => ProcessValue<bool>(payload));
            _webServerService.AddStringEndPoint(this, "ToggleBoolValue", payload => ToggleBoolValue(payload, true));

            _webServerService.AddStringEndPoint(this, "SetIntValue", payload => ProcessValue<Int64>(payload));
            _webServerService.AddStringEndPoint(this, "AddIntOffset", payload => ProcessIntOffset(payload, 0, new Int64Range(Int64.MinValue, Int64.MaxValue)));
            _webServerService.AddStringEndPoint(this, "AddIntOffsetAsPercentage", payload => ProcessIntOffset(payload, 0, new Int64Range(0, 100)));

            _webServerService.AddStringEndPoint(this, "SetFloatValue", payload => ProcessValue<float>(payload));
            _webServerService.AddStringEndPoint(this, "SetStringValue", payload => ProcessValue<string>(payload));
            _webServerService.AddStringEndPoint(this, "RemoveAll", payload => RemoveAll());
            _webServerService.AddStringEndPoint(this, "Remove", payload => Remove(payload));
            LoadSavedValues();
        }

        // TODO: More param checks and log
        private bool ProcessValue<T>(string payload)
        {
            if (!ParsePayload<T>(payload, out string key, out object value))
            {
                // Log
                return false;
            }
            bool setValueResult = SetValue<T>(key, value);
            bool saveValueResult = SaveValue<T>(key, value);
            return setValueResult;
        }

        private bool ToggleBoolValue(string key, bool defaultValue)
        {
            DynamicChild<bool> child;
            if (DataModel.TryGetDynamicChild<bool>(key, out child))
            {
                child.Value = !child.Value;
            }
            else
            {
                child = DataModel.AddDynamicChild<bool>(key, defaultValue);
            }
            if (child != null)
                return SaveValue<bool>(key, child.Value);
            else
                return false;
        }

        private bool ProcessIntOffset(string payload, int defaultValue, Int64Range range)
        {
            if (!ParsePayload<Int64>(payload, out string key, out object value))
            {
                // Log
                return false;
            }

            if (DataModel.TryGetDynamicChild<Int64>(key, out DynamicChild<Int64> child))
            {
                child.Value = Math.Clamp(child.Value + (Int64)value, range.From, range.To);
                return SaveValue<Int64>(key, child.Value);
            }
            else
            {
                DataModel.AddDynamicChild<Int64>(key, defaultValue);
            }
            return true;
        }

        private bool SetValue<T>(string key, object value)
        {
            if (value is not T)
            {
                //Log
                return false;
            }

            if (DataModel.TryGetDynamicChild<T>(key, out DynamicChild<T> child))
            {
                child.Value = (T)value;
            }
            else
            {
                DataModel.AddDynamicChild<T>(key, (T)value);
            }
            return true;
        }

        private bool SaveValue<T>(string key, object value)
        {
            _savedDynamicData = _savedDynamicDataSetting.Value;
            _savedDynamicData[key] = new KeyValuePair<object, Type>((T)value, typeof(T));
            _savedDynamicDataSetting.Value = _savedDynamicData;
            _savedDynamicDataSetting.Save();
            return true;
        }

        private void LoadSavedValues()
        {
            _savedDynamicData = _savedDynamicDataSetting.Value;

            foreach (var savedData in _savedDynamicData)
            {
                try
                {
                    switch (Type.GetTypeCode(savedData.Value.Value))
                    {
                        case (TypeCode.Boolean):
                            SetValue<bool>(savedData.Key, (bool)savedData.Value.Key);
                            break;
                        case (TypeCode.Int32):
                        case (TypeCode.Int64):
                            SetValue<Int64>(savedData.Key, (Int64)savedData.Value.Key);
                            break;
                        case (TypeCode.Single):
                            SetValue<float>(savedData.Key, (float)savedData.Value.Key);
                            break;
                        case (TypeCode.String):
                            SetValue<string>(savedData.Key, (string)savedData.Value.Key);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //Log
                }
            }
        }

        private void RemoveAll()
        {
            _savedDynamicData = _savedDynamicDataSetting.Value;
            _savedDynamicDataSetting.Value.Clear();
            _savedDynamicDataSetting.Save();
            DataModel.ClearDynamicChildren();
        }

        private void Remove(string key)
        {
            DataModel.RemoveDynamicChildByKey(key);
            _savedDynamicDataSetting.Value.Remove(key);
            _savedDynamicData = _savedDynamicDataSetting.Value;
            _savedDynamicDataSetting.Save();
            DataModel.ClearDynamicChildren();
        }

        public bool ParsePayload<T>(string payload, out string key, out object value)
        {
            key = string.Empty;
            value = null;
            string[] data = payload.Split(':');
            if (data.Length != 2)
            {
                return false;
            }
            key = data[0];
            value = (T)Convert.ChangeType(data[1], typeof(T));
            return true;
        }
        public override void Disable() { }
        public override void Update(double deltaTime) { }
    }

    public class Int64Range
    {
        public Int64 From { get; set; }
        public Int64 To { get; set; }

        public Int64Range(Int64 from, Int64 to)
        {
            From = from;
            To = to;
        }
    }
}