using System;
using System.ServiceModel;
using DataConnectors.Common.Helper;

namespace DataConnectors.Adapter
{
    [Serializable]
    [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
    public abstract class ConnectionInfoBase
    {
    }
}