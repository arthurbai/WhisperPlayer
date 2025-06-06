﻿using System.Collections.Generic;

namespace FlyleafLib.MediaFramework.MediaDevice;

public class DeviceBase :DeviceBase<DeviceStreamBase>
{
    public DeviceBase(string friendlyName, string symbolicLink) : base(friendlyName, symbolicLink)
    {
    }
}

public class DeviceBase<T>
    where T: DeviceStreamBase
{
    public string   FriendlyName            { get; }
    public string   SymbolicLink            { get; }
    public IList<T>
                    Streams                 { get; protected set; }
    public string   Url                     { get; protected set; } // default Url

    public DeviceBase(string friendlyName, string symbolicLink)
    {
        FriendlyName = friendlyName;
        SymbolicLink = symbolicLink;

        Engine.Log.Debug($"[{(this is AudioDevice ? "Audio" : "Video")}Device] {friendlyName}");
    }

    public override string ToString() => FriendlyName;
}
