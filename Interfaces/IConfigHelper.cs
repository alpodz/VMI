using System;
using System.Collections.Generic;

namespace Interfaces
{
    public interface IConfigHelper
    {
        Dictionary<String, String> Values { get; }
        string HasRequiredValues(String[] RequiredKeys);
    }
}